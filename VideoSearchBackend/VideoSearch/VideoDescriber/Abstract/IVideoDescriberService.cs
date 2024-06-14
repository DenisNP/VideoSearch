using VideoSearch.VideoDescriber.Models;

namespace VideoSearch.VideoDescriber.Abstract;

public interface IVideoDescriberService
{
    public Task<DescribeVideoResponse> Describe(DescribeVideoRequest request, int nThread = -1);
}