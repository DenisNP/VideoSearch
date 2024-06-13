using VideoSearch.Database.Abstract;

namespace VideoSearch.Database;

public static class Inject
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddDbContext<VsContext>();
        services.AddScoped<IStorage, PgVectorStorage>();

        return services;
    }
}