using FinBot.Bll.Implementation.Services;
using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.WebApi.GroupJob;
using Hangfire;
using Hangfire.PostgreSql;

namespace FinBot.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBll(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IGroupBackgroundService, GroupBackgroundService>();
        services.AddScoped<IIntegrationsService, IntegrationService>();
        
        return services;
    }

    public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<GroupJobWorker>();
        services.AddTransient<GroupJobDispatcher>();
        
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString(nameof(PDbContext))))
        );
        
        services.AddHangfireServer();
        
        return services;
    }
}