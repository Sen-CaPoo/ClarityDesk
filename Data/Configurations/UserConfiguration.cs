using ClarityDesk.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations
{
    /// <summary>
    /// User 實體的 EF Core 組態
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.LineUserId)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.HasIndex(u => u.LineUserId)
                .IsUnique();
            
            builder.Property(u => u.DisplayName)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(u => u.Email)
                .HasMaxLength(255);
            
            builder.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>();
            
            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(u => u.PictureUrl)
                .HasMaxLength(500);
            
            builder.Property(u => u.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            builder.Property(u => u.UpdatedAt)
                .IsRequired();
            
            builder.HasIndex(u => new { u.Role, u.IsActive });
        }
    }
}
