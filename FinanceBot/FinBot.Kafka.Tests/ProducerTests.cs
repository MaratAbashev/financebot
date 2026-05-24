using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Tests.TestEnvironment;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Kafka;

namespace FinBot.Kafka.Tests;

public class ProducerTests
{
    private readonly TestTopic _topic = new ();
    private readonly string _groupId = "1";
    
    [Fact]
    public async Task Producer_ShouldSendMessage()
    {
        var kafkaContainer = new KafkaBuilder("confluentinc/cp-kafka:7.4.0")
            .Build();
        
        await kafkaContainer.StartAsync();

        var bootstrapAddress = kafkaContainer.GetBootstrapAddress();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        
        services.AddKafka(config =>
        {
            config.BootstrapServers = bootstrapAddress;
            config.PartitionNums = 1;
            config.PartitionReplicationFactor = 1;
        });
        services.AddProducerGeneral();
        services.AddProducer<TestMessage, TestTopic>();
        services.AddSingleton<TestService>();
        
        var host = builder.Build();
        await host.StartAsync();
        var serviceProvider = host.Services;
        
        var initManager = serviceProvider.GetRequiredService<BackgroundServicesInitManager>();
        while (initManager.IsTopicsCreated != TopicCreationStatus.Created)
        {
            await Task.Delay(100);
        }
        
        var consumer = new ConsumerBuilder<Null,TestMessage>(
                new ConsumerConfig
                {
                    BootstrapServers = bootstrapAddress,
                    GroupId = _groupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                })
            .SetValueDeserializer(new JsonDeserializer<TestMessage>())
            .Build();
        
        consumer.Subscribe([_topic.TopicName]);
        
        var receivedMessages = new List<TestMessage>();
        var service = serviceProvider.GetService<TestService>()!;
        var message = new TestMessage { Body = "Test" };
        await service.ProduceMessage(message);
        var attempts = 3;
        while (attempts-- > 0)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(3));
            if (consumeResult != null)
            {
                receivedMessages.Add(consumeResult.Message.Value);
                consumer.Commit(consumeResult);
            }
        }

        await kafkaContainer.DisposeAsync();
        
        Assert.Single(receivedMessages);
        Assert.Equal(message.Body, receivedMessages.FirstOrDefault()?.Body);
    }
}