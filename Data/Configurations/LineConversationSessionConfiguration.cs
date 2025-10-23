using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations;

/// <summary>
/// LineConversationSession 實體的 EF Core 配置
/// </summary>
public class LineConversationSessionConfiguration : IEntityTypeConfiguration<LineConversationSession>
{
    public void Configure(EntityTypeBuilder<LineConversationSession> builder)
    {
        builder.ToTable("LineConversationSessions");
        
        // 主鍵
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasDefaultValueSql("NEWID()");
        
        // 屬性配置
        builder.Property(s => s.LineUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(s => s.CurrentStep)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        
        builder.Property(s => s.SessionData)
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();
        
        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        builder.Property(s => s.ExpiresAt)
            .IsRequired();
        
        // 關聯配置
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 索引配置
        // 查詢索引 (依 LineUserId 查詢活躍 Session)
        builder.HasIndex(s => new { s.LineUserId, s.ExpiresAt });
        
        // 清理索引 (定期清理過期 Session)
        builder.HasIndex(s => s.ExpiresAt);
    }
}
