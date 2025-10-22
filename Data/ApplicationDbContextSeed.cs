using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClarityDesk.Data
{
    /// <summary>
    /// 資料庫種子資料
    /// </summary>
    public static class ApplicationDbContextSeed
    {
        /// <summary>
        /// 初始化種子資料
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // 檢查是否已有資料
            if (await context.Users.AnyAsync() || await context.Departments.AnyAsync())
            {
                return; // 資料庫已有資料,不重複初始化
            }
            
            // 建立預設管理員 (需透過 LINE Login 首次登入後手動升級)
            var adminUser = new User
            {
                LineUserId = "system_admin",
                DisplayName = "系統管理員",
                Email = "admin@claritydesk.com",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            context.Users.Add(adminUser);
            
            // 建立預設單位
            var departments = new List<Department>
            {
                new Department
                {
                    Name = "客服部",
                    Description = "處理客戶服務相關問題",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Name = "技術部",
                    Description = "處理技術支援與系統問題",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Name = "業務部",
                    Description = "處理銷售與商業合作問題",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
            
            context.Departments.AddRange(departments);
            
            await context.SaveChangesAsync();
        }
    }
}
