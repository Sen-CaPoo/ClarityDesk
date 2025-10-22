# ClarityDesk AI Agent Instructions

ClarityDesk 是一個基於 ASP.NET Core 8 Razor Pages 的顧客問題紀錄追蹤系統,整合 LINE Login 身份驗證,使用 Azure SQL Database 與三層架構設計。

## 架構概覽

### 三層架構與依賴流向

```
Razor Pages (UI) → Services (業務邏輯) → Data (資料存取)
       ↓                ↓                      ↓
       └────────── Infrastructure ─────────────┘
```

**關鍵原則**:
- **Pages** 僅處理 HTTP 請求/回應與使用者介面邏輯,透過服務介面呼叫業務邏輯
- **Services** 包含所有業務邏輯,透過介面定義 (`Services/Interfaces/I*.cs`),使用 `Scoped` 生命週期
- **Data** 使用 EF Core Code First,實體配置在 `Data/Configurations/`,DbContext 自動管理時間戳記 (`UpdateTimestamps()`)
- **Infrastructure** 處理橫切關注點:LINE OAuth、全域例外處理中介軟體、Tag Helpers

### 核心元件互動

**回報單建立流程範例**:
1. `Pages/Issues/Create.cshtml.cs` 接收表單資料到 `[BindProperty]` 的 ViewModel
2. PageModel 呼叫 `IIssueReportService.CreateIssueReportAsync(dto)`
3. Service 將 DTO 轉換為實體 (`dto.ToEntity()` via Extension Methods)
4. Service 透過 `ApplicationDbContext` 儲存實體,自動觸發 `UpdateTimestamps()`
5. Service 建立 `DepartmentAssignment` 關聯並清除 `IMemoryCache` 中的統計快取
6. PageModel 處理回應並導向列表頁

**LINE Login 流程**:
- OAuth 配置在 `Program.cs` 中,使用 `AddOAuth("LINE", options => {...})`
- `OnCreatingTicket` 事件呼叫 `IAuthenticationService.LoginOrRegisterWithLineAsync()` 建立/更新本地使用者
- Claims 包含 `UserId`, `ClaimTypes.Role`, `ClaimTypes.NameIdentifier` (LINE User ID)
- 角色驗證透過 `[Authorize(Roles = "Admin")]` 或 `options.Conventions.AuthorizePage("/path", "Admin")`

## 資料模型特性

**實體關聯**:
- `IssueReport` ↔ `DepartmentAssignment` ↔ `Department` (多對多)
- `IssueReport.AssignedUser` → `User` (多對一)
- `DepartmentUser` 記錄單位與預設處理人員關聯

**自動時間戳記**: `ApplicationDbContext.SaveChanges[Async]()` 自動更新 `CreatedAt`/`UpdatedAt`,不需手動設定

**軟刪除**: Department 使用 `IsActive` 標記,不實際刪除以保留歷史資料

**DTO 轉換**: 使用 Extension Methods (`Models/Extensions/*.cs`) 實作 `ToDto()` 和 `ToEntity()`,例如 `IssueReportExtensions.cs`

## 開發工作流程

### 建置與執行

```powershell
# 建置專案
dotnet build

# 執行應用程式 (開發模式,使用 appsettings.Development.json)
dotnet run

# 執行特定環境
dotnet run --environment Production

# 發佈至 IIS (詳見 DEPLOYMENT.md)
dotnet publish -c Release -o ./publish
```

### 資料庫 Migrations

```powershell
# 新增 Migration (需在專案根目錄執行)
dotnet ef migrations add MigrationName

# 套用至資料庫
dotnet ef database update

# 產生 SQL 腳本 (用於生產環境)
dotnet ef migrations script --idempotent --output migration.sql

# 復原上一個 Migration
dotnet ef migrations remove
```

### 測試執行

```powershell
# 執行所有測試
dotnet test

# 執行特定專案測試
dotnet test Tests/ClarityDesk.UnitTests/ClarityDesk.UnitTests.csproj

# 產生覆蓋率報告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# 執行特定測試方法 (使用 --filter)
dotnet test --filter "FullyQualifiedName~IssueReportServiceTests.CreateIssueReportAsync"
```

**測試模式**: 使用 xUnit + FluentAssertions + Moq,In-Memory Database 用於單元測試
**命名慣例**: `[MethodName]_[Scenario]_[ExpectedResult]`,例如 `CreateIssueReportAsync_ValidDto_ReturnsIssueId`

## 專案特定慣例

### 服務層模式

所有服務必須:
1. 定義介面於 `Services/Interfaces/I*.cs`
2. 在 `Program.cs` 註冊為 `AddScoped<IService, ServiceImpl>()`
3. 使用建構子注入 `ApplicationDbContext`, `ILogger<T>`, `IMemoryCache` (如需要)
4. 在方法開頭記錄 `_logger.LogInformation()`,異常處理使用 `try-catch-log-throw`
5. 更新資料後清除相關快取 (`_cache.Remove(CacheKey)`)

**快取策略**:
- 統計資訊: 5 分鐘 (`IssueReportService`)
- 單位清單: 1 小時 (`DepartmentService`)
- 使用者清單: 1 小時 (`UserManagementService`)

### Razor Pages 模式

**PageModel 結構**:
```csharp
public class CreateModel : PageModel
{
    private readonly IService _service; // 依賴服務介面,不直接注入 DbContext

    [BindProperty] // 用於表單繫結
    public CreateViewModel Input { get; set; }

    public async Task<IActionResult> OnGetAsync() { /* 載入頁面資料 */ }
    public async Task<IActionResult> OnPostAsync() { /* 處理表單提交 */ }
}
```

