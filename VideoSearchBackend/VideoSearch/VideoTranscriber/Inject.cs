using VideoSearch.VideoTranscriber.Abstract;

namespace VideoSearch.VideoTranscriber;

public static class Inject
{
    public static IServiceCollection AddVideoTranscriber(this IServiceCollection services, string? baseUrl)
    {
        return services.AddScoped<IVideoTranscriberService>(_ => new GigaAmVideoTranscriberService(baseUrl));
    }
}