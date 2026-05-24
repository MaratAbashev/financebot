using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FinBot.Kafka.DLQ;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Tests.TestEnvironment;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Kafka;

namespace FinBot.Kafka.Tests;

public class ConsumerTests
{
    private readonly TestTopic _topic = new ();
    private readonly TopicResponse _topicResponse = new ();
    private readonly DlqTopic _dlqTopic = new ();
    private readonly string _groupId = "1";
    private readonly string _responseGroupId = "2";

    [Fact]
    public async Task ConsumerBackgroundService_ShouldHandleMessage()
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
        services.AddConsumer<TestMessage, TestMessageHandler, TestTopic>(_groupId);
        
        var host = builder.Build();
        await host.StartAsync();
        var serviceProvider = host.Services;

        var initManager = serviceProvider.GetRequiredService<BackgroundServicesInitManager>();
        while (initManager.IsTopicsCreated != TopicCreationStatus.Created)
        {
            await Task.Delay(100);
        }
        var producer = new ProducerBuilder<Null, TestMessage>(new ProducerConfig
        {
            BootstrapServers = bootstrapAddress
        })
            .SetValueSerializer(new JsonSerializer<TestMessage>())
            .Build();
        
        var message = new TestMessage { Body = "Test" };
        await producer.ProduceAsync(_topic.TopicName, 
            new Message<Null, TestMessage> {Value = message});
        
        await Task.Delay(3000);
        var handler = serviceProvider.GetRequiredService<TestMessageHandler>();
        
        await kafkaContainer.DisposeAsync();
        
