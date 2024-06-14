using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.VideoDescriber.Abstract;
using VideoSearch.VideoDescriber.Models;

namespace VideoSearch.Indexer.Steps;

public class DescribeStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.Queued;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.Described;

    private const string Prompt = "Provide a list of keywords describing this video. Only list, no title text.";

    protected override async Task InternalRun(VideoMeta record)
    {
        var videoDescriberService = ServiceProvider.GetRequiredService<IVideoDescriberService>();

        DescribeVideoRequest request = new(record.Url, Prompt);
        DescribeVideoResponse result = await videoDescriberService.Describe(request);

        if (result.Error != null)
        {
            throw new Exception(result.Error);
        }

        record.RawDescription = result.Result;
    }
}