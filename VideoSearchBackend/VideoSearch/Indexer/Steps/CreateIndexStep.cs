using Pgvector;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Abstract;
using VideoSearch.KMeans;
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

        var nonZero = vectors
            .Where(v => v.Vector.Length > 0)
            .ToList();

        record.Keywords = new();
        
        List<DataVec> points = nonZero
            .Select(v => new DataVec(v.Vector.Select(x => (double)x).ToArray()){ Word = v.Word })
            .ToList();

        // create clusters
        KMeansClustering cl = new KMeansClustering(points.ToArray(), Math.Clamp(points.Count / 12, 2, 4));
        Cluster[] clusters =  cl.Compute();

        var maxClusterPoints = clusters.Select(c => c.Points.Count).MaxBy(x => x);

        foreach (Cluster cluster in clusters/*.Where(c => c.Points.Count >= maxClusterPoints / 2)*/)
        {
            var word = cluster.MostCenterPoint().Word;
            record.Keywords.Add(word);

            await Storage.AddIndex(new VideoIndex
            {
                Id = Guid.NewGuid(),
                VideoMetaId = record.Id,
                Word = word,
                Vector = new Vector(cluster.Centroid.Components.Select(x => (float)x).ToArray()),
                ClusterSize = cluster.Points.Count,
                Type = VideoIndexType.Video
            });
        }
    }
}