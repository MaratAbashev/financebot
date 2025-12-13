using FinBot.Bll.Implementation.Requests;
using FinBot.Dal;
using FinBot.Dal.DbContexts;
using FinBot.WebApi.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var webHookUrl = configuration["Bot:WebhookUrl"]!;

builder.Host.UseSerilog((context, loggerConfig) => 
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341");
});

services.AddPostgresDb(configuration);
services.AddTelegram(configuration);

var app = builder.Build();

app.MapGet("/bot/set-webhook", async (ITelegramBotClient botClient) =>
{
    await botClient.SetWebhook(webHookUrl, dropPendingUpdates: true);
    return Results.Ok($"webhook set to {webHookUrl}");
});

app.MapPost("/bot", async (IMediator mediator, Update update, CancellationToken cancellationToken) =>
{
    await mediator.Send(new ProcessTelegramUpdateRequest(update), cancellationToken);
});

MigrateDatabase(app);

app.Run();
return;

static void MigrateDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var db = scope.ServiceProvider.GetRequiredService<PDbContext>();

    if (db.Database.GetPendingMigrations().Any())
    {
        db.Database.Migrate();
    }
}