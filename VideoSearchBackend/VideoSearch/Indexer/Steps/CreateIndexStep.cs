using Pgvector;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.External.KMeans;
using VideoSearch.Indexer.Abstract;
using VideoSearch.Vectorizer.Abstract;
using VideoSearch.Vectorizer.Models;

namespace VideoSearch.Indexer.Steps;

public class CreateIndexStep(ILogger logger) : BaseIndexStep(logger)
{
    protected override VideoIndexStatus InitialStatus => VideoIndexStatus.Translated;
    protected override VideoIndexStatus TargetStatus => VideoIndexStatus.VideoIndexed;

    protected override async Task InternalRun(VideoMeta record, IServiceProvider serviceProvider, IStorage storage, int nThread)
    {
        string[] tokens = record.TranslatedDescription
            .Tokenize()
            .Select(x => x.ToLower())
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var vectorizer = serviceProvider.GetRequiredService<IVectorizerService>();
        var hintService = serviceProvider.GetRequiredService<IHintService>();

        var request = new VectorizeRequest(tokens);
        List<VectorizedWord> vectors = await vectorizer.Vectorize(request);

        record.Keywords = vectors.Select(v => v.Word).ToList();

        List<DataVec> points = vectors
            .Select(v => new DataVec(v.Vector.Select(x => (double)x).ToArray()){ Word = v.Word })
            .ToList();

        // run indexing
        Cluster[] clusters = Clasterize(points);
        await CreateIndices(clusters, record, storage);
        hintService.NotifyIndexUpdated();
    }

    private async Task CreateIndices(Cluster[] clusters, VideoMeta record, IStorage storage)
    {
        record.Centroids = new();
        await storage.RemoveIndicesFor(record.Id, VideoIndexType.Video);

        foreach (Cluster cluster in clusters)
        {
            string word = cluster.MostCenterPoint().Word;
            record.Centroids.Add(word);

            await storage.AddIndex(new VideoIndex
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

    private Cluster[] Clasterize(List<DataVec> points)
    {
        var cl = new KMeansClustering(points.ToArray(), Math.Clamp(points.Count / 12, 2, 4));
        Cluster[] clusters =  cl.Compute();

        int maxClusterPoints = clusters.Select(c => c.Points.Count).MaxBy(x => x);
        int minimumPoints = Math.Max(2, maxClusterPoints / 10);

        return clusters.Where(c => c.Points.Count >= minimumPoints).ToArray();
    }
}