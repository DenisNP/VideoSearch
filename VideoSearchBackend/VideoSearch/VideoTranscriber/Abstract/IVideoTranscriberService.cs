using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.VideoTranscriber.Abstract;

public interface IVideoTranscriberService
{
    public bool IsActivated();
    public Task<TranscribeVideoResponse> Transcribe(TranscribeVideoRequest request);
}