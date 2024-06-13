namespace VideoSearch.Translator;

public static class Inject
{
    public static IServiceCollection AddTranslator(this IServiceCollection services, string? baseUrl)
    {
        return services.AddScoped<ITranslatorService>(_ => new LibreTranslatorService(baseUrl));
    }
}