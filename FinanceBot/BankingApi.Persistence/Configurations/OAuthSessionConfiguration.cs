using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingApi.Persistence.Configurations;

public class OAuthSessionConfiguration : IEntityTypeConfiguration<OAuthSession>
{
    public void Configure(EntityTypeBuilder<OAuthSession> builder)
    {
        builder
            .HasKey(o => o.Id);
        
        builder
            .Property(o => o.AuthCode)
            .HasMaxLength(64)
            .IsRequired();
        
        builder
            .Property(o => o.RedirectUri)
            .HasMaxLength(500)
            .IsRequired();
        
        builder
            .Property(o => o.State)
            .HasMaxLength(256)
            .IsRequired();
        
        builder
            .Property(o => o.Status)
            .HasConversion<string>();
        
        builder
            .HasIndex(o => o.AuthCode)
            .IsUnique();

        builder
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId);
    }
}