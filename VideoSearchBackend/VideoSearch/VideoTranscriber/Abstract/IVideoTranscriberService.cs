using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.VideoTranscriber.Abstract;

public interface IVideoTranscriberService
{
    public Task<TranscribeVideoResponse> Transcribe(TranscribeVideoRequest request);
}