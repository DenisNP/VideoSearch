using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.VideoDescriber.Abstract;
using VideoSearch.VideoDescriber.Models;

namespace VideoSearch.Indexer.Steps;

public class TryFixErrorStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.Error;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.Described;

    private readonly string[] _prompts =
    [
        "Describe this video with a list of keywords. Only list, no title text, no long sentences.",
        "What keywords are describing this video?",
        "What words are describing this video?",
        "Please, describe this video with just a list of keywords"
    ];

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        var videoDescriberService = serviceProvider.GetRequiredService<IVideoDescriberService>();

        DescribeVideoRequest request = new(record.Url, _prompts.PickRandom());
        DescribeVideoResponse result = await videoDescriberService.Describe(request, nThread);

        if (result.Error != null)
        {
            throw new Exception(result.Error);
        }

        record.RawDescription = result.Result;
    }
}