using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.TelegramId)
            .IsRequired();

        builder.HasIndex(u => u.TelegramId)
            .IsUnique();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(128);
    }
}