using System.Text.Json;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinBot.Dal.Configurations;

public class DialogConfiguration: IEntityTypeConfiguration<DialogContext>
{
    public void Configure(EntityTypeBuilder<DialogContext> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd();
        
        builder.HasIndex(d => d.UserId)
            .IsUnique();
        builder.Property(d => d.UserId)
            .IsRequired();
        
        builder.Property(d => d.CurrentStep)
            .IsRequired();
        
        builder.Property(d => d.DialogName)
            .IsRequired();

        builder.Property(d => d.DialogStorage)
            .HasColumnType("jsonb")
            .HasConversion(
                ds => 
                    JsonSerializer.Serialize(ds, new JsonSerializerOptions { WriteIndented = true }),
                ds => 
                    JsonSerializer.Deserialize<Dictionary<string, object>>(ds, new JsonSerializerOptions { WriteIndented = true }));
    }
}