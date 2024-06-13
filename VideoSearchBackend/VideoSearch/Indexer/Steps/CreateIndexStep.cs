using Pgvector;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Indexer.Steps;

public class CreateIndexStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.Translated;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.Indexed;

    protected override async Task InternalRun(VideoMeta record)
    {
        string[] tokens = record.TranslatedDescription
            .Tokenize()
            .Select(x => x.ToLower())
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var vectorizer = ServiceProvider.GetRequiredService<IVectorizerService>();

        var request = new VectorizeRequest(tokens);
        var vectors = await vectorizer.Vectorize(request);

        record.Keywords = new();

        foreach (var (word, vector) in vectors)
        {
            record.Keywords.Add(word);
            await Storage.AddIndex(new VideoIndex
            {
                Id = Guid.NewGuid(),
                VideoMetaId = record.Id,
                Word = word,
                Vector = new Vector(vector)
            });
        }
    }
}