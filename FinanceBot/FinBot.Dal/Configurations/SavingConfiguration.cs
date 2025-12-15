using FinBot.Domain.Models.SavingModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class SavingConfiguration : IEntityTypeConfiguration<Saving>
{
    public void Configure(EntityTypeBuilder<Saving> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.TargetAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.CurrentAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)); // Для PostgreSQL важно явно указывать UTC

        builder.HasOne(s => s.Group)
            .WithOne(g => g.Saving)
            .HasForeignKey<Saving>(s => s.GroupId);
    }
}