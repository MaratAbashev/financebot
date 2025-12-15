using FinBot.Domain.Models;
using FinBot.Domain.Models.SavingModel;
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
        
        builder.Property(g => g.SavingStrategy)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(g => g.Creator)
            .WithMany(g => g.Groups)
            .HasForeignKey(g => g.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Accounts)
            .WithOne(a => a.Group)
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Saving)
            .WithOne(s => s.Group)
            .HasForeignKey<Saving>(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}