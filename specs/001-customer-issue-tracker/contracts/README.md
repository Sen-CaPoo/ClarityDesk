# Service Contracts: 顧客問題紀錄追蹤系統

**Feature**: 001-customer-issue-tracker  
**Date**: 2025-10-20  
**Purpose**: 定義服務層介面合約

## 概述

本目錄包含系統所有服務層的介面定義,遵循介面隔離原則 (Interface Segregation Principle) 與依賴反轉原則 (Dependency Inversion Principle)。

## 服務介面清單

### 1. IIssueReportService (回報單管理服務)

**職責**: 處理回報單的 CRUD 操作、狀態管理與查詢篩選

**主要方法**:
- `CreateIssueReportAsync`: 建立新回報單
- `UpdateIssueReportAsync`: 更新回報單
- `DeleteIssueReportAsync`: 刪除回報單
- `GetIssueReportByIdAsync`: 取得單筆回報單
- `GetIssueReportsAsync`: 取得回報單列表 (支援篩選與分頁)
- `GetIssueStatisticsAsync`: 取得回報單統計資訊
- `UpdateIssueStatusAsync`: 更新回報單狀態
- `AssignIssueToUserAsync`: 指派回報單給使用者

**相依性**:
- `ILogger<IssueReportService>`
- `ApplicationDbContext`
- `IMemoryCache` (快取查詢結果)

---

### 2. IAuthenticationService (身份驗證服務)

**職責**: 處理 LINE Login OAuth 2.0 整合與使用者身份驗證

**主要方法**:
- `LoginOrRegisterWithLineAsync`: LINE 登入或註冊
- `GetUserByLineIdAsync`: 根據 LINE User ID 取得使用者
- `IsAdminAsync`: 驗證管理員權限
- `IsUserActiveAsync`: 驗證使用者活躍狀態

**相依性**:
- `ILogger<AuthenticationService>`
- `ApplicationDbContext`
- `IOptions<LineLoginOptions>` (LINE OAuth 設定)

**安全性考量**:
- 驗證 LINE User ID 的真實性 (透過 Access Token 驗證)
- 防止 CSRF 攻擊 (驗證 state 參數)
- 使用 HTTPS 傳輸所有敏感資料

---

### 3. IUserManagementService (使用者管理服務)

**職責**: 管理使用者權限、帳號狀態與角色分配

**主要方法**:
- `GetAllUsersAsync`: 取得所有使用者列表
- `GetUserByIdAsync`: 取得單筆使用者資料
- `UpdateUserRoleAsync`: 更新使用者權限角色
- `SetUserActiveStatusAsync`: 啟用或停用使用者帳號
- `GetUsersByRoleAsync`: 根據角色篩選使用者

**相依性**:
- `ILogger<UserManagementService>`
- `ApplicationDbContext`
- `IMemoryCache` (快取使用者清單)

**權限控制**:
- 只有管理員可以呼叫此服務
- 使用 `[Authorize(Roles = "Admin")]` 保護相關頁面

---

### 4. IDepartmentService (問題所屬單位服務)

**職責**: 管理問題所屬單位的 CRUD 操作與處理人員指派

**主要方法**:
- `CreateDepartmentAsync`: 建立新單位
- `UpdateDepartmentAsync`: 更新單位資訊
- `DeleteDepartmentAsync`: 軟刪除單位 (標記為已停用)
- `GetAllDepartmentsAsync`: 取得所有單位列表
- `GetDepartmentByIdAsync`: 取得單筆單位資料
- `AssignUsersToDepartmentAsync`: 為單位指派預設處理人員
- `GetDepartmentUsersAsync`: 取得單位的處理人員列表

**相依性**:
- `ILogger<DepartmentService>`
- `ApplicationDbContext`
- `IMemoryCache` (快取單位清單)

**快取策略**:
- 單位清單快取 1 小時 (參考資料不常變動)
- 新增、更新、刪除單位時清除快取

