using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations;

/// <summary>
/// LineMessageLog 實體的 EF Core 配置
/// </summary>
public class LineMessageLogConfiguration : IEntityTypeConfiguration<LineMessageLog>
{
    public void Configure(EntityTypeBuilder<LineMessageLog> builder)
    {
        builder.ToTable("LineMessageLogs");
        
        // 主鍵
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id)
            .HasDefaultValueSql("NEWID()");
        
        // 屬性配置
        builder.Property(log => log.LineUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(log => log.MessageType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(log => log.Direction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(log => log.Content)
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();
        
        builder.Property(log => log.IsSuccess)
            .IsRequired();
        
        builder.Property(log => log.ErrorCode)
            .HasMaxLength(50);
        
        builder.Property(log => log.ErrorMessage)
            .HasMaxLength(500);
        
        builder.Property(log => log.SentAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        // 關聯配置
        builder.HasOne(log => log.IssueReport)
            .WithMany()
            .HasForeignKey(log => log.IssueReportId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // 索引配置
        // 查詢索引 (依時間範圍與類型統計配額)
        builder.HasIndex(log => new { log.SentAt, log.MessageType, log.IsSuccess });
        
        // 查詢索引 (依使用者查詢歷史訊息)
        builder.HasIndex(log => new { log.LineUserId, log.SentAt });
        
        // 外鍵索引 (僅當 IssueReportId 不為 NULL 時建立)
        builder.HasIndex(log => log.IssueReportId)
            .HasFilter($"[{nameof(LineMessageLog.IssueReportId)}] IS NOT NULL");
    }
}
