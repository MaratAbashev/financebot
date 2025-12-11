using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.Dal.DbContexts;

public class PDbContext(DbContextOptions<PDbContext> options) : DbContext(options)
{
    // public DbSet<BudgetEvent> BudgetEvents { get; set; }
    public DbSet<DialogContext> Dialogs { get; set; }
    // public DbSet<Transaction> Transactions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PDbContext).Assembly);
    }
}