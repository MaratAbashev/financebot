using FinBot.BankService.BankingApi;
using FinBot.BankService.Cache;
using FinBot.BankService.Consumers;
using FinBot.BankService.Hangfire;
using FinBot.BankService.Repositories;
using FinBot.BankService.Services;
using FinBot.Cache;
using FinBot.Dal;
using FinBot.Dal.DbContexts;
using FinBot.Integrations;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Messages;
using FinBot.Kafka.Topics;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FinBot.BankService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBankServicePipeline(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DB
        services.AddPostgresDb(configuration);

        // Redis
        services.AddRedisCacheIntegration(configuration);
        services.AddSingleton<ITokenCache, TokenCache>();

        // Banking API client
        services.Configure<BankingApiOptions>(configuration.GetSection("BankingApi"));
        services.AddHttpClient<IBankingApiClient, BankingApiClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<BankingApiOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
        });

        // Repositories
        services.AddScoped<IBankConnectionRepository, BankConnectionRepository>();
        services.AddScoped<IBankTransactionRepository, BankTransactionRepository>();
        services.AddScoped<IExpenseWriteRepository, ExpenseWriteRepository>();

        // Services
        services.AddScoped<IBankAuthService, BankAuthService>();
        services.AddScoped<IBankSyncService, BankSyncService>();

        // Kafka
        services.AddKafka(settings =>
        {
            if (configuration["Kafka:BootstrapServers"] != null)
                settings.BootstrapServers = configuration["Kafka:BootstrapServers"]!;
        });
        services.AddProducerGeneral();
        services.AddProducer<BankSyncCompletedMessage, ApiBankTopic>();
        services.AddTransactionConsumer<BankSyncMessage, BankSyncRequestHandler, BankTopic>(
            configuration["Kafka:BankGroupId"]!);

        // Hangfire
        services.AddHangfire(config => config
            .UsePostgreSqlStorage(
                configuration.GetConnectionString(nameof(PDbContext)),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire_bank"
                }));
        services.AddHangfireServer();
        services.AddScoped<BankSyncJob>();

        // TimeProvider
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}