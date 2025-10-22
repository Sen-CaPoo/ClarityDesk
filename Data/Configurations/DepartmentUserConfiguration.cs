using ClarityDesk.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations;

/// <summary>
/// DepartmentUser 實體配置
/// </summary>
public class DepartmentUserConfiguration : IEntityTypeConfiguration<DepartmentUser>
{
    public void Configure(EntityTypeBuilder<DepartmentUser> builder)
    {
        // 資料表名稱
        builder.ToTable("DepartmentUsers");

        // 主鍵
        builder.HasKey(du => du.Id);

        // 唯一約束: 同一個單位不能重複指派同一個使用者
        builder.HasIndex(du => new { du.DepartmentId, du.UserId })
            .IsUnique()
            .HasDatabaseName("IX_DepartmentUsers_DepartmentId_UserId");

        // 外鍵關聯: DepartmentId -> Department
        builder.HasOne(du => du.Department)
            .WithMany()
            .HasForeignKey(du => du.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // 外鍵關聯: UserId -> User
        builder.HasOne(du => du.User)
            .WithMany()
            .HasForeignKey(du => du.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AssignedAt 欄位設定
        builder.Property(du => du.AssignedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
