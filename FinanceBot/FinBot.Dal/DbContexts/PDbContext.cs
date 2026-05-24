using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.Dal.DbContexts;

public class PDbContext : DbContext
{
    public PDbContext(DbContextOptions<PDbContext> options) : base(options)
    {
    }

    protected PDbContext(DbContextOptions options) : base(options)
    {
        
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Saving> Savings { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<JoinRequest> JoinRequests { get; set; }
    public DbSet<BankConnection> BankConnections { set; get; }
    public DbSet<BankTransaction> BankTransactions { set; get; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PDbContext).Assembly);
    }
}