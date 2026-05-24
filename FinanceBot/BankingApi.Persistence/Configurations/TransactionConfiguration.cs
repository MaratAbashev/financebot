using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder
            .HasKey(t => t.Id);
        
        builder
            .Property(t => t.Amount)
            .HasPrecision(18, 2);
        
        builder
            .Property(t => t.Type)
            .HasConversion<string>();
        
        builder
            .Property(t => t.Description)
            .HasMaxLength(500);

        builder
            .HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId);

        builder
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId);
    }
}