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
            .FirstOrDefaultAsync(m => m.Status != VideoIndexStatus.Indexed);
    }

    public async Task AddIndex(VideoIndex index)
    {
        await context.VideoIndices.AddAsync(index);
    }

    public async Task<List<VideoMeta>> Search(Vector vector, float tolerance, int count = 100)
    {
        var indicesFound = await context.VideoIndices.OrderBy(i => i.Vector.CosineDistance(vector))
            .Select(i => new { Index = i, Distance = i.Vector.CosineDistance(vector) })
            .Take(count)
            .ToListAsync();
    }
}