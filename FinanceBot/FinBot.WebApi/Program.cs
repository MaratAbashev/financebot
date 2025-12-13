using FinBot.Bll.Implementation.Handlers;
using FinBot.Bll.Implementation.Requests;
using FinBot.Bll.Interfaces;
using FinBot.Dal;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.WebApi.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

using FinBot.Dal;
using FinBot.Dal.DbContexts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var congiguration = builder.Configuration;

services.AddPostgresDb(congiguration);

var token = builder.Configuration["Bot:Token"]!;
var webHookUrl = builder.Configuration["Bot:WebhookUrl"]!;
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<TelegramUpdateRequestHandler>();
});

builder.Services.AddStaticCommands();
builder.Services.AddRegExpCommands();
builder.Services.AddDialogs();

builder.Services.AddDbContext<PDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("FinbotDb"));
});
builder.Services
    .AddScoped<IGenericRepository<DialogContext, int, PDbContext>, GenericRepository<DialogContext, int, PDbContext>>();

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