using VideoSearch.VideoDescriber.Abstract;

namespace VideoSearch.VideoDescriber;

public static class Inject
{
    public static IServiceCollection AddVideoDescriber(this IServiceCollection services, string? baseUrl)
    {
        return services.AddScoped<IVideoDescriberService>(_ => new VideoDescriberService(baseUrl));
    }
}