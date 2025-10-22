using ClarityDesk.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations
{
    /// <summary>
    /// DepartmentAssignment 實體的 EF Core 組態
    /// </summary>
    public class DepartmentAssignmentConfiguration : IEntityTypeConfiguration<DepartmentAssignment>
    {
        public void Configure(EntityTypeBuilder<DepartmentAssignment> builder)
        {
            builder.HasKey(da => da.Id);
            
            builder.Property(da => da.AssignedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Foreign Keys
            builder.HasOne(da => da.IssueReport)
                .WithMany(i => i.DepartmentAssignments)
                .HasForeignKey(da => da.IssueReportId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(da => da.Department)
                .WithMany()
                .HasForeignKey(da => da.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Unique Constraint
            builder.HasIndex(da => new { da.IssueReportId, da.DepartmentId })
                .IsUnique();
            
            builder.HasIndex(da => da.DepartmentId);
        }
    }
}
