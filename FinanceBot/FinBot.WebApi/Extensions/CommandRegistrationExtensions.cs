using System.Reflection;
using FinBot.Bll.Implementation.Commands.StaticCommands;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;

namespace FinBot.WebApi.Extensions;

public static class CommandRegistrationExtensions
{
    public static IServiceCollection AddStaticCommands(this IServiceCollection services)
    {
        var commandTypes = typeof(StartCommand)
            .Assembly
            .GetTypes()
            .Where(t =>
                t.IsAssignableTo(typeof(IStaticCommand)));
        foreach (var commandType in commandTypes)
        {
            services.AddTransient(typeof(IStaticCommand), commandType);
        }
        services.AddSingleton<Dictionary<string, IStaticCommand>>(sp =>
        {
            var staticCommandMap = sp
                .GetKeyedServices<IStaticCommand>(null)
                .Where(command => command.GetType().GetCustomAttribute<SlashCommandAttribute>() != null)
                .ToDictionary(k => k
                        .GetType()
                        .GetCustomAttribute<SlashCommandAttribute>()!.Command,
                    v => v);
            foreach(var staticCommand in sp.GetKeyedServices<IStaticCommand>(null)
                        .Where(command => command.GetType().GetCustomAttribute<TextCommandAttribute>() != null
                                          && !command.GetType().GetCustomAttribute<TextCommandAttribute>()!.IsRegularExpression))
            {
                staticCommandMap.Add(
                    staticCommand
                        .GetType()
                        .GetCustomAttribute<TextCommandAttribute>()!
                        .TextCommand, staticCommand);
            }    return staticCommandMap;
        });

        return services;
    }

    public static IServiceCollection AddRegExpCommands(this IServiceCollection services)
    {
        var regExpTypes = typeof(StartCommand)
            .Assembly
            .GetTypes()
            .Where(t =>
                t.IsAssignableTo(typeof(IRegExpCommand)));
        foreach (var commandType in regExpTypes)
        {
            services.AddTransient(typeof(IRegExpCommand), commandType);
        }
        services.AddSingleton<Dictionary<string, IRegExpCommand>>(sp =>
        {
            return sp
                .GetKeyedServices<IRegExpCommand>(null)
                .Where(command => command.GetType().GetCustomAttribute<TextCommandAttribute>() != null
                                  && command.GetType().GetCustomAttribute<TextCommandAttribute>()!.IsRegularExpression)
                .ToDictionary(k => k
                        .GetType()
                        .GetCustomAttribute<TextCommandAttribute>()!
                        .TextCommand,
                    v => v);
        });
        return services;
    }
}