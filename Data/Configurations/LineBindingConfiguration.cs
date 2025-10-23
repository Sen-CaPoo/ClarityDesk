using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClarityDesk.Data.Configurations;

/// <summary>
/// LineBinding 實體的 EF Core 配置
/// </summary>
public class LineBindingConfiguration : IEntityTypeConfiguration<LineBinding>
{
    public void Configure(EntityTypeBuilder<LineBinding> builder)
    {
        builder.ToTable("LineBindings");
        
        // 主鍵
        builder.HasKey(lb => lb.Id);
        
        // 屬性配置
        builder.Property(lb => lb.LineUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(lb => lb.DisplayName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(lb => lb.PictureUrl)
            .HasMaxLength(500);
        
        builder.Property(lb => lb.BindingStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(lb => lb.BoundAt)
            .IsRequired();
        
        builder.Property(lb => lb.LastInteractedAt)
            .IsRequired();
        
        builder.Property(lb => lb.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        builder.Property(lb => lb.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        
        // 關聯配置
        builder.HasOne(lb => lb.User)
            .WithOne(u => u.LineBinding)
            .HasForeignKey<LineBinding>(lb => lb.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 索引配置
        // 確保一個使用者只能有一個 Active 狀態的綁定
        builder.HasIndex(lb => lb.UserId)
            .IsUnique()
            .HasFilter($"[{nameof(LineBinding.BindingStatus)}] = 'Active'");
        
        // 確保一個 LINE 帳號在整個系統中唯一
        builder.HasIndex(lb => lb.LineUserId)
            .IsUnique();
        
        // 查詢索引 (依狀態查詢)
        builder.HasIndex(lb => lb.BindingStatus);
    }
}
