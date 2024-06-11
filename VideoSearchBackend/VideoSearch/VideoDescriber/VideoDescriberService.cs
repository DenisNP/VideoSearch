using VideoSearch.VideoDescriber.Abstract;
using VideoSearch.VideoDescriber.Models;

namespace VideoSearch.VideoDescriber;

public class VideoDescriberService(string? baseUrl) : IVideoDescriberService
{
    private readonly string _baseUrl = baseUrl
        ?? throw new Exception(nameof(VideoDescriberService) + " " + nameof(baseUrl) + " is null");

    public async Task<DescribeVideoResponse> Describe(DescribeVideoRequest request)
    {
        // TODO
        return new DescribeVideoResponse(
            "Soccer\nVideo game\nFIFA\nGameplay\nVirtual\nOnline\nMultiplayer\nFootball\nSports simulation\nEA Sport",
            null
        );
    }
}