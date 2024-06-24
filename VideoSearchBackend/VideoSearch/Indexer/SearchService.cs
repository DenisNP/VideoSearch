using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Models;
using VideoSearch.Indexer.Steps;

namespace VideoSearch.Indexer;

public class SearchService(IStorage storage)
{
    private const double Tolerance = 0.6;

    public async Task<List<SearchResult>> Search(string q, bool bm = false, bool semantic = false)
    {
        string[] words = q.Tokenize();

        if (semantic)
        {
            var allWords = new List<string>(words);

            List<Task> tasks = new();
            foreach (var word in words)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var similar = await storage.GetClosestWords(word, Tolerance);
                    allWords.AddRange(similar.Select(w => w.word));
                }));
            }
            await Task.WhenAll(tasks);

            words = allWords.Distinct().ToArray();
        }

        var ngrams = Utils.GetNgrams(words, CreateIndexStep.NgramSize);
        List<NgramDocument> nDocs =
            await storage.Search(ngrams.Keys.ToArray(), (int)(300 * Utils.GetAverageDocLenNgrams()), bm);

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