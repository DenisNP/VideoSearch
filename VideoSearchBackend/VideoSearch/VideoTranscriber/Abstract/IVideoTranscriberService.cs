using Microsoft.AspNetCore.Routing.Template;
using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.VideoTranscriber.Abstract;

public interface IVideoTranscriberService
{
    public Task<TemplateValuesResult> Transcribe(TranscribeVideoRequest request);
}