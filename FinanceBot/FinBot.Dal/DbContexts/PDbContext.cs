using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FinBot.Dal.DbContexts;

public class PDbContext(DbContextOptions<PDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    // public DbSet<BudgetEvent> BudgetEvents { get; set; }
    public DbSet<DailyAllocation> DailyAllocations { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Saving> Savings { get; set; }
    // public DbSet<Transaction> Transactions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PDbContext).Assembly);
    }
}