using Microsoft.AspNetCore.Http.HttpResults;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);
var token = builder.Configuration["Bot:Token"]!;
var webHookUrl = builder.Configuration["Bot:WebhookUrl"]!;
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
var app = builder.Build();

app.MapGet("/bot/set-webhook", async (ITelegramBotClient botClient) =>
{
    await botClient.SetWebhook(webHookUrl);
    return Results.Ok($"webhook set to {webHookUrl}");
});

app.MapPost("/bot", async (ITelegramBotClient botClient, Update update) =>
{
    if (update.Message is null) return; 
    if (update.Message.Text is null) return;
    var msg = update.Message;
    await botClient.SendMessage(msg.Chat, $"{msg.From} said: {msg.Text}");
});

app.Run();