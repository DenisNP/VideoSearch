using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Models;
using VideoSearch.Indexer.Steps;

namespace VideoSearch.Indexer;

public class SearchService(IStorage storage, IServiceProvider serviceProvider)
{
    private const double Tolerance = 0.6;
    private const double IdfTolerance = 1.5;
    private const int Count = 300;

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
                    await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
                    IStorage tempStorage = scope.ServiceProvider.GetRequiredService<IStorage>();
                    var similar = await tempStorage.GetClosestWords(word, Tolerance);
                    allWords.AddRange(similar.Select(w => w.word));
                }));
            }
            await Task.WhenAll(tasks);

            words = allWords.Distinct().ToArray();
        }

        Dictionary<string, int> ngrams = Utils.GetNgrams(words, CreateIndexStep.NgramSize);
        List<NgramModel> ngModels = await storage.GetNgrams(ngrams.Keys.ToList());
        foreach (NgramModel ngModel in ngModels.Where(m => bm ? m.IdfBm < IdfTolerance : m.Idf < IdfTolerance))
        {
            ngrams.Remove(ngModel.Ngram);
        }

        List<NgramDocument> nDocs = await storage.Search(ngrams.Keys.ToArray(), Count * ngrams.Count * 2, bm);

        Dictionary<Guid, double> scores = new();
        foreach (NgramDocument nDoc in nDocs)
        {
            scores.TryAdd(nDoc.DocumentId, 0.0);
            scores[nDoc.DocumentId] += bm ? nDoc.ScoreBm : nDoc.Score;
        }

        List<Guid> ids = scores
            .OrderByDescending(s => s.Value)
            .Select(s => s.Key)
            .Take(Count)
            .ToList();

        var metas = await storage.GetByIds(ids);
        var result = metas.Select(m => new SearchResult(m, scores[m.Id])).OrderByDescending(r => r.Score).ToList();
        return result;
    }
}