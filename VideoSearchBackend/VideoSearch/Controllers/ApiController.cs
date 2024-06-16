using Microsoft.AspNetCore.Mvc;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Indexer.Models;

namespace VideoSearch.Controllers;

[ApiController]
[Route("/api")]
public class ApiController(IStorage storage, SearchService searchService, IHintService hintService) : ControllerBase
{
    [HttpGet("GetQueue")]
    public async Task<IndexingResult> GetQueue([FromQuery] int count, [FromQuery] int offset = 0)
    {
        List<VideoMeta> videos = await storage.ListIndexingVideos(offset, count);
        var statuses = new[]
        {
            VideoIndexStatus.Queued,
            VideoIndexStatus.VideoIndexed,
            VideoIndexStatus.FullIndexed,
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

    [HttpGet("Search")]
    public Task<List<SearchResult>> Search([FromQuery] string q)
    {
        return searchService.Search(q);
    }

    [HttpGet("Hints")]
    public Task<List<string>> Hints([FromQuery] string q)
    {
        return Task.FromResult(hintService.GetHintsFor(q));
    }
}

public record IndexingResult(List<VideoMeta> Videos, Dictionary<string, int> TotalByStatus);