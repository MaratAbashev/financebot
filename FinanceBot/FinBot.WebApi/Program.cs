using System.Reflection;
using FinBot.Bll.implementation.Handlers;
using FinBot.Bll.implementation.Requests;
using FinBot.Bll.implementation.Requests.Commands.StaticCommands;
using FinBot.Bll.Interfaces.TelegramCommands;
using FinBot.Domain.Attributes;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);
var token = builder.Configuration["Bot:Token"]!;
var webHookUrl = builder.Configuration["Bot:WebhookUrl"]!;
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<TelegramUpdateRequestHandler>();
});
var commandTypes = typeof(StartCommand)
    .Assembly
    .GetTypes()
    .Where(t =>
        t.IsAssignableTo(typeof(IStaticCommand)));
foreach (var commandType in commandTypes)
{
    builder.Services.AddTransient(typeof(IStaticCommand), commandType);
}
builder.Services.AddSingleton<Dictionary<string, IStaticCommand>>(sp =>
{
    return sp
        .GetKeyedServices<IStaticCommand>(null)
        .Where(command => command.GetType().GetCustomAttribute<SlashCommandAttribute>() != null)
        .ToDictionary(k => k
            .GetType()
            .GetCustomAttribute<SlashCommandAttribute>()!.Command,
            v => v);
});

var app = builder.Build();

app.MapGet("/bot/set-webhook", async (ITelegramBotClient botClient) =>
{
    await botClient.SetWebhook(webHookUrl, dropPendingUpdates: true);
    return Results.Ok($"webhook set to {webHookUrl}");
});

app.MapPost("/bot", async (IMediator mediator, Update update) =>
{
    await mediator.Send(new ProcessTelegramUpdateRequest(update));
});

app.Run();