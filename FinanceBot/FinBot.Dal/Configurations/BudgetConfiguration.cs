using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.Property(b => b.AllocationValue)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.HasOne(b => b.Owner)
            .WithMany()
            .HasForeignKey(b => b.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Group)
            .WithMany()
            .HasForeignKey(b => b.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Account)
            .WithMany()
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}