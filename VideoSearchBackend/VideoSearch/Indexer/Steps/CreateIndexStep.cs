using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.External.KMeans;
using VideoSearch.Indexer.Abstract;

namespace VideoSearch.Indexer.Steps;

public class CreateIndexStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.Translated;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.VideoIndexed;

    public const int NgramSize = 3;
    private const double SimilarityThreshold = 0.6;
    public const int MaxMainKeywords = 12;
    public const int MaxAdditionalTotal = 36;

    protected override async Task InternalRun(
        VideoMeta record,
        IServiceProvider serviceProvider,
        IStorage storage,
        int nThread
    )
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

        tokens = await ShrinkTokens(storage, tokens);

        // vectorize
        var vectors = new List<(string word, double sim)>();
        var additionalPerMain = (int)Math.Ceiling((double) MaxAdditionalTotal / tokens.Count);
        foreach (var token in tokens)
        {
            List<(string word, double sim)> closest = await storage.GetClosestWords(token, SimilarityThreshold, additionalPerMain);
            vectors.AddRange(closest);
        }

        Dictionary<string, double> lowerCoefficients = new();
        List<string> additionalTokens = new();
        foreach (var (word, sim) in vectors)
        {
            if (!lowerCoefficients.ContainsKey(word) || sim > lowerCoefficients[word])
            {
                lowerCoefficients[word] = sim;
            }
            additionalTokens.Add(word);
        }

        // set kw
        record.Keywords = tokens;
        record.Cloud = additionalTokens.Distinct().ToList();
        List<string> fullTokens = tokens.Concat(additionalTokens).Distinct().ToList();

        // create indices
        await CreateIndexFor(storage, record, fullTokens, totalDocs, lowerCoefficients);

        // rebuild hints
        var hintService = serviceProvider.GetRequiredService<IHintService>();
        hintService.NotifyIndexUpdated();
    }

    private async Task<List<string>> ShrinkTokens(IStorage storage, List<string> tokens)
    {
        if (tokens.Count <= MaxMainKeywords)
        {
            return tokens;
        }

        List<WordVector> vectors = await storage.GetVectors(tokens);
        DataVec[] dataVecs = vectors
            .Select(v => new DataVec(v.Vector.AsDoubleArray()) { Word = v.Word })
            .ToArray();

        if (dataVecs.Length <= MaxMainKeywords)
        {
            return tokens;
        }

        var clustering = new KMeansClustering(dataVecs, MaxMainKeywords);
        var clusters = clustering.Compute();
        var centers = clusters.Select(c => c.MostCenterPoint().Word).ToList();

        return centers;
    }

    public static async Task CreateIndexFor(IStorage storage, VideoMeta record, IList<string> tokens, int totalDocs, Dictionary<string, double> lowerCoefficients)
    {
        Dictionary<string, double> ngrams = Utils.GetNgrams(tokens, NgramSize, lowerCoefficients);
        double totalNgramsInDoc = await storage.GetTotalNgramsInDoc(record.Id) + ngrams.Values.Sum();

        foreach (var (ngram, ngCount) in ngrams)
        {
            NgramModel ngModel = await UpdateNgram(storage, ngram, ngCount, totalDocs);
            await AddNgramDocument(storage, record, ngModel, ngram, ngCount, totalNgramsInDoc);
        }
    }

    private static async Task<NgramModel> UpdateNgram(IStorage storage, string ngram, double ngCount, int totalDocs)
    {
        NgramModel ng = await storage.GetOrCreateNgram(ngram);
        ng.TotalDocs++;
        ng.TotalNgrams += ngCount;

        ng.Idf = Math.Log((double) totalDocs / ng.TotalDocs);
        ng.IdfBm = Utils.IdfBm(totalDocs, ng.TotalDocs);

        await storage.UpdateNgram(ng);
        return ng;
    }

    private static async Task AddNgramDocument(IStorage storage, VideoMeta document, NgramModel ngramModel, string ngram, double ngCount, double ngramsInDoc)
    {
        var ngDoc = await storage.GetNgramDocument(ngram, document.Id);
        if (ngDoc == null)
        {
            ngDoc = new NgramDocument
            {
                Ngram = ngram,
                DocumentId = document.Id,
                CountInDoc = ngCount,
                Tf = ((double) ngCount / ngramsInDoc),
                TfBm = Utils.TfBm(ngCount, ngramsInDoc, Utils.GetAverageDocLenNgrams()),
            };
            ngDoc.Score = ngDoc.Tf * ngramModel.Idf;
            ngDoc.ScoreBm = ngDoc.TfBm * ngramModel.IdfBm;

            await storage.AddNgramDocument(ngDoc);
        }
        else
        {
            ngDoc.CountInDoc += ngCount;
            ngDoc.Tf = (double)ngDoc.CountInDoc / ngramsInDoc;
            ngDoc.TfBm = Utils.TfBm(ngDoc.CountInDoc, ngramsInDoc, Utils.GetAverageDocLenNgrams());
            ngDoc.Score = ngDoc.Tf * ngramModel.Idf;
            ngDoc.ScoreBm = ngDoc.TfBm * ngramModel.IdfBm;

            await storage.UpdateNgramDocument(ngDoc);
        }
    }
}