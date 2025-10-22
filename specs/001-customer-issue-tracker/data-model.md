# Data Model: 顧客問題紀錄追蹤系統

**Feature**: 001-customer-issue-tracker  
**Date**: 2025-10-20  
**Status**: Phase 1 Design

## 概述

本文件定義顧客問題紀錄追蹤系統的資料模型,包含實體關聯、欄位定義、驗證規則與資料庫索引策略。

## Entity Relationship Diagram (ERD)

```text
┌─────────────────┐         ┌──────────────────────┐         ┌─────────────────┐
│     User        │         │    IssueReport       │         │   Department    │
├─────────────────┤         ├──────────────────────┤         ├─────────────────┤
│ Id (PK)         │────┐    │ Id (PK)              │    ┌───│ Id (PK)         │
│ LineUserId      │    │    │ Title                │    │    │ Name            │
│ DisplayName     │    │    │ Content              │    │    │ Description     │
│ Email           │    │    │ RecordDate           │    │    │ IsActive        │
│ Role            │    │    │ Status               │    │    │ CreatedAt       │
│ IsActive        │    │    │ Priority             │    │    │ UpdatedAt       │
│ PictureUrl      │    │    │ ReporterName         │    │    └─────────────────┘
│ CreatedAt       │    │    │ CustomerName         │    │             │
│ UpdatedAt       │    │    │ CustomerPhone        │    │             │
└─────────────────┘    │    │ AssignedUserId (FK)  │────┘             │
                       │    │ CreatedAt            │                  │
                       │    │ UpdatedAt            │                  │
                       │    └──────────────────────┘                  │
                       │              │                                │
                       │              │ M:N                            │
                       │              ▼                                │
                       │    ┌──────────────────────┐                  │
                       └───│ DepartmentAssignment  │◄─────────────────┘
                            ├──────────────────────┤
                            │ Id (PK)              │
                            │ IssueReportId (FK)   │
                            │ DepartmentId (FK)    │
                            │ AssignedAt           │
                            └──────────────────────┘
```

## 實體定義

### 1. User (使用者)

**描述**: 代表系統的註冊使用者,綁定 LINE 帳號並擁有權限角色。

**欄位定義**:

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|----------|
| Id | int | ✅ | 主鍵,自動遞增 | PK, Identity |
| LineUserId | nvarchar(100) | ✅ | LINE User ID (唯一識別碼) | Unique Index, Max Length: 100 |
| DisplayName | nvarchar(100) | ✅ | 顯示名稱 | Max Length: 100 |
| Email | nvarchar(255) | ❌ | 電子信箱 | Email Format, Max Length: 255 |
| Role | nvarchar(20) | ✅ | 權限角色 (`User` or `Admin`) | Enum: UserRole |
| IsActive | bit | ✅ | 帳號狀態 | Default: true |
| PictureUrl | nvarchar(500) | ❌ | LINE 頭像 URL | Max Length: 500 |
| CreatedAt | datetime2 | ✅ | 建立時間 | Default: GETUTCDATE() |
| UpdatedAt | datetime2 | ✅ | 更新時間 | Auto-update on modify |

**索引**:
- Primary Key: `Id`
- Unique Index: `LineUserId`
- Non-Clustered Index: `Role`, `IsActive`

**關聯**:
- One-to-Many: `User` → `IssueReport` (AssignedUser)