---

## DTOs (Data Transfer Objects)

以下是服務介面使用的 DTO 定義:

### CreateIssueReportDto

```csharp
public class CreateIssueReportDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime RecordDate { get; set; }
    public IssueStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public string ReporterName { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public int AssignedUserId { get; set; }
    public List<int> DepartmentIds { get; set; }
}
```

### UpdateIssueReportDto

```csharp
public class UpdateIssueReportDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime RecordDate { get; set; }
    public IssueStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public int AssignedUserId { get; set; }
    public List<int> DepartmentIds { get; set; }
}
```

### IssueReportDto

```csharp
public class IssueReportDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime RecordDate { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
    public string AssignedUserName { get; set; }
    public string ReporterName { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public List<string> DepartmentNames { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### IssueFilterDto

```csharp
public class IssueFilterDto
{
    public IssueStatus? Status { get; set; }
    public PriorityLevel? Priority { get; set; }
    public List<int>? DepartmentIds { get; set; }
    public int? AssignedUserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchKeyword { get; set; }
}
```

### PagedResult<T>

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
```

### IssueStatisticsDto

```csharp
public class IssueStatisticsDto
{
    public int TotalIssues { get; set; }
    public int PendingIssues { get; set; }
    public int InProgressIssues { get; set; }
    public int CompletedIssues { get; set; }
    public int HighPriorityIssues { get; set; }
    public int MediumPriorityIssues { get; set; }
    public int LowPriorityIssues { get; set; }
}
```

### UserDto

```csharp
public class UserDto
{
    public int Id { get; set; }
    public string LineUserId { get; set; }
    public string DisplayName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public string? PictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### CreateDepartmentDto

```csharp
public class CreateDepartmentDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
}
```

### UpdateDepartmentDto

```csharp
public class UpdateDepartmentDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
```

### DepartmentDto

```csharp
public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 服務註冊 (Dependency Injection)

在 `Program.cs` 中註冊服務:

```csharp
// 註冊服務層
builder.Services.AddScoped<IIssueReportService, IssueReportService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();

// 註冊記憶體快取
builder.Services.AddMemoryCache();

// 註冊 DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 錯誤處理策略

所有服務方法應遵循以下錯誤處理模式:

```csharp
public async Task<int> CreateIssueReportAsync(CreateIssueReportDto dto, CancellationToken cancellationToken)
{
    try
    {
        // 業務邏輯
        // ...
        
        await _context.SaveChangesAsync(cancellationToken);
        return issueReport.Id;
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "建立回報單時發生資料庫錯誤");
        throw new ApplicationException("建立回報單失敗,請稍後再試", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "建立回報單時發生未預期的錯誤");
        throw;
    }
}
```

---

## 測試策略

每個服務介面需要對應的測試類別:

### 單元測試

- `IssueReportServiceTests.cs`: 測試回報單服務邏輯
- `AuthenticationServiceTests.cs`: 測試身份驗證流程
- `UserManagementServiceTests.cs`: 測試使用者管理功能
- `DepartmentServiceTests.cs`: 測試單位管理功能

### 測試範例

```csharp
public class IssueReportServiceTests
{
    [Fact]
    public async Task CreateIssueReportAsync_ValidDto_ReturnsIssueId()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var service = new IssueReportService(context, Mock.Of<ILogger<IssueReportService>>());
        var dto = new CreateIssueReportDto { /* ... */ };
        
        // Act
        var issueId = await service.CreateIssueReportAsync(dto);
        
        // Assert
        issueId.Should().BeGreaterThan(0);
        var issue = await context.IssueReports.FindAsync(issueId);
        issue.Should().NotBeNull();
        issue.Title.Should().Be(dto.Title);
    }
}
```

---

**版本**: 1.0  
**狀態**: ✅ Phase 1 Complete  
**最後更新**: 2025-10-20
