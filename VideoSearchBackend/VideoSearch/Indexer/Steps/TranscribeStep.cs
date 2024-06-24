using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.VideoTranscriber.Abstract;
using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.Indexer.Steps;

public class TranscribeStep(ILogger logger) : BaseIndexStep(logger)
{
    private const int MaxKeywords = 5;

    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.VideoIndexed;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.FullIndexed;

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        var hintService = serviceProvider.GetRequiredService<IHintService>();
        string[] words = null;

        if (record.SttKeywords == null)
        {
            return; // TODO
            var videoTranscriberService = serviceProvider.GetRequiredService<IVideoTranscriberService>();

            var transcribeRequest = new TranscribeVideoRequest(record.Url);
            var transcribeResult = await videoTranscriberService.Transcribe(transcribeRequest);

            if (transcribeResult.Error != null)
            {
                throw new Exception(transcribeResult.Error);
            }

            words = transcribeResult.Result?.Take(MaxKeywords).ToArray() ?? [];
            record.SttKeywords = words.ToList();
        }
        else
        {
            words = record.SttKeywords.ToArray();
        }

        if (words.Length == 0)
        {
            return;
        }

        await CreateIndexStep.CreateIndexFor(storage, record, words, await storage.CountAll(), new Dictionary<string, double>());

        hintService.NotifyIndexUpdated();
    }
}