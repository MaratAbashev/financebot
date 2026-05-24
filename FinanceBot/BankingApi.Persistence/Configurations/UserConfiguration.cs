using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasKey(u => u.Id);
        
        builder
            .Property(u => u.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();
        
        builder
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();
        
        builder
            .Property(u => u.Name)
            .HasMaxLength(100);
    }
}