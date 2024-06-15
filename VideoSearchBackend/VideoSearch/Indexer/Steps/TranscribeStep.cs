using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.Vectorizer.Models;
using VideoSearch.VideoTranscriber.Abstract;
using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.Indexer.Steps;

public class TranscribeStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.VideoIndexed;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.FullIndexed;

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        var videoTranscriberService = serviceProvider.GetRequiredService<IVideoTranscriberService>();
        var vectorizer = serviceProvider.GetRequiredService<IVectorizerService>();

        var transcribeRequest = new TranscribeVideoRequest(record.Url);
        var transcribeResult = await videoTranscriberService.Transcribe(transcribeRequest);

        var words = transcribeResult.Result?.ToArray();
        var vectorizeRequest = new VectorizeRequest(words);
        var vectorizeResult = await vectorizer.Vectorize(vectorizeRequest);

        record.SttKeywords = vectorizeResult.Select(v => v.Word).ToList();

        await storage.RemoveIndicesFor(record.Id, VideoIndexType.Stt);

        foreach (var vectorizedWord in vectorizeResult)
        {
            await storage.AddIndex(new VideoIndex
            {
                Id = Guid.NewGuid(),
                VideoMetaId = record.Id,
                Word = vectorizedWord.Word,
                Vector = new Pgvector.Vector(vectorizedWord.Vector),
                ClusterSize = 1,
                Type = VideoIndexType.Stt
            });
        }
    }
}