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

    private const int MinimumPoints = 4;

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
        Cluster[] clusters = Clusterize(points);
        await CreateIndices(clusters, record, storage);
        hintService.NotifyIndexUpdated();
    }

    private async Task CreateIndices(Cluster[] clusters, VideoMeta record, IStorage storage)
    {
        record.Centroids = new();
        await storage.RemoveIndicesFor(record.Id, VideoIndexType.Video);

        foreach (Cluster cluster in clusters)
        {
            DataVec center = cluster.MostCenterPoint();
            record.Centroids.Add(center.Word);

            // add centroid itself
            await storage.AddIndex(new VideoIndex
            {
                Id = Guid.NewGuid(),
                VideoMetaId = record.Id,
                Word = center.Word,
                Vector = new Vector(cluster.Centroid.Components.Select(x => (float)x).ToArray()),
                ClusterSize = cluster.Points.Count,
                Type = VideoIndexType.Video
            });
            
            // add closest to center word
            await storage.AddIndex(new VideoIndex
            {
                Id = Guid.NewGuid(),
                VideoMetaId = record.Id,
                Word = center.Word,
                Vector = new Vector(center.Components.Select(c => (float)c).ToArray()),
                ClusterSize = cluster.Points.Count,
                Type = VideoIndexType.Video
            });
        }
    }

    private Cluster[] Clusterize(List<DataVec> points)
    {
        double lastDist = Double.MaxValue;
        Cluster[] lastClusters = null;
        double bestDist = Double.MaxValue;
        Cluster[] bestClusters = null;
        var bestWasSet = false;

        int startCount = points.Count / 2;
        int endCount = startCount >= 2 ? 2 : startCount;

        for (int num = startCount; num >= endCount; num--)
        {
            (Cluster[] clusters, double avgDist) = FindClusterVariant(points, num);

            bestClusters ??= clusters;
            
            if (avgDist < bestDist)
            {
                double avgPoints = clusters.AveragePointsCount();
                if (avgPoints >= MinimumPoints)
                {
                    bestDist = avgDist;
                    bestClusters = clusters;
                    bestWasSet = true;
                }
            }

            if (lastClusters != null && lastDist > avgDist && bestWasSet)
            {
                bestDist = avgDist;
                bestClusters = clusters;
                break;
            }

            lastDist = avgDist;
            lastClusters = clusters;
        }

        /*Console.WriteLine("Total: {0}, clusters: {1}, points: {2}, avgDist: {3}",
            points.Count,
            bestClusters.Length,
            bestClusters.AveragePointsCount(),
            bestDist
        );
        Console.WriteLine();*/
        return bestClusters;
    }

    private (Cluster[] clusters, double avgDist) FindClusterVariant(List<DataVec> points, int numClusters)
    {
        var cl = new KMeansClustering(points.ToArray(), Math.Min(numClusters, points.Count));
        Cluster[] clusters =  cl.Compute();

        return (clusters, clusters.Select(c => c.AvgDistanceToCenter()).Average());
    }
}