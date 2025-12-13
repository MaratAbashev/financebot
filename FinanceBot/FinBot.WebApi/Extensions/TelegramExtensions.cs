using FinBot.Bll.Implementation.Handlers;
using Telegram.Bot;

namespace FinBot.WebApi.Extensions;

public static class TelegramExtensions
{
    public static IServiceCollection AddTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        var token = configuration["Bot:Token"]!;
        var webHookUrl = configuration["Bot:WebhookUrl"]!;
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<TelegramUpdateRequestHandler>();
        });

        services.AddStaticCommands();
        services.AddRegExpCommands();
        services.AddDialogs();
        
        return services;
    }
}