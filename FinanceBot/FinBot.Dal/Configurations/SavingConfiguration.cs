using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class SavingConfiguration : IEntityTypeConfiguration<Saving>
{
    public void Configure(EntityTypeBuilder<Saving> builder)
    {
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.TargetAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.CurrentAmount)
            .HasColumnType("numeric(18,2)");

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasOne(s => s.Owner)
            .WithMany()
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Group)
            .WithMany()
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}