**EF Core Configuration**:

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.LineUserId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(u => u.LineUserId)
            .IsUnique();
        
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.Email)
            .HasMaxLength(255);
        
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();
        
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(u => u.PictureUrl)
            .HasMaxLength(500);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
        
        builder.Property(u => u.UpdatedAt)
            .IsRequired();
        
        builder.HasIndex(u => new { u.Role, u.IsActive });
    }
}
```

---

### 2. IssueReport (回報單)

**描述**: 代表一筆顧客問題回報記錄,包含問題詳情、狀態、優先級與指派資訊。

**欄位定義**:

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|----------|
| Id | int | ✅ | 主鍵,自動遞增 | PK, Identity |
| Title | nvarchar(200) | ✅ | 問題標題 | Max Length: 200 |
| Content | nvarchar(max) | ✅ | 問題內容詳述 | Min Length: 10 |
| RecordDate | date | ✅ | 紀錄日期 | Date Format |
| Status | nvarchar(20) | ✅ | 處理狀態 | Enum: IssueStatus (Pending, InProgress, Completed) |
| Priority | nvarchar(20) | ✅ | 緊急程度 | Enum: PriorityLevel (High, Medium, Low) |
| ReporterName | nvarchar(100) | ✅ | 回報人姓名 | Max Length: 100 |
| CustomerName | nvarchar(100) | ✅ | 顧客聯絡人姓名 | Max Length: 100 |
| CustomerPhone | nvarchar(20) | ✅ | 顧客連絡電話 | Phone Format, Max Length: 20 |
| AssignedUserId | int | ✅ | 指派處理人員 ID | FK to User |
| CreatedAt | datetime2 | ✅ | 建立時間 | Default: GETUTCDATE() |
| UpdatedAt | datetime2 | ✅ | 最後修改時間 | Auto-update on modify |

**索引**:
- Primary Key: `Id`
- Non-Clustered Index: `Status`, `Priority`, `RecordDate`, `AssignedUserId`
- Composite Index: `(Status, Priority)` (常用篩選條件)

**關聯**:
- Many-to-One: `IssueReport` → `User` (AssignedUser)
- Many-to-Many: `IssueReport` ↔ `Department` (透過 DepartmentAssignment)

**EF Core Configuration**:

```csharp
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
        
        // Indexes
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Priority);
        builder.HasIndex(i => i.RecordDate);
        builder.HasIndex(i => i.AssignedUserId);
        builder.HasIndex(i => new { i.Status, i.Priority });
    }
}
```

---

### 3. Department (問題所屬單位)

**描述**: 代表組織內的部門或單位,用於分類回報單。

**欄位定義**:

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|----------|
| Id | int | ✅ | 主鍵,自動遞增 | PK, Identity |
| Name | nvarchar(100) | ✅ | 單位名稱 | Unique, Max Length: 100 |
| Description | nvarchar(500) | ❌ | 單位描述 | Max Length: 500 |
| IsActive | bit | ✅ | 單位狀態 (啟用/停用) | Default: true |
| CreatedAt | datetime2 | ✅ | 建立時間 | Default: GETUTCDATE() |
| UpdatedAt | datetime2 | ✅ | 更新時間 | Auto-update on modify |

**索引**:
- Primary Key: `Id`
- Unique Index: `Name`
- Non-Clustered Index: `IsActive`

**關聯**:
- Many-to-Many: `Department` ↔ `IssueReport` (透過 DepartmentAssignment)

**EF Core Configuration**:

```csharp
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
```

---

### 4. DepartmentAssignment (部門指派)

**描述**: 多對多關聯表,記錄回報單與所屬單位的關係。

**欄位定義**:

| 欄位名稱 | 資料型別 | 必填 | 說明 | 驗證規則 |
|---------|---------|------|------|----------|
| Id | int | ✅ | 主鍵,自動遞增 | PK, Identity |
| IssueReportId | int | ✅ | 回報單 ID | FK to IssueReport |
| DepartmentId | int | ✅ | 單位 ID | FK to Department |
| AssignedAt | datetime2 | ✅ | 指派時間 | Default: GETUTCDATE() |

**索引**:
- Primary Key: `Id`
- Unique Composite Index: `(IssueReportId, DepartmentId)`
- Non-Clustered Index: `DepartmentId`

**關聯**:
- Many-to-One: `DepartmentAssignment` → `IssueReport`
- Many-to-One: `DepartmentAssignment` → `Department`

**EF Core Configuration**:

```csharp
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
```

---

## 列舉型別 (Enums)

### UserRole (使用者角色)

```csharp
namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// 使用者權限角色
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// 普通使用者 (可建立、編輯、刪除回報單)
        /// </summary>
        User,
        
        /// <summary>
        /// 管理人員 (擁有所有權限 + 系統管理功能)
        /// </summary>
        Admin
    }
}
```

### IssueStatus (處理狀態)

```csharp
namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// 回報單處理狀態
    /// </summary>
    public enum IssueStatus
    {
        /// <summary>
        /// 待處理
        /// </summary>
        Pending,
        
        /// <summary>
        /// 處理中
        /// </summary>
        InProgress,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed
    }
}
```

### PriorityLevel (緊急程度)

```csharp
namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// 回報單緊急程度
    /// </summary>
    public enum PriorityLevel
    {
        /// <summary>
        /// 低 (綠色標示)
        /// </summary>
        Low,
        
        /// <summary>
        /// 中 (橙色標示)
        /// </summary>
        Medium,
        
        /// <summary>
        /// 高 (紅色標示)
        /// </summary>
        High
    }
}
```

---

## 資料驗證規則

### 伺服器端驗證 (Data Annotations)

```csharp
// Models/DTOs/CreateIssueReportDto.cs
public class CreateIssueReportDto
{
    [Required(ErrorMessage = "標題為必填欄位")]
    [StringLength(200, ErrorMessage = "標題不可超過 200 字元")]
    public string Title { get; set; }
    
    [Required(ErrorMessage = "內容為必填欄位")]
    [MinLength(10, ErrorMessage = "內容至少需要 10 字元")]
    public string Content { get; set; }
    
