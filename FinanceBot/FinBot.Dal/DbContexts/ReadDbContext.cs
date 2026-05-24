using Microsoft.EntityFrameworkCore;

namespace FinBot.Dal.DbContexts;

public class ReadDbContext(DbContextOptions<ReadDbContext> options) : PDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //TODO: навесить индексы
    }
}