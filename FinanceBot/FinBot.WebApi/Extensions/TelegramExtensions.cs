using FinBot.Bll.Implementation.Handlers;
using FinBot.Bll.Implementation.Services;
using FinBot.WebApi.BackgroundServices;
using Telegram.Bot;

namespace FinBot.WebApi.Extensions;

public static class TelegramExtensions
{
    public static IServiceCollection AddTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        var token = configuration["Bot:Token"]!;
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<TelegramUpdateRequestHandler>();
        });
        services.AddScoped<ReceiverService>();
        services.AddStaticCommands();
        services.AddRegExpCommands();
        services.AddDialogs();
        services.AddHostedService<PollingService>();
        
        return services;
    }
}