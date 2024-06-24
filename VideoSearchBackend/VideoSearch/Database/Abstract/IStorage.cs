using VideoSearch.Database.Models;

namespace VideoSearch.Database.Abstract;

public interface IStorage
{
    public void Init();
    
    public Task AddMeta(VideoMeta meta);
    public Task UpdateMeta(VideoMeta meta);
    public VideoMeta LockNextUnprocessed();
    public Task ClearProcessing();
    public Task<List<VideoMeta>> GetAllIndexed();
    public Task<int> CountAll();

    public Task AddIndex(VideoIndex index);
    public Task<List<(VideoMeta video, double distance)>> Search(float[] vector, float tolerance, int indexSearchCount = 100);
    public Task<List<VideoMeta>> ListIndexingVideos(int offset, int count);
    public Task<int> CountForStatus(VideoIndexStatus status);
    public Task RemoveIndicesFor(Guid videoMetaId, VideoIndexType indexType);

    public Task<NgramModel> GetOrCreateNgram(string ngram);
    public Task UpdateNgram(NgramModel ngramModel);
    public Task<NgramDocument> GetNgramDocument(string ngram, Guid documentId);
    public Task AddNgramDocument(NgramDocument document);
    public Task UpdateNgramDocument(NgramDocument document);
}