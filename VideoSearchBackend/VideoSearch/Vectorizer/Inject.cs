using VideoSearch.Vectorizer.Abstract;

namespace VideoSearch.Vectorizer;

public static class Inject
{
    public static IServiceCollection AddVectorizer(this IServiceCollection services, string? baseUrl)
    {
        return services.AddScoped<IVectorizerService>(_ => new NavecVectorizerService(baseUrl));
    }
}