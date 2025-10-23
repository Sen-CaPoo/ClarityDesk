using ClarityDesk.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations
{
    /// <summary>
    /// IssueReport 實體的 EF Core 組態
    /// </summary>
    public class IssueReportConfiguration : IEntityTypeConfiguration<IssueReport>
    {
        public void Configure(EntityTypeBuilder<IssueReport> builder)
        {
            builder.HasKey(i => i.Id);
            
            builder.Property(i => i.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(i => i.Content)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
            
            builder.Property(i => i.RecordDate)
                .IsRequired()
                .HasColumnType("date");
            
            builder.Property(i => i.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>();
            
            builder.Property(i => i.Priority)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>();
            
            builder.Property(i => i.Source)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion<string>()
                .HasDefaultValue(Models.Enums.IssueReportSource.Web);
            
            builder.Property(i => i.ReporterName)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(i => i.CustomerName)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(i => i.CustomerPhone)
                .IsRequired()
                .HasMaxLength(20);
            
            builder.Property(i => i.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            builder.Property(i => i.UpdatedAt)
                .IsRequired();
            
            // Foreign Key
            builder.HasOne(i => i.AssignedUser)
                .WithMany()
                .HasForeignKey(i => i.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // LastModifiedBy Foreign Key
            builder.HasOne(i => i.LastModifiedBy)
                .WithMany()
                .HasForeignKey(i => i.LastModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            builder.HasIndex(i => i.Status);
            builder.HasIndex(i => i.Priority);
            builder.HasIndex(i => i.Source);
            builder.HasIndex(i => i.RecordDate);
            builder.HasIndex(i => i.AssignedUserId);
            builder.HasIndex(i => i.LastModifiedByUserId);
            builder.HasIndex(i => new { i.Status, i.Priority });
        }
    }
}