**表單驗證**: 同時使用客戶端 (jQuery Validation) 與伺服器端 (Data Annotations),驗證失敗回傳 `Page()` 並保留表單狀態

**TempData 訊息**:
- 成功: `TempData["SuccessMessage"] = "操作成功"`
- 錯誤: `TempData["ErrorMessage"] = "操作失敗"`
- 在 `_Layout.cshtml` 或 `_ValidationScriptsPartial.cshtml` 中顯示

### 程式碼風格

- **命名**: PascalCase (public), _camelCase (private fields), UPPER_CASE (constants)
- **Using Directives**: 隱式啟用 (`<ImplicitUsings>enable</ImplicitUsings>`),常見命名空間已自動匯入
- **Nullable**: 啟用 (`<Nullable>enable</Nullable>`),導覽屬性使用 `Entity?`
- **非同步**: 所有資料庫操作使用 `async`/`await`,方法名稱加 `Async` 後綴
- **CancellationToken**: Service 方法接受 `CancellationToken cancellationToken = default`

### 權限控制

**角色定義** (`UserRole` enum):
- `User`: 普通使用者,可管理回報單
- `Admin`: 管理員,額外可存取系統管理功能

**權限驗證**:
- 頁面層級: `options.Conventions.AuthorizePage("/Admin/Users/Index", "Admin")` in `Program.cs`
- Controller 層級: `[Authorize(Roles = "Admin")]` attribute
- 未登入導向: `/Account/Login`
- 權限不足導向: `/Account/AccessDenied`

## 環境配置

**敏感資訊管理**:
- 開發環境使用 `dotnet user-secrets` (不提交到 Git)
- 生產環境使用環境變數或 Azure Key Vault
- **絕對不要** 將資料庫密碼或 LINE Channel Secret 提交到版本控制

```powershell
# 設定 User Secrets
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "LineLogin:ChannelSecret" "your-secret"
```

**必要配置** (參考 `appsettings.Development.json`):
- `ConnectionStrings:DefaultConnection`: Azure SQL Database 連線字串
- `LineLogin:ChannelId`: LINE Developers Console Channel ID
- `LineLogin:ChannelSecret`: LINE Channel Secret
- `LineLogin:CallbackPath`: `/signin-line` (固定)

## 重要檔案參考

- **架構設計**: `specs/001-customer-issue-tracker/spec.md` - User Stories 與 Acceptance Criteria
- **資料模型**: `specs/001-customer-issue-tracker/data-model.md` - 完整的 ERD 與欄位定義
- **服務合約**: `specs/001-customer-issue-tracker/contracts/README.md` - 所有服務介面與 DTO 定義
- **部署指南**: `docs/deployment/DEPLOYMENT.md` - 完整部署流程與故障排除
- **部署檢查清單**: `docs/deployment/IIS-DEPLOYMENT-CHECKLIST.md` - IIS 部署步驟與診斷
- **貢獻指南**: `docs/development/CONTRIBUTING.md` - 程式碼風格、分支策略、PR 檢查清單
- **AI 協作指引**: `docs/development/AGENTS.md` - 專案概覽與開發指令
- **變更記錄**: `docs/changelogs/` - 各功能的變更歷史記錄
- **使用者手冊**: `docs/user-manual.md` - 完整的使用者操作指南

## 新增功能指引

**新增 Service 步驟**:
1. 定義介面於 `Services/Interfaces/INewService.cs`
2. 實作於 `Services/NewService.cs`,遵循現有服務模式 (logging, caching, error handling)
3. 在 `Program.cs` 註冊: `builder.Services.AddScoped<INewService, NewService>()`
4. 建立測試檔案 `Tests/ClarityDesk.UnitTests/Services/NewServiceTests.cs`

**新增實體步驟**:
1. 定義實體於 `Models/Entities/NewEntity.cs`
2. 建立配置於 `Data/Configurations/NewEntityConfiguration.cs` (Fluent API)
3. 在 `ApplicationDbContext` 新增 `DbSet<NewEntity>`
4. 執行 `dotnet ef migrations add AddNewEntity`
5. 建立對應 DTO 於 `Models/DTOs/` 與 Extension Methods 於 `Models/Extensions/`

**新增 Razor Page 步驟**:
1. 建立 PageModel 於 `Pages/FeatureName/PageName.cshtml.cs`,注入必要服務
2. 建立 View 於 `Pages/FeatureName/PageName.cshtml`,使用 Bootstrap 5 樣式
3. 在 `Program.cs` 設定授權: `options.Conventions.AuthorizePage("/FeatureName/PageName")`
4. 加入導覽連結至 `Pages/Shared/_Layout.cshtml`

## 常見陷阱

- ❌ **不要** 在 PageModel 中直接注入 `ApplicationDbContext`,應透過服務介面
- ❌ **不要** 在 Service 方法中使用 `HttpContext`,應從 PageModel 傳入必要參數
- ❌ **不要** 忘記在資料更新後清除相關快取 (`_cache.Remove()`)
- ❌ **不要** 使用 `Find()` 或 `FirstOrDefault()` 而不加 `await`,EF Core 同步方法已過時
- ✅ **務必** 使用 `AsNoTracking()` 進行唯讀查詢以提升效能
- ✅ **務必** 在多對多關聯變更時先移除舊關聯再新增 (參考 `IssueReportService.UpdateIssueReportAsync`)
- ✅ **務必** 使用 `.Include()` 載入導覽屬性避免 N+1 查詢問題

---

**最後更新**: 2025-10-23 | **專案狀態**: Phase 1-4 實作完成,Phase 5 部分進行中 | **目錄結構**: 已整理並分類所有文件檔案
