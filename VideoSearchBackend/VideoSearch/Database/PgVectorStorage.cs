using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;

namespace VideoSearch.Database;

public class PgVectorStorage(VsContext context, ILogger<PgVectorStorage> logger) : IStorage
{
    private static readonly object Lock = new();
    
    public void Init()
    {
        context.Database.EnsureCreated();
        logger.LogInformation("Database initialized");
    }

    /*private async Task PreloadData()
    {
        string[] lines = await File.ReadAllLinesAsync("yappy_hackaton_2024_400k.csv");
        int count = 0;
        foreach (string line in lines)
        {
            var arr = line.Split(",");
            if (arr.Length > 0 && arr[0].StartsWith("https://") && arr[0].EndsWith(".mp4"))
            {
                count++;
                var meta = await context.VideoMetas.FirstOrDefaultAsync(m => m.Url == arr[0]);
                if (meta == null)
                {
                    await context.VideoMetas.AddAsync(new VideoMeta
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = count <= 500 ? DateTime.UtcNow - TimeSpan.FromDays(5) : DateTime.UtcNow,
                        StatusChangedAt = DateTime.UtcNow,
                        Status = VideoIndexStatus.Queued,
                        Url = arr[0],
                        RawDescription = null,
                        TranslatedDescription = null,
                        Stt = null,
                        Keywords = null,
                        Centroids = null
                    });
                } 
                else if (meta.Status == VideoIndexStatus.Unknown)
                {
                    meta.Status = VideoIndexStatus.Queued;
                    await context.SaveChangesAsync();
                }

                if (count % 1000 == 0)
                {
                    Console.WriteLine(count);
                }

                if (count >= 20500)
                {
                    break;
                }
            }
        }

        await context.VideoMetas.Where(m => m.Status == VideoIndexStatus.Unknown).ExecuteDeleteAsync();
    }*/

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

    public Task<VideoMeta> LockNextUnprocessed()
    {
        lock (Lock)
        {
            VideoMeta video = context.VideoMetas
                .OrderBy(m => m.StatusChangedAt)
                .FirstOrDefault(m => m.Status != VideoIndexStatus.Error 
                                     && m.Status != VideoIndexStatus.FullIndexed 
                                     && !m.Processing);

            if (video == null)
            {
                return null;
            }

            video.Processing = true;
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return Task.FromResult(video);
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
        
#if DEBUG
        foreach (var v in videos.OrderBy(v => bestDistances[v.Id]).Take(10))
        {
            Console.WriteLine(bestDistances[v.Id]);
            Console.WriteLine(v.Id);
            Console.WriteLine(string.Join(", ", v.Keywords.Take(15)));
            Console.WriteLine();
        }
#endif

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