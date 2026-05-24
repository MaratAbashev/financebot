using FinBot.Bll.Interfaces.Integration;
using FinBot.Integrations.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Integrations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IReportProducer, KafkaProducer>();

        return services;
    }
}