        Assert.NotNull(handler.Message);
        Assert.Equal(message.Body, handler.Message?.Body);
    }
    
    [Fact]
    public async Task TransactionalConsumerBackgroundService_ShouldHandleMessageAtomically()
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
        services.AddTransactionConsumer<TestMessage, TestTransactionalMessageHandler, TestTopic>(_groupId);
        services.AddProducer<TestMessage, TopicResponse>();
        
        var host = builder.Build();
        await host.StartAsync();

        var serviceProvider = host.Services;
        
        var initManager = serviceProvider.GetRequiredService<BackgroundServicesInitManager>();
        while (initManager.IsTopicsCreated != TopicCreationStatus.Created)
        {
            await Task.Delay(100);
        }
        
        var producer = new ProducerBuilder<Null, TestMessage>(new ProducerConfig
            {
                BootstrapServers = bootstrapAddress
            })
            .SetValueSerializer(new JsonSerializer<TestMessage>())
            .Build();
        var consumer = new ConsumerBuilder<Null,TestMessage>(
                new ConsumerConfig
                {
                    BootstrapServers = bootstrapAddress,
                    GroupId = _responseGroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                })
            .SetValueDeserializer(new JsonDeserializer<TestMessage>())
            .Build();
        consumer.Subscribe([_topicResponse.TopicName]);
        
        var message = new TestMessage { Body = "Test" };
        await producer.ProduceAsync(_topic.TopicName, 
            new Message<Null, TestMessage> {Value = message});
        
        var receivedMessages = new List<TestMessage>();
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
        Assert.Equal(message.Body.ToUpper(), receivedMessages.FirstOrDefault()?.Body);
    }
    
    [Fact]
    public async Task ConsumerBackgroundService_WithErrorMessageHandle_ShouldRetryAndPublishToDlq()
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
        services.AddConsumer<TestMessage,TestErrorMessageHandler, TestTopic>(_groupId, config =>
        {
            config.EnableDeadLetterQueue = true;
            config.RetryDelay = TimeSpan.FromSeconds(1);
            config.MaxRetryCount = 3;
        });
        
        var host = builder.Build();
        await host.StartAsync();
        var serviceProvider = host.Services;
        
        var initManager = serviceProvider.GetRequiredService<BackgroundServicesInitManager>();
        while (initManager.IsTopicsCreated != TopicCreationStatus.Created)
        {
            await Task.Delay(100);
        }
        
        var producer = new ProducerBuilder<Null, TestMessage>(new ProducerConfig
            {
                BootstrapServers = bootstrapAddress
            })
            .SetValueSerializer(new JsonSerializer<TestMessage>())
            .Build();
        
        var dlqConsumer = new ConsumerBuilder<Null, DlqMessage>(
                new ConsumerConfig
                {
                    BootstrapServers = bootstrapAddress,
                    GroupId = _responseGroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                })
            .SetValueDeserializer(new JsonDeserializer<DlqMessage>())
            .Build();
        dlqConsumer.Subscribe(_dlqTopic.TopicName);
        
        var message = new TestMessage { Body = "Test" };
        await producer.ProduceAsync(_topic.TopicName, 
            new Message<Null, TestMessage> {Value = message});
        
        var receivedMessages = new List<DlqMessage?>();
        var attempts = 3;
        while (attempts-- > 0)
        {
            var consumeResult = dlqConsumer.Consume(TimeSpan.FromSeconds(3));
            if (consumeResult != null)
            {
                receivedMessages.Add(consumeResult?.Message?.Value);
                dlqConsumer.Commit(consumeResult);
            }
        }
        
        var handler = serviceProvider.GetRequiredService<TestErrorMessageHandler>();
        
        await kafkaContainer.DisposeAsync();

        var messageFromDlq = JsonSerializer.Deserialize<TestMessage>(
            receivedMessages.FirstOrDefault()?.Value ?? "{}");
        
        Assert.Single(receivedMessages);
        Assert.NotNull(receivedMessages.FirstOrDefault());
        Assert.Equal(message.Body, messageFromDlq?.Body);
        Assert.Equal(3, handler.InvokedCount);
    }
    
    [Fact]
    public async Task TransactionalConsumerBackgroundService_WithErrorMessageHandle_ShouldRetryAndPublishToDlq()
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
        services.AddTransactionConsumer<TestMessage,TestErrorTransactionalMessageHandler, TestTopic>(_groupId, config =>
        {
            config.EnableDeadLetterQueue = true;
            config.RetryDelay = TimeSpan.FromSeconds(1);
            config.MaxRetryCount = 3;
        });
        
        var host = builder.Build();
        await host.StartAsync();
        var serviceProvider = host.Services;
        
        var initManager = serviceProvider.GetRequiredService<BackgroundServicesInitManager>();
        while (initManager.IsTopicsCreated != TopicCreationStatus.Created)
        {
            await Task.Delay(100);
        }
        
        var producer = new ProducerBuilder<Null, TestMessage>(new ProducerConfig
            {
                BootstrapServers = bootstrapAddress
            })
            .SetValueSerializer(new JsonSerializer<TestMessage>())
            .Build();
        
        var dlqConsumer = new ConsumerBuilder<Null, DlqMessage>(
                new ConsumerConfig
                {
                    BootstrapServers = bootstrapAddress,
                    GroupId = _responseGroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                })
            .SetValueDeserializer(new JsonDeserializer<DlqMessage>())
            .Build();
        dlqConsumer.Subscribe(_dlqTopic.TopicName);
        
        var message = new TestMessage { Body = "Test" };
        await producer.ProduceAsync(_topic.TopicName, 
            new Message<Null, TestMessage> {Value = message});
        
        var receivedMessages = new List<DlqMessage?>();
        var attempts = 3;
        while (attempts-- > 0)
        {
            var consumeResult = dlqConsumer.Consume(TimeSpan.FromSeconds(3));
            if (consumeResult != null)
            {
                receivedMessages.Add(consumeResult?.Message?.Value);
                dlqConsumer.Commit(consumeResult);
            }
        }
        
        var handler = serviceProvider.GetRequiredService<TestErrorTransactionalMessageHandler>();
        
        await kafkaContainer.DisposeAsync();

        var messageFromDlq = JsonSerializer.Deserialize<TestMessage>(
            receivedMessages.FirstOrDefault()?.Value ?? "{}");
        
        Assert.Single(receivedMessages);
        Assert.NotNull(receivedMessages.FirstOrDefault());
        Assert.Equal(message.Body, messageFromDlq?.Body);
        Assert.Equal(3, handler.InvokedCount);
    }
}