using FinBot.Bll.Implementation.Handlers;
using FinBot.Domain.KafkaMessages;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Messages;
using FinBot.Kafka.Topics;

namespace FinBot.WebApi.Extensions;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKafka(settings =>
        {
            if(configuration["Kafka:BootstrapServers"] != null)
                settings.BootstrapServers = configuration["Kafka:BootstrapServers"]!;
        });

        services.AddProducerGeneral();
        services.AddProducer<AiAnalyseMessage, LlmTopic>();
        services.AddConsumer<AiAnalyseCompletedMessage, AiCompletedMessageHandler ,ApiLlmTopic>(configuration["Kafka:LlmApiGroupId"]!);
        services.AddProducer<ExcelMessage, ExcelTopic>();
        services.AddConsumer<ExcelCompletedMessage, ExcelCompletedMessageHandler, ApiExcelTopic>(configuration["Kafka:ExcelApiGroupId"]!);
        return services;
    }
}