    [Required(ErrorMessage = "紀錄日期為必填欄位")]
    [DataType(DataType.Date)]
    public DateTime RecordDate { get; set; }
    
    [Required(ErrorMessage = "處理狀態為必填欄位")]
    public IssueStatus Status { get; set; }
    
    [Required(ErrorMessage = "緊急程度為必填欄位")]
    public PriorityLevel Priority { get; set; }
    
    [Required(ErrorMessage = "回報人姓名為必填欄位")]
    [StringLength(100, ErrorMessage = "回報人姓名不可超過 100 字元")]
    public string ReporterName { get; set; }
    
    [Required(ErrorMessage = "顧客姓名為必填欄位")]
    [StringLength(100, ErrorMessage = "顧客姓名不可超過 100 字元")]
    public string CustomerName { get; set; }
    
    [Required(ErrorMessage = "顧客電話為必填欄位")]
    [Phone(ErrorMessage = "請輸入有效的電話號碼")]
    [StringLength(20, ErrorMessage = "電話號碼不可超過 20 字元")]
    public string CustomerPhone { get; set; }
    
    [Required(ErrorMessage = "指派處理人員為必填欄位")]
    public int AssignedUserId { get; set; }
    
    [Required(ErrorMessage = "問題所屬單位為必填欄位")]
    [MinLength(1, ErrorMessage = "至少需選擇一個單位")]
    public List<int> DepartmentIds { get; set; }
}
```

### 客戶端驗證 (jQuery Validation)

使用 ASP.NET Core 內建的 `_ValidationScriptsPartial.cshtml`:

```html
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

---

## 資料庫初始化種子資料 (Seed Data)

### 預設管理員帳號

```csharp
// Data/ApplicationDbContextSeed.cs
public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 檢查是否已有使用者
        if (await context.Users.AnyAsync())
            return;
        
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
```

---

## 效能考量

### 查詢最佳化策略

1. **索引策略**:
   - 為常用篩選欄位建立索引 (`Status`, `Priority`, `RecordDate`)
   - 使用複合索引加速多條件查詢 (`Status + Priority`)
   - 為外鍵建立索引 (`AssignedUserId`, `DepartmentId`)

2. **查詢模式**:
   ```csharp
   // ✅ 好的做法: 使用 Include 避免 N+1
   var issues = await _context.IssueReports
       .Include(i => i.AssignedUser)
       .Include(i => i.DepartmentAssignments)
           .ThenInclude(da => da.Department)
       .Where(i => i.Status == IssueStatus.Pending)
       .OrderByDescending(i => i.CreatedAt)
       .AsNoTracking()
       .ToListAsync();
   
   // ❌ 不好的做法: 導致 N+1 查詢
   var issues = await _context.IssueReports.ToListAsync();
   foreach (var issue in issues)
   {
       var user = await _context.Users.FindAsync(issue.AssignedUserId);
   }
   ```

3. **分頁查詢**:
   ```csharp
   const int PageSize = 20;
   var pagedIssues = await _context.IssueReports
       .OrderByDescending(i => i.CreatedAt)
       .Skip((page - 1) * PageSize)
       .Take(PageSize)
       .AsNoTracking()
       .ToListAsync();
   ```

4. **投影查詢** (僅載入必要欄位):
   ```csharp
   var issueSummaries = await _context.IssueReports
       .Select(i => new IssueListItemDto
       {
           Id = i.Id,
           Title = i.Title,
           Status = i.Status,
           Priority = i.Priority,
           AssignedUserName = i.AssignedUser.DisplayName
       })
       .ToListAsync();
   ```

---

## Migration 腳本

### 初始 Migration

```bash
# 建立初始 Migration
dotnet ef migrations add InitialCreate --project ClarityDesk --startup-project ClarityDesk

# 套用至資料庫
dotnet ef database update --project ClarityDesk --startup-project ClarityDesk

# 產生 SQL 腳本 (正式環境使用)
dotnet ef migrations script --idempotent --output ./migrations/001_InitialCreate.sql
```

---

## 資料模型演進策略

### 未來擴展考量

1. **審計日誌 (Audit Log)**:
   - 若需追蹤回報單的變更歷史,可新增 `IssueReportHistory` 表

2. **檔案附件 (Attachments)**:
   - 若需上傳圖片或文件,可新增 `Attachment` 表

3. **評論系統 (Comments)**:
   - 若需在回報單中新增評論,可新增 `IssueComment` 表

4. **通知系統 (Notifications)**:
   - 若需推播通知,可新增 `Notification` 表

---

**版本**: 1.0  
**狀態**: ✅ Phase 1 Complete  
**最後更新**: 2025-10-20
