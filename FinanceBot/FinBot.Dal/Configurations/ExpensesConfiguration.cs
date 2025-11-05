using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class ExpensesConfiguration : IEntityTypeConfiguration<Expenses>
{
    public void Configure(EntityTypeBuilder<Expenses> builder)
    {
        builder.Property(e => e.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(e => e.OccuredAt)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Budget)
            .WithMany()
            .HasForeignKey(e => e.BudgetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DailyAllocation)
            .WithMany()
            .HasForeignKey(e => e.AllocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}