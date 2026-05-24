using FinBot.Dal.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FinBot.Bll.Tests.Infrastructure;

public static class TestDbFactory
{
    public static PDbContext Create()
    {
        var options = new DbContextOptionsBuilder<PDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PDbContext(options);
    }
}
