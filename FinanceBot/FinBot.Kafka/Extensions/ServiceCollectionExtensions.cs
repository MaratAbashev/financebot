using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.BackgroundServices;
using FinBot.Kafka.BackgroundServices.Base;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.DLQ;
using FinBot.Kafka.Impl.Providers;
using FinBot.Kafka.Internal.DI;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
    private static Dictionary<string, int> _topicRegistrations = new();
    /// <summary>
    /// Добавить глобальные настройки для кафки
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddKafka(
        this IServiceCollection serviceCollection,
        Action<KafkaGlobalSettings> configure)
    {
        var settings = new KafkaGlobalSettings();
        configure(settings);
        serviceCollection.AddSingleton(settings);
        var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = settings.BootstrapServers }
        ).Build();
        serviceCollection.AddSingleton(adminClient);
        serviceCollection.AddSingleton<BackgroundServicesInitManager>();
        serviceCollection.AddHostedService<KafkaTopicsService>();
        return serviceCollection;
    }
    
    /// <summary>
    /// Добавить глобальные настройки для продюсеров, вызывать всегда, так как консьюмеры используют DLQ
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddProducerGeneral(
        this IServiceCollection serviceCollection,
        Action<ProducerSettingsGeneral>? configure = null)
    {
        var settings = new ProducerSettingsGeneral();
        if (configure is not null)
        {
            configure(settings);
        }
        serviceCollection.AddSingleton(settings);
        var config = new ProducerConfig();
        serviceCollection.AddSingleton(sp =>
        {
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
           
            return new ProducerBuilder<byte[]?, byte[]>(
                    config.FromProducerSettingsGeneral(settings, globalSettings))
                .Build();
        });
        serviceCollection.AddSingleton<IAsyncProducerProvider,DefaultProducerProvider>();
        serviceCollection.AddScoped<IProducerContext, ProducerContext>();
        
        return serviceCollection;
    }
    
    /// <summary>
    /// Добавить продюсера
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="overrideConfigure">переопределяющая настройка для продюсера</param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TTopic"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddProducer<TKey, TValue, TTopic>(
        this IServiceCollection serviceCollection,
        Action<ProducerSettingsGeneral>? overrideConfigure = null) 
        where TTopic : class, ITopic
    {
        var producerSettings = new ProducerSettings<TKey, TValue, TTopic>();
        serviceCollection.AddSingleton<TTopic>();
        serviceCollection.AddSingleton<ITopic, TTopic>();
        serviceCollection.AddSingleton<RegistrationProducer<TKey, TValue, TTopic>>(sp =>
            {
                var producer = sp.GetRequiredService<IProducer<byte[]?, byte[]>>();
                var generalSettings = sp.GetRequiredService<ProducerSettingsGeneral>();
                var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
                var topic = sp.GetRequiredService<TTopic>();
                _topicRegistrations[topic.TopicName] = 
                    _topicRegistrations.GetValueOrDefault(topic.TopicName,0) + 1;
                if (_topicRegistrations[topic.TopicName] < 2)
                {
                    var adminClient = sp.GetRequiredService<IAdminClient>();
                }
                producerSettings.Topic = topic;
                if (overrideConfigure is not null)
                {
                    var newGeneralSettings = new ProducerSettingsGeneral();
                    overrideConfigure(newGeneralSettings);
                    generalSettings = newGeneralSettings;
                    var config = new ProducerConfig();
                    producer = new ProducerBuilder<byte[]?, byte[]>(
                            config.FromProducerSettingsGeneral(generalSettings, globalSettings))
                        .Build();
                }

                producerSettings.GeneralSettings = generalSettings;
                
                return new RegistrationProducer<TKey, TValue, TTopic>
                {
                    Settings = producerSettings,
                    Producer = producer
                };
            });
        
        return serviceCollection;
    }

    /// <summary>
    /// Добавить продюсера
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="overrideConfigure">переопределяющая настройка для продюсера</param>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TTopic"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddProducer<TValue, TTopic>(
        this IServiceCollection serviceCollection,
        Action<ProducerSettingsGeneral>? overrideConfigure = null)
        where TTopic : class, ITopic
    {
        return serviceCollection.AddProducer<Null, TValue, TTopic>(overrideConfigure);
    }

    /// <summary>
    /// Добавить консьюмера
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="groupId">группа консьюмера</param>
    /// <param name="configure">конфигурация консьюмера</param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="THandler">обработчик сообщений</typeparam>
    /// <typeparam name="TTopic">топик на который подписан</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddConsumer<TKey,TValue, THandler, TTopic>(
        this IServiceCollection serviceCollection,
        string groupId,
        Action<ConsumerSettings>? configure = null) 
        where THandler: class, IMessageHandler<TKey,TValue> 
        where TTopic : class, ITopic
    {
        return serviceCollection.AddConsumer<TKey, TValue, THandler, 
                DefaultConsumerService<TKey,TValue,THandler>, TTopic>
            (groupId, configure);
    }
    
    /// <summary>
    /// Добавить консьюмера
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="groupId">группа консьюмера</param>
    /// <param name="configure">конфигурация консьюмера</param>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="THandler">обработчик сообщений</typeparam>
    /// <typeparam name="TTopic">топик на который подписан</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddConsumer<TValue, THandler, TTopic>(
        this IServiceCollection serviceCollection,
        string groupId,
        Action<ConsumerSettings>? configure = null) 
        where THandler: class, IMessageHandler<TValue> 
        where TTopic : class, ITopic
    {
        return serviceCollection.AddConsumer<Null, TValue, THandler, 
                DefaultConsumerService<TValue,THandler>, TTopic>
            (groupId, configure);
    }
    
    /// <summary>
    /// Добавить консьюмера для сценария "получил-отправил" в одной транзакции
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="groupId">группа консьюмера</param>
    /// <param name="configure">конфигурация консьюмера</param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="THandler">обработчик сообщений</typeparam>
    /// <typeparam name="TTopic">топик на который подписан</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddTransactionConsumer<TKey,TValue, THandler, TTopic>(
        this IServiceCollection serviceCollection,
        string groupId,
        Action<ConsumerSettings>? configure = null) 
        where THandler: class, ITransactionMessageHandler<TKey,TValue> 
        where TTopic : class, ITopic
    {
        return serviceCollection.AddConsumer<TKey, TValue, THandler, 
                TransactionalConsumerService<TKey,TValue, THandler>, TTopic>
            (groupId, configure);
    }

    /// <summary>
    /// Добавить консьюмера для сценария "получил-отправил" в одной транзакции
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="groupId">группа консьюмера</param>
    /// <param name="configure">конфигурация консьюмера</param>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="THandler">обработчик сообщений</typeparam>
    /// <typeparam name="TTopic">топик на который подписан</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddTransactionConsumer<TValue, THandler, TTopic>(
        this IServiceCollection serviceCollection,
        string groupId,
        Action<ConsumerSettings>? configure = null) 
        where THandler: class, ITransactionMessageHandler<TValue> 
        where TTopic : class, ITopic
    {
        return serviceCollection.AddConsumer<Null, TValue, THandler, 
                TransactionalConsumerService<TValue, THandler>, TTopic>
            (groupId, configure);
    }
    
    private static IServiceCollection AddConsumer<TKey,TValue, THandler, TService, TTopic>(
        this IServiceCollection serviceCollection,
        string groupId,
        Action<ConsumerSettings>? configure) 
        where THandler: class 
        where TService : ConsumerServiceBase<TKey,TValue> 
        where TTopic : class, ITopic
    {
        var config = new ConsumerSettings
        {
            GroupId = groupId
        };
        if (configure is not null)
        {
            configure(config);
        }
        serviceCollection.AddSingleton<THandler>();
        serviceCollection.AddSingleton<ITopic, TTopic>();
        serviceCollection.AddSingleton<TTopic>();
        serviceCollection.AddSingleton<RegistrationConsumer<TKey, TValue>>(sp =>
        {
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
            var topic = sp.GetRequiredService<TTopic>();
            
            var consumer = new ConsumerBuilder<TKey, TValue>(
                    new ConsumerConfig().FromConsumerSettings(config, globalSettings))
                .SetKeyDeserializer(new JsonDeserializer<TKey>())
                .SetValueDeserializer(new JsonDeserializer<TValue>())
                .Build();
            
            config.Topics.Add(topic);
            
            return new RegistrationConsumer<TKey, TValue>
            {
                Settings = config,
                Consumer = consumer
            };
        });
        
        serviceCollection.AddProducer<DlqMessage, DlqTopic>();

        if (config.EnableDeadLetterQueue)
        {
            serviceCollection.AddSingleton<ITopic, DlqTopic>();
        }
        
        serviceCollection.AddHostedService<TService>();

        return serviceCollection;
    }
}