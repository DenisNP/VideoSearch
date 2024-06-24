using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Models;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Indexer;

public class SearchService(IVectorizerService vectorizerService, IStorage storage)
{
    public async Task<List<SearchResult>> Search(string q)
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
            List<(VideoMeta video, double distance)> searchResult = await storage.Search(vec, 0.75f, 50000);
            foreach ((VideoMeta video, double dist) in searchResult)
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
        List<SearchResult> found = distances.Values
            .OrderBy(v => v.AvgDist)
            .Take(500)
            .Where(v => v.AvgDist <= expectedDist)
            .ToList();

        return found.ToList();
    }
}