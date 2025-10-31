using ClarityDesk.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations
{
    /// <summary>
    /// LineBinding 實體的 Fluent API 配置
    /// </summary>
    public class LineBindingConfiguration : IEntityTypeConfiguration<LineBinding>
    {
        public void Configure(EntityTypeBuilder<LineBinding> builder)
        {
            builder.ToTable("LineBindings");

            builder.HasKey(lb => lb.Id);

            builder.Property(lb => lb.LineUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(lb => lb.DisplayName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(lb => lb.PictureUrl)
                .HasMaxLength(500);

            builder.Property(lb => lb.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(lb => lb.BoundAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(lb => lb.CreatedAt)
                .IsRequired();

            builder.Property(lb => lb.UpdatedAt)
                .IsRequired();

            // 索引
            builder.HasIndex(lb => lb.LineUserId)
                .IsUnique()
                .HasDatabaseName("IX_LineBinding_LineUserId");

            builder.HasIndex(lb => lb.UserId)
                .IsUnique()
                .HasDatabaseName("IX_LineBinding_UserId");

            // 關聯
            builder.HasOne(lb => lb.User)
                .WithOne()
                .HasForeignKey<LineBinding>(lb => lb.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
