using VideoSearch.Database.Models;

namespace VideoSearch.Database.Abstract;

public interface IStorage
{
    public void Init();
    
    public Task AddMeta(VideoMeta meta);
    public Task UpdateMeta(VideoMeta meta);
    public Task<VideoMeta> GetNextNotIndexed();

    public Task AddIndex(VideoIndex index);
    public Task<List<VideoMeta>> Search(float[] vector, float tolerance, int indexSearchCount = 100);
    public Task<List<VideoMeta>> ListIndexingVideos(int count);
}