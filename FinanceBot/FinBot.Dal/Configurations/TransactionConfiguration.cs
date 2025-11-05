using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.Property(t => t.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(t => t.OccuredAt)
            .IsRequired();

        builder.HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Saving)
            .WithMany()
            .HasForeignKey(t => t.SavingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Performer)
            .WithMany()
            .HasForeignKey(t => t.PerformerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}