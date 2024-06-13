using Microsoft.AspNetCore.Mvc;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;

namespace VideoSearch.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController(IStorage storage) : ControllerBase
{
    [HttpGet("/GetIndexing")]
    public async Task<List<VideoMeta>> GetIndexing([FromQuery] int count)
    {
        return await storage.ListIndexingVideos(count);
    }
}