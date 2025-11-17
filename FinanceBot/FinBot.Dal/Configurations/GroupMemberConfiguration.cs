using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.Property(gm => gm.GroupId)
            .IsRequired();
        builder.Property(gm => gm.UserId)
            .IsRequired();

        builder.Property(gm => gm.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(gm => gm.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}