using FinBot.Dal.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinBot.Bll.Tests.Infrastructure;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PDbContext"] = "Host=test;Database=test;Username=test;Password=test",
                ["App:Redis:RedisCacheConnection"] = "localhost:6379",
                ["App:Redis:RedisCachePrefix"] = "test:",
                ["Seq:ServerUrl"] = "http://localhost:5341",
            });
        });

        builder.ConfigureServices(services =>
        {
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(PDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<PDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false))
                .ToList();
            foreach (var d in efDescriptors)
                services.Remove(d);

            services.AddDbContext<PDbContext>(o => o
                .UseInMemoryDatabase(_dbName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    public async Task SeedAsync(Action<PDbContext> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    public async Task UsingDbAsync(Func<PDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PDbContext>();
        await action(db);
    }
}