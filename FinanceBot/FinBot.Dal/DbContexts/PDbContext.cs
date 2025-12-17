using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.Dal.DbContexts;

public class PDbContext(DbContextOptions<PDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Saving> Savings { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<DialogContext> Dialogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PDbContext).Assembly);
    }
}