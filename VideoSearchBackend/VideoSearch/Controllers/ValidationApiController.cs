using Microsoft.AspNetCore.Mvc;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer;
using VideoSearch.Indexer.Models;
using VideoSearch.Validation.Models;

namespace VideoSearch.Controllers;

[ApiController]
[Route("validation-api")]
public class ValidationApiController(IStorage storage, SearchService searchService) : ControllerBase
{
    [HttpPost("/index")]
    [ProducesResponseType(typeof(Video), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> AddIndex([FromBody] Video video)
    {
        if (video == null || string.IsNullOrEmpty(video.Link) || string.IsNullOrEmpty(video.Description))
        {
            return BadRequest("Invalid input");
        }

        if (!video.Link.StartsWith("https") || !video.Link.EndsWith(".mp4"))
        {
            return UnprocessableEntity("Validation exception");
        }

        try
        {
            await storage.AddMeta(new VideoMeta
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                StatusChangedAt = DateTime.UtcNow,
                Status = VideoIndexStatus.Queued,
                Url = video.Link
            });
            return Ok();
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpGet("/search")]
    [ProducesResponseType(typeof(List<Video>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchVideo([FromQuery] string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return BadRequest("Empty search query");
        }

        List<SearchResult> searchResults = await searchService.Search(text);
        
        List<Video> results = searchResults.Select(result => new Video(
            result.Video.Url,
            string.Join(" ", result.Video.Keywords.Select(k => $"#{k}"))
        )).ToList();

        return Ok(results);
    }
}
