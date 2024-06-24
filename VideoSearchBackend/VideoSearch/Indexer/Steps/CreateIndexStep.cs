using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Indexer.Steps;

public class CreateIndexStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.Translated;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.VideoIndexed;

    private const int NgramSize = 3;
    private const double AvgDocLenNgrams = 90.0;
    private const double SimilarityThreshold = 0.6;

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        // prepare
        int totalDocs = await storage.CountAll();
        
        // extract tokens
        List<string> tokens = record.TranslatedDescription
            .Tokenize()
            .Select(x => x.ToLower())
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        // vectorize
        var vectorizer = serviceProvider.GetRequiredService<IVectorizerService>();
        var similarReq = new SimilarWordsRequest(tokens.ToArray(), SimilarityThreshold);
        List<SimilarWordsResult> vectors = new List<SimilarWordsResult>();

        /*TODO try
        {
            vectors = await vectorizer.FindSimilarWords(similarReq);
        }
        catch
        {
            // ignored
        }*/

        Dictionary<string, double> lowerCoefficients = new();
        foreach (var (_, similarWords) in vectors)
        {
            foreach (var (word, sim) in similarWords)
            {
                if (!lowerCoefficients.ContainsKey(word) || sim > lowerCoefficients[word])
                {
                    lowerCoefficients[word] = sim;
                }
                tokens.Add(word);
            }
        }

        tokens = tokens.Distinct().ToList();

        // create indices
        await CreateIndexFor(storage, record, tokens, totalDocs, lowerCoefficients);
        
        // rebuild hints
        var hintService = serviceProvider.GetRequiredService<IHintService>();
        hintService.NotifyIndexUpdated();
    }

    public static async Task CreateIndexFor(IStorage storage, VideoMeta record, IList<string> tokens, int totalDocs, Dictionary<string, double> lowerCoefficients)
    {
        Dictionary<string, double> ngrams = Utils.GetNgrams(tokens, NgramSize, lowerCoefficients);
        double totalNgrams = ngrams.Values.Sum();

        foreach (var (ngram, ngCount) in ngrams)
        {
            await UpdateNgram(storage, ngram, ngCount, totalDocs);
            await AddNgramDocument(storage, record, ngram, ngCount, totalNgrams);
        }
    }

    private static async Task UpdateNgram(IStorage storage, string ngram, double ngCount, int totalDocs)
    {
        NgramModel ng = await storage.GetOrCreateNgram(ngram);
        ng.TotalDocs++;
        ng.TotalNgrams += ngCount;

        ng.Idf = Math.Log((double) totalDocs / ng.TotalDocs);
        ng.IdfBm = Utils.IdfBm(totalDocs, ng.TotalDocs);

        await storage.UpdateNgram(ng);
    }

    private static async Task AddNgramDocument(IStorage storage, VideoMeta document, string ngram, double ngCount, double ngramsInDoc)
    {
        var ngDoc = await storage.GetNgramDocument(ngram, document.Id);
        if (ngDoc == null)
        {
            ngDoc = new NgramDocument
            {
                Ngram = ngram,
                DocumentId = document.Id,
                CountInDoc = ngCount,
                TotalNgramsInDoc = ngramsInDoc,
                Tf = ((double) ngCount / ngramsInDoc),
                TfBm = Utils.TfBm(ngCount, ngramsInDoc, AvgDocLenNgrams)
            };

            await storage.AddNgramDocument(ngDoc);
        }
        else
        {
            ngDoc.CountInDoc += ngCount;
            ngDoc.TotalNgramsInDoc += ngramsInDoc;
            ngDoc.Tf = (double)ngDoc.CountInDoc / ngDoc.TotalNgramsInDoc;
            ngDoc.TfBm = Utils.TfBm(ngDoc.CountInDoc, ngDoc.TotalNgramsInDoc, AvgDocLenNgrams);

            await storage.UpdateNgramDocument(ngDoc);
        }
    }
}