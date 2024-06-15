using Microsoft.AspNetCore.Routing.Template;
using VideoSearch.VideoTranscriber.Abstract;
using VideoSearch.VideoTranscriber.Models;

namespace VideoSearch.VideoTranscriber;

public class GigaAmVideoTranscriberService(string? baseUrl) : IVideoTranscriberService
{
    public Task<TemplateValuesResult> Transcribe(TranscribeVideoRequest request)
    {
        throw new NotImplementedException();
    }
}