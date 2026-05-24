using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Persistence.Configurations;

public class PhoneVerificationCodeConfiguration : IEntityTypeConfiguration<PhoneVerificationCode>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationCode> builder)
    {
        builder
            .HasKey(p => p.Id);
        
        builder
            .Property(p => p.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();
        
        builder
            .Property(p => p.Code)
            .HasMaxLength(6)
            .IsRequired();
        
        builder
            .HasIndex(p => p.PhoneNumber);
    }
}