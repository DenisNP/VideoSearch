using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Vectorizer.Abstract;

namespace VideoSearch.Database;

public class PgVectorStorage(VsContext context, ILogger<PgVectorStorage> logger, IVectorizerService vectorizerService) : IStorage
{
    private static readonly object Lock = new();

    public void Init()
    {
        context.Database.EnsureCreated();
        logger.LogInformation("Database initialized");
    }

    public async Task AddMeta(VideoMeta meta)
    {
        await context.VideoMetas.AddAsync(meta);
        await context.SaveChangesAsync();
    }

    public async Task UpdateMeta(VideoMeta meta)
    {
        context.VideoMetas.Update(meta);
        await context.SaveChangesAsync();
    }

    public VideoMeta LockNextUnprocessed()
    {
        lock (Lock)
        {
            var statusesExclude = new List<VideoIndexStatus>
            {
                VideoIndexStatus.Queued,
                VideoIndexStatus.Error,
                VideoIndexStatus.FullIndexed
            };

            VideoMeta video = null;
            while (statusesExclude.Count > 0 && video == null)
            {
                video = context.VideoMetas
                    .OrderBy(m => m.StatusChangedAt)
                    .FirstOrDefault(m => !statusesExclude.Contains(m.Status)
                                         && !m.Processing);
                
                statusesExclude.RemoveAt(0);
            }

            // nothing to index
            if (video == null)
            {
                return null;
            }

            video.Processing = true;
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return video;
        }
    }

    public async Task ClearProcessing()
    {
        List<VideoMeta> processing = await context.VideoMetas
            .Where(m => m.Processing)
            .ToListAsync();
        
        processing.ForEach(m => m.Processing = false);
        await context.SaveChangesAsync();
    }

    public async Task<List<VideoMeta>> GetAllIndexed()
    {
        return await context.VideoMetas
            .Where(m => m.Status == VideoIndexStatus.VideoIndexed || m.Status == VideoIndexStatus.FullIndexed)
            .ToListAsync();
    }

    public async Task AddIndex(VideoIndex index)
    {
        await context.VideoIndices.AddAsync(index);
        await context.SaveChangesAsync();
    }

    public async Task<List<(VideoMeta video, double distance)>> Search(float[] vector, float tolerance, int indexSearchCount = 100)
    {
        var vec = new Vector(vector);
        var indicesFound = await context.VideoIndices.OrderBy(i => i.Vector.CosineDistance(vec))
            .Select(i => new { Index = i, Distance = i.Vector.CosineDistance(vec) })
            //.Where(i => i.Distance <= tolerance)
            .Take(indexSearchCount)
            .ToListAsync();

        var bestDistances = new Dictionary<Guid, double>();
        foreach (var idx in indicesFound.Where(i => i.Distance <= tolerance))
        {
            Guid metaId = idx.Index.VideoMetaId;
            double newDist = idx.Distance;

            if (!bestDistances.TryGetValue(metaId, out double oldDist))
            {
                bestDistances.Add(metaId, newDist);
            }
            else if (oldDist > newDist)
            {
                bestDistances[metaId] = newDist;
            }
        }

        List<Guid> ids = bestDistances.Keys.ToList();
        List<VideoMeta> videos = await context.VideoMetas
            .Where(m => ids.Contains(m.Id))
            .ToListAsync();

        return videos.Select(v => (video: v, distance: bestDistances[v.Id])).OrderBy(x => x.distance).ToList();
    }

    public async Task<List<VideoMeta>> ListIndexingVideos(int offset, int count)
    {
        return await context.VideoMetas
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> CountForStatus(VideoIndexStatus status)
    {
        return await context.VideoMetas.CountAsync(v => v.Status == status);
    }

    public async Task RemoveIndicesFor(Guid videoMetaId, VideoIndexType indexType)
    {
        await context.VideoIndices
            .Where(i => i.VideoMetaId == videoMetaId && i.Type == indexType)
            .ExecuteDeleteAsync();
    }
}