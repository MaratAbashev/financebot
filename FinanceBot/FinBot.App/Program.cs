using System.Text.Json.Serialization;
using FinBot.App;
using FinBot.App.Endpoints;
using FinBot.App.Extensions;
using FinBot.App.GroupJob;
using FinBot.Cache;
using FinBot.Dal;
using FinBot.Dal.DbContexts;
using FinBot.Observability;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddObservability(configuration);

// игнорировать циклы при возврате json
services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

services
    .AddPostgresDb(configuration)
    .AddBll(configuration)
    .AddOpenApi()
    .AddRedisCacheIntegration(configuration);

if (!builder.Environment.IsEnvironment("Testing"))
    services.AddHangfire(configuration);

var app = builder.Build();

app.UseObservability();

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHangfireDashboard();

app.MapUserEndpoints();
app.MapGroupEndpoints();
app.MapBackgroundEndpoints();
app.MapExpenseEndpoints();
app.MapInvitationEndpoints();
app.MapInternalBankEndpoints();

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
}

app.MapGet("/", () => "Hello World!");

if (!app.Environment.IsEnvironment("Testing"))
{
    AddDailyJob(app);
    await MigrateDatabase(app);
}

app.Run();
return;

static async Task MigrateDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    await using var db = scope.ServiceProvider.GetRequiredService<PDbContext>();

    if (db.Database.GetPendingMigrations().Any())
    {
        db.Database.Migrate();
        await DebeziumSetup.SetupDebeziumPrivileges(app.Configuration.GetConnectionString(nameof(PDbContext)) ??
                                                    throw new Exception("Connection string for database is null"));
    }
}

static void AddDailyJob(WebApplication app)
{
    using var scope = app.Services.CreateScope();
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

public partial class Program;