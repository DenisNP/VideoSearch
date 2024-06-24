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
    public async Task<List<VideoMeta>> GetQueue([FromQuery] int count, [FromQuery] int offset = 0)
    {
        List<VideoMeta> videos = await storage.ListIndexingVideos(offset, count);
        return videos;
    }

    [HttpGet("GetCounters")]
    public async Task<Dictionary<string, int>> GetCounters()
    {
        var statuses = new[]
        {
            VideoIndexStatus.Queued,
            VideoIndexStatus.FullIndexed,
            VideoIndexStatus.Error
        };
        var result = new Dictionary<string, int>();
        foreach (VideoIndexStatus videoIndexStatus in statuses)
        {
            int total = await storage.CountForStatus(videoIndexStatus);
            result.Add(videoIndexStatus.ToString(), total);
        }

        return result;
    }

    [HttpGet("Search")]
    public Task<List<SearchResult>> Search([FromQuery] string q, [FromQuery] bool bm = false, [FromQuery] bool semantic = false)
    {
        return searchService.Search(q, bm, semantic);
    }

    [HttpGet("Hints")]
    public Task<List<string>> Hints([FromQuery] string q)
    {
        return Task.FromResult(hintService.GetHintsFor(q));
    }
}