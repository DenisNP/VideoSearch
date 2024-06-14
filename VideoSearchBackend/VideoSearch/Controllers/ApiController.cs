using Microsoft.AspNetCore.Mvc;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer;
using VideoSearch.Indexer.Models;

namespace VideoSearch.Controllers;

[ApiController]
[Route("api")]
public class ApiController(IStorage storage, SearchService searchService) : ControllerBase
{
    [HttpGet("/GetIndexing")]
    public async Task<IndexingResult> GetIndexing([FromQuery] int count, [FromQuery] int offset = 0)
    {
        List<VideoMeta> videos = await storage.ListIndexingVideos(offset, count);
        var statuses = new[]
        {
            // VideoIndexStatus.Added,
            VideoIndexStatus.Queued,
            VideoIndexStatus.Indexed,
            VideoIndexStatus.Error
        };

        var result = new IndexingResult(videos, new Dictionary<string, int>());
        foreach (VideoIndexStatus videoIndexStatus in statuses)
        {
            int total = await storage.CountForStatus(videoIndexStatus);
            result.TotalByStatus.Add(videoIndexStatus.ToString(), total);
        }

        return result;
    }

    [HttpGet("/Search")]
    public Task<List<SearchResult>> Search([FromQuery] string q)
    {
        return searchService.Search(q);
    }
}

public record IndexingResult(List<VideoMeta> Videos, Dictionary<string, int> TotalByStatus);