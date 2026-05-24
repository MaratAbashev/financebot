using FinBot.Cache;
using FinBot.Dal;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Messages;
using FinBot.Kafka.Topics;
using FinBot.Llm;
using FinBot.Llm.Services;
using FinBot.Observability;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservability(builder.Configuration);

builder.Services.AddRedisCacheIntegration(builder.Configuration);

builder.Services.AddKafka(settings =>
{
    if (builder.Configuration["Kafka:BootstrapServers"] != null)
        settings.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]!;
});
builder.Services.AddProducerGeneral();
builder.Services.AddProducer<AiAnalyseCompletedMessage, ApiLlmTopic>();
builder.Services.AddTransactionConsumer<AiAnalyseMessage, AiAnalyseRequestHandler, LlmTopic>(builder.Configuration["Kafka:LlmGroupId"]!);

builder.Services.AddReadDb(builder.Configuration);

builder.Services.AddLlmIntegration(builder.Configuration);

var host = builder.Build();
host.Run();