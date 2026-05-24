using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class JoinRequestConfiguration : IEntityTypeConfiguration<JoinRequest>
{
    public void Configure(EntityTypeBuilder<JoinRequest> builder)
    {
        builder.HasKey(jr => jr.Id);

        builder.HasOne(jr => jr.User)
            .WithMany()
            .HasForeignKey(jr => jr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(jr => jr.Group)
            .WithMany()
            .HasForeignKey(jr => jr.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(jr => new { jr.UserId, jr.GroupId }).IsUnique();
    }
}
