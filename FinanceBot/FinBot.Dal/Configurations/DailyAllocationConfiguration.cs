using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class DailyAllocationConfiguration : IEntityTypeConfiguration<DailyAllocation>
{
    public void Configure(EntityTypeBuilder<DailyAllocation> builder)
    {
        builder.Property(d => d.Date)
            .IsRequired();
        builder.Property(d => d.Allocated)
            .HasColumnType("numeric(18,2)")
            .IsRequired();
        builder.Property(d => d.Spent)
            .HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(d => d.Leftover)
            .HasColumnType("numeric(18,2)").IsRequired();

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Budget)
            .WithMany()
            .HasForeignKey(d => d.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.UserId, d.Date })
            .IsUnique(false);
    }
}