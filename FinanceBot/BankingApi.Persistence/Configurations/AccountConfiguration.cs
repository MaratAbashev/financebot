using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder
            .HasKey(a => a.Id);
        
        builder
            .Property(a => a.Name)
            .HasMaxLength(100)
            .IsRequired();
        
        builder
            .Property(a => a.Balance)
            .HasPrecision(18, 2);
        
        builder
            .Property(a => a.Currency)
            .HasConversion<string>();

        builder
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId);
    }
}