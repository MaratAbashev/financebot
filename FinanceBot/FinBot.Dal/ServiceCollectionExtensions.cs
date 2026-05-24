using FinBot.Dal.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Dal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDb(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDb<PDbContext>(configuration, nameof(PDbContext));
    }

    public static IServiceCollection AddReplicaDb(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDb<ReplicaDbContext>(configuration, nameof(ReplicaDbContext));
    }

    public static IServiceCollection AddReadDb(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDb<ReadDbContext>(configuration, nameof(ReadDbContext));
    }
    
    private static IServiceCollection AddDb<TDbContext>(this IServiceCollection services, 
        IConfiguration configuration, string configContextName) 
        where TDbContext : DbContext
    {
        services.AddDbContext<TDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(configContextName));
            options.UseSnakeCaseNamingConvention();
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        return services;
    }
}