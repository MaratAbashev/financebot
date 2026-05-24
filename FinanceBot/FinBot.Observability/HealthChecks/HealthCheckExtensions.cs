using FinBot.Observability.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinBot.Observability.HealthChecks;

internal static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddFinBotHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<MetricsHealthCheckPublisher>();
        services.AddSingleton<IHealthCheckPublisher>(sp =>
            sp.GetRequiredService<MetricsHealthCheckPublisher>());
        services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromSeconds(5);
            options.Period = TimeSpan.FromSeconds(10);
        });

        var builder = services.AddHealthChecks();

        var pgConnection = configuration.GetConnectionString("PDbContext");
        if (!string.IsNullOrWhiteSpace(pgConnection))
        {
            builder.AddNpgSql(pgConnection, name: "postgres", tags: [ReadyTag]);
        }

        var redisConnection = configuration["App:Redis:RedisCacheConnection"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            builder.AddRedis(redisConnection, name: "redis", tags: [ReadyTag]);
        }

        var kafkaBootstrap = configuration["Kafka:BootstrapServers"];
        if (!string.IsNullOrWhiteSpace(kafkaBootstrap))
        {
            builder.AddKafka(
                setup: p => p.BootstrapServers = kafkaBootstrap,
                name: "kafka",
                tags: [ReadyTag]);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapFinBotHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(ObservabilityConstants.HealthEndpoints.Health);

        endpoints.MapHealthChecks(ObservabilityConstants.HealthEndpoints.Live, new HealthCheckOptions
        {
            Predicate = _ => false
        });

        endpoints.MapHealthChecks(ObservabilityConstants.HealthEndpoints.Ready, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag)
        });

        return endpoints;
    }
}