using FinBot.Dal.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Dal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(nameof(PDbContext)));
            options.UseSnakeCaseNamingConvention();
        });

        return services;
    }
}