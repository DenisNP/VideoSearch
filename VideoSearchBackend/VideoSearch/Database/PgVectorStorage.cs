using Microsoft.EntityFrameworkCore;
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
        return await context.VideoMetas
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(m => m.Status != VideoIndexStatus.Indexed
                                      && m.Status != VideoIndexStatus.Added
                                      && m.Status != VideoIndexStatus.Error);
    }

    public async Task AddIndex(VideoIndex index)
    {
        await context.VideoIndices.AddAsync(index);
        await context.SaveChangesAsync();
    }

    public async Task<List<VideoMeta>> Search(Vector vector, float tolerance, int count = 100)
    {
        var indicesFound = await context.VideoIndices.OrderBy(i => i.Vector.CosineDistance(vector))
            .Select(i => new { Index = i, Distance = i.Vector.CosineDistance(vector) })
            .Take(count)
            .ToListAsync();
        
#if DEBUG
        foreach (var idx in indicesFound.Take(10))
        {
            Console.WriteLine(idx.Distance);
            Console.WriteLine(idx.Index.Word);
            Console.WriteLine();
        }
#endif

        List<Guid> ids = indicesFound
            .Where(i => i.Distance <= tolerance)
            .Select(i => i.Index.VideoMetaId)
            .ToList();

        List<VideoMeta> videos = await context.VideoMetas.Where(m => ids.Contains(m.Id)).ToListAsync();
        return videos;
    }
}