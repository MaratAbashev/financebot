using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class BudgetEventConfiguration : IEntityTypeConfiguration<BudgetEvent>
{
    public void Configure(EntityTypeBuilder<BudgetEvent> builder)
    {
        builder.Property(be => be.OccuredAt)
            .IsRequired();

        builder.Property(be => be.Event)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(be => be.Performer)
            .WithMany()
            .HasForeignKey(be => be.PerformerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(be => be.Budget)
            .WithMany()
            .HasForeignKey(be => be.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}