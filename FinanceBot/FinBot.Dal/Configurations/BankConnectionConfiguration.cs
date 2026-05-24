using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class BankConnectionConfiguration : IEntityTypeConfiguration<BankConnection>
{
    public void Configure(EntityTypeBuilder<BankConnection> builder)
    {
        builder
            .HasKey(c => c.Id);
            
        builder
            .Property(c => c.UserId)
            .IsRequired();
            
        builder
            .Property(c => c.BankingApiBaseUrl)
            .HasMaxLength(500)
            .IsRequired();
            
        builder
            .Property(c => c.RefreshToken)
            .HasMaxLength(512)
            .IsRequired();
            
        builder
            .HasIndex(c => c.UserId)
            .IsUnique();
    }
}