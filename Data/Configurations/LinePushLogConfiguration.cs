using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations
{
    /// <summary>
    /// LinePushLog 實體的 Fluent API 配置
    /// </summary>
    public class LinePushLogConfiguration : IEntityTypeConfiguration<LinePushLog>
    {
        public void Configure(EntityTypeBuilder<LinePushLog> builder)
        {
            builder.ToTable("LinePushLogs");

            builder.HasKey(lpl => lpl.Id);

            builder.Property(lpl => lpl.LineUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(lpl => lpl.MessageType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(lpl => lpl.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(lpl => lpl.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(lpl => lpl.ErrorMessage)
                .HasMaxLength(1000);

            builder.Property(lpl => lpl.PushedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(lpl => lpl.CreatedAt)
                .IsRequired();

            // 索引
            builder.HasIndex(lpl => lpl.IssueReportId)
                .HasDatabaseName("IX_LinePushLog_IssueReportId");

            builder.HasIndex(lpl => new { lpl.LineUserId, lpl.PushedAt })
                .HasDatabaseName("IX_LinePushLog_LineUserId_PushedAt");

            builder.HasIndex(lpl => lpl.Status)
                .HasDatabaseName("IX_LinePushLog_Status");

            // 關聯
            builder.HasOne(lpl => lpl.IssueReport)
                .WithMany()
                .HasForeignKey(lpl => lpl.IssueReportId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
