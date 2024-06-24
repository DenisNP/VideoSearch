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
    public Task<List<VideoMeta>> GetByIds(List<Guid> ids);
    
    public Task<List<NgramDocument>> Search(string[] ngrams, int count, bool bm = false);

    public Task<List<VideoMeta>> ListIndexingVideos(int offset, int count);
    public Task<int> CountForStatus(VideoIndexStatus status);

    public Task<List<(string word, double sim)>> GetClosestWords(string word, double similarity, int limit = 50);

    public Task<NgramModel> GetOrCreateNgram(string ngram);
    public Task UpdateNgram(NgramModel ngramModel);
    public Task<NgramDocument> GetNgramDocument(string ngram, Guid documentId);
    public Task AddNgramDocument(NgramDocument document);
    public Task UpdateNgramDocument(NgramDocument document);
    public Task<double> GetTotalNgramsInDoc(Guid documentId);
}