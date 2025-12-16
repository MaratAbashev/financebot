using FinBot.Bll.Implementation.Requests;
using FinBot.Dal;
using FinBot.Dal.DbContexts;
using FinBot.WebApi;
using FinBot.WebApi.Extensions;
using FinBot.WebApi.GroupJob;
using FinBot.WebApi.TestEndpoints;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Serilog;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var webHookUrl = configuration["Bot:WebhookUrl"]!;

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Filter.ByExcluding(logEvent => 
            logEvent.Properties.ContainsKey("RequestPath") && 
            logEvent.Properties["RequestPath"].ToString().Contains("/hf"))
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341");
});

services.AddPostgresDb(configuration);
services.AddTelegram(configuration);
services.AddBll(configuration);
services.AddHangfire(configuration);
services.AddOpenApi();

var app = builder.Build();

app.UseHangfireDashboard();

if (app.Environment.IsDevelopment())
{
    // Scalar
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
    
    // Hangfire
    app.MapHangfireDashboard("/hf", new DashboardOptions
    {
        Authorization = [new HangfireAllowAllAuthFilter()]
    });
    
    // Test endpoints
    app.MapUserEndpoints();
    app.MapGroupEndpoints();
    app.MapBackgroundEndpoints();
}

AddDailyJob(app);

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

static void AddDailyJob(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        var mskTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        recurringJobManager.AddOrUpdate<GroupJobDispatcher>(
            "main-group-dispatch-job",
            dispatcher => dispatcher.DispatchTasksAsync(),
            Cron.Daily(0, 0),
            new RecurringJobOptions
            {
                TimeZone = mskTimeZone
            }
        );
    }
}