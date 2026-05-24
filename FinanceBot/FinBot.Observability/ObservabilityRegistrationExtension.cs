using FinBot.Observability.Constants;
using FinBot.Observability.HealthChecks;
using FinBot.Observability.Logging;
using FinBot.Observability.Metrics;
using FinBot.Observability.Middleware;
using FinBot.Observability.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using Serilog;

namespace FinBot.Observability;

public static class ObservabilityRegistrationExtension
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = ObservabilityConstants.ConfigurationSectionName)
    {
        var section = configuration.GetSection(sectionName);
        services.Configure<ObservabilityOptions>(section);

        var options = section.Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        services.AddHttpContextAccessor();
        services.AddSingleton<EndpointEnricher>();
        services.AddSingleton<BusinessMetrics>();

        services.AddSerilog((serviceProvider, loggerConfiguration) =>
        {
            SerilogConfiguration.Configure(loggerConfiguration, configuration, serviceProvider, options);
        });

        var otelBuilder = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion,
                    serviceInstanceId: Environment.GetEnvironmentVariable("HOSTNAME")
                                       ?? Environment.GetEnvironmentVariable("POD_NAME")
                                       ?? Environment.MachineName));

        if (options.EnableTracing)
        {
            otelBuilder.WithTracing(tracing => tracing.AddFinBotTracing(options));
        }

        if (options.EnableMetrics)
        {
            otelBuilder.WithMetrics(metrics => metrics.AddFinBotMetrics(options));
        }

        services.AddFinBotHealthChecks(configuration);

        return services;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<RequestEnrichmentMiddleware>();

        var options = app.Services.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        if (options.ExposePrometheusEndpoint)
        {
            app.MapPrometheusScrapingEndpoint();
        }

        app.MapFinBotHealthChecks();

        return app;
    }

    public static IHost UseObservability(this IHost host) => host;
}