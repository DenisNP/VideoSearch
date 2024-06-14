﻿using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;

namespace VideoSearch.Database;

public class PgVectorStorage(VsContext context, ILogger<PgVectorStorage> logger) : IStorage
{
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

    public async Task<VideoMeta> GetNextNotIndexed()
    {
        VideoMeta video = await context.VideoMetas
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(m => m.Status != VideoIndexStatus.Indexed
                                      && m.Status != VideoIndexStatus.Error);

        if (video == null)
        {
            return null;
        }

        video.Status = VideoIndexStatus.Queued;
        await UpdateMeta(video);
        return video;
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
            .OrderBy(m => m.Status)
            .ThenByDescending(m => m.StatusChangedAt)
            .Where(m => m.Status != VideoIndexStatus.Idle)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> CountForStatus(VideoIndexStatus status)
    {
        return await context.VideoMetas.CountAsync(v => v.Status == status);
    }
}