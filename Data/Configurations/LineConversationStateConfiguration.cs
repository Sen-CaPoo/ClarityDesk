using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations
{
    /// <summary>
    /// LineConversationState 實體的 Fluent API 配置
    /// </summary>
    public class LineConversationStateConfiguration : IEntityTypeConfiguration<LineConversationState>
    {
        public void Configure(EntityTypeBuilder<LineConversationState> builder)
        {
            builder.ToTable("LineConversationStates");

            builder.HasKey(lcs => lcs.Id);

            builder.Property(lcs => lcs.LineUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(lcs => lcs.CurrentStep)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(lcs => lcs.Title)
                .HasMaxLength(200);

            builder.Property(lcs => lcs.Content)
                .HasMaxLength(2000);

            builder.Property(lcs => lcs.Priority)
                .HasMaxLength(20);

            builder.Property(lcs => lcs.CustomerName)
                .HasMaxLength(100);

            builder.Property(lcs => lcs.CustomerPhone)
                .HasMaxLength(20);

            builder.Property(lcs => lcs.ImageUrls)
                .HasMaxLength(1000);

            builder.Property(lcs => lcs.ExpiresAt)
                .IsRequired();

            builder.Property(lcs => lcs.CreatedAt)
                .IsRequired();

            builder.Property(lcs => lcs.UpdatedAt)
                .IsRequired();

            // 索引
            builder.HasIndex(lcs => lcs.LineUserId)
                .HasDatabaseName("IX_LineConversationState_LineUserId");

            builder.HasIndex(lcs => lcs.ExpiresAt)
                .HasDatabaseName("IX_LineConversationState_ExpiresAt");

            // 關聯
            builder.HasOne(lcs => lcs.User)
                .WithMany()
                .HasForeignKey(lcs => lcs.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
