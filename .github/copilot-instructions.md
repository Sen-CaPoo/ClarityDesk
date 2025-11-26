# ClarityDesk AI Agent Instructions

ASP.NET Core 8 Razor Pages 顧客問題追蹤系統,整合 LINE Login 與 LINE Messaging API。

## 架構概覽

```
Pages (UI) → Services (業務邏輯) → Data (EF Core)
     └──────── Infrastructure (Middleware, Helpers) ────────┘
```

**核心原則**: PageModel 透過服務介面呼叫業務邏輯,不直接存取 `ApplicationDbContext`。

## 開發指令

```powershell
dotnet build                                    # 建置
dotnet run                                      # 執行 (https://localhost:5001)
dotnet ef migrations add MigrationName          # 新增 Migration
dotnet ef database update                       # 套用 Migration
dotnet test                                     # 執行測試
dotnet test --filter "FullyQualifiedName~Line"  # 執行特定測試
```

## 關鍵模式與慣例

### 服務層 (參考 `Services/IssueReportService.cs`)

```csharp
public class NewService : INewService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NewService> _logger;
    
    public async Task<T> MethodAsync(..., CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("操作描述: {Param}", param);
        try {
            // 業務邏輯
            _cache.Remove(CacheKey);  // 更新後清除快取
            return result;
        } catch (Exception ex) {
            _logger.LogError(ex, "錯誤描述");
            throw;
        }
    }
}
```

- 介面定義: `Services/Interfaces/I*.cs`
- 註冊方式: `builder.Services.AddScoped<IService, ServiceImpl>()` in `Program.cs`

### DTO 轉換 (參考 `Models/Extensions/IssueReportExtensions.cs`)

```csharp
// Entity → DTO
public static IssueReportDto ToDto(this IssueReport entity) { ... }
// DTO → Entity  
public static IssueReport ToEntity(this CreateIssueReportDto dto) { ... }
```

### 自動時間戳記

`ApplicationDbContext.UpdateTimestamps()` 自動設定 `CreatedAt`/`UpdatedAt`,無需手動處理。

### 頁面授權 (參考 `Program.cs`)

```csharp
options.Conventions.AuthorizePage("/Admin/Users/Index", "Admin");
options.Conventions.AllowAnonymousToPage("/Account/Login");
```

### TempData 訊息

```csharp
TempData["SuccessMessage"] = "操作成功";  // 或 "ErrorMessage"
```

## LINE 整合 (分支: `002-line-integration`)

**服務元件**:
- `ILineBindingService`: 帳號綁定管理
- `ILineMessagingService`: 推送通知 (Flex Message)
- `ILineConversationService`: 對話 Session 管理
- `ILineWebhookHandler`: Webhook 事件處理

**Webhook 端點**: `POST /api/line/webhook` (簽章驗證於 `LineSignatureValidationMiddleware`)

**設定**: `LineSettings` section in appsettings.json (ChannelId, ChannelSecret, ChannelAccessToken)

## 新增功能檢查清單

**新增 Service**:
1. `Services/Interfaces/INewService.cs` - 介面定義
2. `Services/NewService.cs` - 實作
3. `Program.cs` - `AddScoped` 註冊
4. `Tests/ClarityDesk.UnitTests/Services/NewServiceTests.cs` - 測試

**新增 Entity**:
1. `Models/Entities/NewEntity.cs`
2. `Data/Configurations/NewEntityConfiguration.cs`
3. `ApplicationDbContext` 新增 `DbSet`
4. `dotnet ef migrations add AddNewEntity`
5. `Models/DTOs/` + `Models/Extensions/` 建立對應 DTO 與轉換方法

**新增 Razor Page**:
1. `Pages/Feature/Page.cshtml.cs` - 注入服務介面
2. `Pages/Feature/Page.cshtml` - Bootstrap 5 樣式
3. `Program.cs` - 設定授權 (`AuthorizePage`)

## 常見陷阱

- ❌ PageModel 直接注入 DbContext → ✅ 透過服務介面
- ❌ 同步方法 `Find()`, `FirstOrDefault()` → ✅ `FindAsync()`, `FirstOrDefaultAsync()`
- ❌ 忘記清除快取 → ✅ 資料更新後呼叫 `_cache.Remove()`
- ❌ N+1 查詢 → ✅ 使用 `.Include()` 載入導覽屬性
- ❌ 唯讀查詢追蹤實體 → ✅ 使用 `.AsNoTracking()`

## 測試慣例

- 框架: xUnit + FluentAssertions + Moq
- 命名: `[Method]_[Scenario]_[Result]` (如 `CreateAsync_ValidDto_ReturnsId`)
- 測試資料庫: In-Memory Database

## 重要參考文件

- `specs/001-customer-issue-tracker/` - 核心功能規格
- `specs/002-line-integration/` - LINE 整合規格
- `docs/deployment/DEPLOYMENT.md` - 部署指南
- `AGENTS.md` - 開發工作流程與 PR 指南
