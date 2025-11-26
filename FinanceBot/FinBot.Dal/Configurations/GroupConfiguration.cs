using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(g => g.AllocationStrategy)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.OwnsOne(g => g.AllocationPeriod, period =>
        {
            period.Property(p => p.Months).HasColumnName("allocation_period_months").HasDefaultValue(0);
            period.Property(p => p.Days).HasColumnName("allocation_period_days").HasDefaultValue(0);
        });

        builder.HasOne(g => g.Creator)
            .WithMany()
            .HasForeignKey(g => g.CreatorId)
            .OnDelete(DeleteBehavior.Restrict); // Не удаляем пользователя, если удаляется группа

        builder.HasMany(g => g.Accounts)
            .WithOne(a => a.Group)
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Cascade); // Удаляем аккаунты при удалении группы
    }
}