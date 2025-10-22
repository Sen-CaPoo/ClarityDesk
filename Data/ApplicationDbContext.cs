using ClarityDesk.Data.Configurations;
using ClarityDesk.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClarityDesk.Data
{
    /// <summary>
    /// 應用程式 DbContext
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        /// <summary>
        /// 使用者
        /// </summary>
        public DbSet<User> Users { get; set; }
        
        /// <summary>
        /// 單位
        /// </summary>
        public DbSet<Department> Departments { get; set; }
        
        /// <summary>
        /// 回報單
        /// </summary>
        public DbSet<IssueReport> IssueReports { get; set; }
        
        /// <summary>
        /// 部門指派
        /// </summary>
        public DbSet<DepartmentAssignment> DepartmentAssignments { get; set; }

        /// <summary>
        /// 單位處理人員
        /// </summary>
        public DbSet<DepartmentUser> DepartmentUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // 套用所有 Entity Configurations
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new IssueReportConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new DepartmentUserConfiguration());
        }
        
        /// <summary>
        /// 覆寫 SaveChanges 自動更新 UpdatedAt 欄位
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }
        
        /// <summary>
        /// 覆寫 SaveChangesAsync 自動更新 UpdatedAt 欄位
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }
        
        /// <summary>
        /// 更新時間戳記
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            
            foreach (var entry in entries)
            {
                if (entry.Entity is User user)
                {
                    if (entry.State == EntityState.Added)
                    {
                        user.CreatedAt = DateTime.UtcNow;
                    }
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is Department department)
                {
                    if (entry.State == EntityState.Added)
                    {
                        department.CreatedAt = DateTime.UtcNow;
                    }
                    department.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is IssueReport issue)
                {
                    if (entry.State == EntityState.Added)
                    {
                        issue.CreatedAt = DateTime.UtcNow;
                    }
                    issue.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is DepartmentAssignment assignment)
                {
                    if (entry.State == EntityState.Added)
                    {
                        assignment.AssignedAt = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}
