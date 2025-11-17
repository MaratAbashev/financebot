using FinBot.Dal;
using FinBot.Dal.DbContexts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var congiguration = builder.Configuration;

services.AddPostgresDb(congiguration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

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