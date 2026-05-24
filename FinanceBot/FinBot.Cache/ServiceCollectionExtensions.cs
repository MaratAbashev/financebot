using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCacheIntegration(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddSingleton<ICacheStorage>(_ =>
        {
            var connectionString = configuration["App:Redis:RedisCacheConnection"]!;
            return new CacheStorage(connectionString);
        });

        return serviceCollection;
    }
}