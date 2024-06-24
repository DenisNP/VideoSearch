using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Models;
using VideoSearch.Indexer.Steps;

namespace VideoSearch.Indexer;

public class SearchService(IStorage storage)
{
    public async Task<List<SearchResult>> Search(string q, bool bm = false)
    {
        string[] words = q.Tokenize();
        var ngrams = Utils.GetNgrams(words, CreateIndexStep.NgramSize);
        List<NgramDocument> nDocs =
            await storage.Search(ngrams.Keys.ToArray(), (int)(300 * CreateIndexStep.AvgDocLenNgrams), bm);

        Dictionary<Guid, double> scores = new();
        foreach (NgramDocument nDoc in nDocs)
        {
            scores.TryAdd(nDoc.DocumentId, 0.0);
            scores[nDoc.DocumentId] += bm ? nDoc.ScoreBm : nDoc.Score;
        }

        List<Guid> ids = scores.Select(s => s.Key).ToList();

        var metas = await storage.GetByIds(ids);
        var result = metas.Select(m => new SearchResult(m, scores[m.Id])).OrderByDescending(r => r.Score).ToList();
        return result;
    }
}