using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using VideoSearch.Database.Abstract;
using VideoSearch.Database.Models;
using VideoSearch.Indexer.Models;

namespace VideoSearch.Database;

public class PgVectorStorage(VsContext context, ILogger<PgVectorStorage> logger) : IStorage
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
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CountAll()
    {
        return await context.VideoMetas.CountAsync();
    }

    public async Task<List<VideoMeta>> GetByIds(List<Guid> ids)
    {
        return await context.VideoMetas.Where(m => ids.Contains(m.Id)).ToListAsync();
    }

    public async Task<List<NgramDocument>> Search(string[] ngrams, int count, bool bm = false)
    {
        return await context.NgramDocuments
            .Where(nd => ngrams.Contains(nd.Ngram))
            .OrderByDescending(nd => bm ? nd.ScoreBm : nd.Score)
            .Take(count).ToListAsync();
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

    public async Task<List<(string word, double sim)>> GetClosestWords(string word, double similarity, int limit = 50)
    {
        WordVector wordFound = await context.Navec.FirstOrDefaultAsync(w => w.Word == word);
        if (wordFound == null)
        {
            return new List<(string word, double sim)>();
        }

        var vectorsFound = await context.Navec.OrderBy(i => i.Vector.CosineDistance(wordFound.Vector))
            .Select(w => new { Word = w, Distance = w.Vector.CosineDistance(wordFound.Vector) })
            // .Where(i => i.Distance <= distance)
            .Take(limit)
            .ToListAsync();

        return vectorsFound
            .Select(v => (word: v.Word.Word, sim: 1.0 - v.Distance))
            .Where(v => v.sim >= similarity)
            .ToList();
    }

    public async Task<NgramModel> GetOrCreateNgram(string ngram)
    {
        NgramModel ng = await context.Ngrams.FirstOrDefaultAsync(n => n.Ngram == ngram);
        if (ng == null)
        {
            ng = new NgramModel
            {
                Ngram = ngram,
                Idf = 0.0,
                IdfBm = 0.0,
                TotalDocs = 0,
                TotalNgrams = 0
            };
            await context.Ngrams.AddAsync(ng);
            await context.SaveChangesAsync();
        }

        return await context.Ngrams.FirstAsync(n => n.Ngram == ngram);
    }

    public async Task UpdateNgram(NgramModel ngramModel)
    {
        context.Ngrams.Update(ngramModel);
        await context.SaveChangesAsync();
    }

    public async Task<NgramDocument> GetNgramDocument(string ngram, Guid documentId)
    {
        return await context.NgramDocuments.FirstOrDefaultAsync(d => d.DocumentId == documentId && d.Ngram == ngram);
    }

    public async Task AddNgramDocument(NgramDocument document)
    {
        await context.NgramDocuments.AddAsync(document);
        await context.SaveChangesAsync();
    }

    public async Task UpdateNgramDocument(NgramDocument document)
    {
        context.NgramDocuments.Update(document);
        await context.SaveChangesAsync();
    }

    public async Task<double> GetTotalNgramsInDoc(Guid documentId)
    {
        return await context.NgramDocuments
            .Where(d => d.DocumentId == documentId)
            .Select(d => d.CountInDoc)
            .SumAsync();
    }
}