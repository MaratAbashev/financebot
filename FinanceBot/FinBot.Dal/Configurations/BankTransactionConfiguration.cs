using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder
            .HasKey(t => t.Id);
            
        builder
            .Property(t => t.Amount)
            .HasPrecision(18, 2);
            
        builder
            .Property(t => t.Type)
            .HasMaxLength(50);
            
        builder
            .Property(t => t.CategoryName)
            .HasMaxLength(100);
            
        builder
            .Property(t => t.Description)
            .HasMaxLength(500);
            
        builder
            .Property(t => t.Status)
            .HasConversion<string>();
            
        builder
            .HasIndex(t => t.ExternalId)
            .IsUnique();
            
        builder
            .HasIndex(t => t.UserId);
    }
}