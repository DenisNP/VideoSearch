using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController(IStorage storage, IVectorizerService vectorizerService) : ControllerBase
{
    [HttpGet("/GetIndexing")]
    public async Task<List<VideoMeta>> GetIndexing([FromQuery] int count, [FromQuery] int offset = 0)
    {
        return await storage.ListIndexingVideos(offset, count);
    }

    [HttpGet("/Search")]
    public async Task<List<SearchResult>> Search([FromQuery] string q)
    {
        string[] words = q.Tokenize();
        List<VectorizedWord> vectors = await vectorizerService.Vectorize(new VectorizeRequest(words));
        if (vectors.Count == 0)
        {
            return new List<SearchResult>();
        }

        // collect found and distances
        Dictionary<Guid, SearchResult> distances = new();
        foreach (var (_, vec) in vectors)
        {
            var searchResult = await storage.Search(vec, 0.75f);
            foreach ((VideoMeta video, var dist) in searchResult)
            {
                if (!distances.ContainsKey(video.Id))
                {
                    distances.Add(video.Id, new SearchResult(video, new List<double>()));
                }
                distances[video.Id].Distances.Add(dist);
            }
        }

        // find avg
        foreach (SearchResult searchResult in distances.Values)
        {
            // add penalty for not found words
            while (searchResult.Distances.Count < vectors.Count)
            {
                searchResult.Distances.Add(1.0);
            }
        }

        double expectedDist = Math.Min(0.65 + (vectors.Count - 1) * 0.1, 0.9);
        var found = distances.Values.OrderBy(v => v.AvgDist).Where(v => v.AvgDist <= expectedDist).ToList();
        return found.ToList();
    }
}

public record SearchResult(VideoMeta Video, [property: JsonIgnore] List<double> Distances)
{
    private double? _avg;
    public double AvgDist => _avg ??= Distances.Average();
}