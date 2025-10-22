using ClarityDesk.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations
{
    /// <summary>
    /// Department 實體的 EF Core 組態
    /// </summary>
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.HasKey(d => d.Id);
            
            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.HasIndex(d => d.Name)
                .IsUnique();
            
            builder.Property(d => d.Description)
                .HasMaxLength(500);
            
            builder.Property(d => d.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(d => d.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            builder.Property(d => d.UpdatedAt)
                .IsRequired();
            
            builder.HasIndex(d => d.IsActive);
        }
    }
}
