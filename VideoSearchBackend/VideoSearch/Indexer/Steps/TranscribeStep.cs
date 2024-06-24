using Pgvector;
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
    private const int MaxKeywords = 4;

    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.VideoIndexed;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.FullIndexed;

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        var videoTranscriberService = serviceProvider.GetRequiredService<IVideoTranscriberService>();
        var vectorizer = serviceProvider.GetRequiredService<IVectorizerService>();
        var hintService = serviceProvider.GetRequiredService<IHintService>();

        var transcribeRequest = new TranscribeVideoRequest(record.Url);
        var transcribeResult = await videoTranscriberService.Transcribe(transcribeRequest);

        if (transcribeResult.Error != null)
        {
            if (transcribeResult.Error.Contains("No such file or directory"))
            {
                return;
            }
            throw new Exception(transcribeResult.Error);
        }

        string[] words = transcribeResult.Result?.ToArray() ?? [];

        if (words.Length == 0)
        {
            return;
        }

        var vectorizeRequest = new VectorizeRequest(words);
        List<VectorizedWord> vectorizeResult = await vectorizer.Vectorize(vectorizeRequest);
        vectorizeResult = vectorizeResult.Take(MaxKeywords).ToList();

        if (vectorizeResult.Count == 0)
        {
            return;
        }

        record.SttKeywords = vectorizeResult.Select(v => v.Word).ToList();
        await CreateIndices(record, storage, vectorizeResult);
        hintService.NotifyIndexUpdated();
    }

    private async Task CreateIndices(VideoMeta record, IStorage storage, List<VectorizedWord> vectorizeResult)
    {
        await storage.RemoveIndicesFor(record.Id, VideoIndexType.Stt);

        foreach (VectorizedWord vectorizedWord in vectorizeResult)
        {
            await storage.AddIndex(new VideoIndex
            {
                Id = Guid.NewGuid(),
                VideoMetaId = record.Id,
                Word = vectorizedWord.Word,
                Vector = new Vector(vectorizedWord.Vector),
                ClusterSize = 1,
                Type = VideoIndexType.Stt
            });
        }
    }
}