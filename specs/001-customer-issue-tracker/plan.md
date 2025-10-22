# Implementation Plan: 顧客問題紀錄追蹤系統

**Branch**: `001-customer-issue-tracker` | **Date**: 2025-10-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-customer-issue-tracker/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

本功能實作一個顧客問題紀錄追蹤系統,讓使用者可以快速建立、管理、追蹤顧客回報單。系統採用 ASP.NET Core 8 Razor Pages 架構,使用 Entity Framework Core Code First 連接 Azure SQL Database,整合 LINE Login 提供身份驗證,並支援角色權限管理(普通使用者/管理人員)。部署於 Windows IIS 環境。

## Technical Context

**Language/Version**: C# / .NET Core 8.0  
**Primary Framework**: ASP.NET Core Razor Pages  
**Primary Dependencies**:

- Entity Framework Core 8.0 (資料存取)
- Microsoft.AspNetCore.Authentication.OAuth (LINE Login 整合)
- Microsoft.Data.SqlClient (Azure SQL 連線)
- 不使用 AutoMapper (採用 POCO 直接映射)
- 不使用 Redis (會話與快取使用替代方案)

**Storage**: Azure SQL Database (透過 EF Core Code First workflow)  
**Testing**: xUnit + FluentAssertions + Moq (依據憲法要求)  
**Target Platform**: Windows Server + IIS 10.0+  
**Authentication**: LINE Login OAuth 2.0 整合

**Project Type**: Web Application (ASP.NET Core Razor Pages)  
**Performance Goals**:

- 頁面載入 < 2秒 (100筆回報單)
- 篩選查詢回應 < 1秒
- 支援 50 位同時在線使用者

**Constraints**:

- 頁面載入時間 < 200ms (p95)
- 資料庫查詢 < 50ms (p95)
- 記憶體使用 < 50MB per request
- 必須支援響應式設計 (320px - 1920px)

**Scale/Scope**:

- 預期使用者: ~50 人
- 預估回報單數量: 初期 1000 筆,成長至 10000+ 筆
- 主要頁面數: ~15 個 Razor Pages
- 資料表數量: ~6-8 個核心實體

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. 程式碼品質與可維護性 ✅

- **整潔架構**: 採用 ASP.NET Core Razor Pages 的分層架構 (Pages/Models/Services/Data),符合關注點分離原則
- **SOLID 原則**: 服務層將遵循依賴注入,介面導向設計
- **命名慣例**: 遵循 Microsoft C# 命名規範 (PascalCase for public, _camelCase for private fields)
- **可空參考型別**: .NET 8 專案預設啟用,將正確標註所有可空性
- **依賴注入**: 使用 ASP.NET Core 內建 DI 容器註冊所有服務
- **組態管理**: 使用 appsettings.json 搭配強型別 Options 類別 (LINE Login 設定、Connection String 等)
- **錯誤處理**: 實作自訂例外類別處理領域錯誤,使用 try-catch-log 模式

**評估**: ✅ 完全合規

### II. 測試優先開發 ✅

- **TDD 循環**: 遵循紅-綠-重構循環,先撰寫測試再實作
- **測試覆蓋率**: 服務層與業務邏輯目標 80%+ 單元測試覆蓋率
- **測試類型**:
  - 單元測試: 使用 xUnit 測試 Services、Models 的業務邏輯
  - 整合測試: 測試 EF Core 資料庫操作、LINE Login OAuth 流程
  - UI 測試: Razor PageModel 測試驗證使用者操作流程
- **測試框架**: xUnit + FluentAssertions + Moq
- **測試命名**: `[MethodName]_[Scenario]_[ExpectedResult]` 格式
- **測試獨立性**: 每個測試使用獨立的資料庫 Context 或 In-Memory Database

**評估**: ✅ 完全合規

### III. 使用者體驗一致性 ✅

- **響應式設計**: 使用 Bootstrap 5 實現 RWD,測試 320px-1920px 各尺寸
- **無障礙性**: 符合 WCAG 2.1 AA 標準
  - 使用語意化 HTML5 標籤 (`<nav>`, `<main>`, `<section>`)
  - 所有表單欄位包含 `<label>` 與適當的 `aria-*` 屬性
  - 顏色對比度符合 4.5:1 (一般文字)、3:1 (大型文字)
  - 鍵盤導覽支援 (Tab 順序、Enter/Space 觸發)
- **視覺一致性**: 使用共享 `_Layout.cshtml`,統一 CSS 類別與元件模式
- **表單驗證**: jQuery Validation (客戶端) + Data Annotations (伺服器端)雙重驗證
- **載入狀態**: AJAX 操作顯示 loading spinner,骨架螢幕用於資料載入
- **錯誤處理**: 友善錯誤訊息,使用 `_ValidationScriptsPartial.cshtml`,正式環境隱藏技術細節
- **瀏覽器支援**: Chrome、Firefox、Edge、Safari 最新兩版本

**評估**: ✅ 完全合規

### IV. 效能與可擴展性 ✅

- **回應時間標準**:
  - 頁面載入 (SSR): < 200ms (p95) ✅
  - API 回應: < 100ms (p95) ✅ (Razor Pages 為 SSR,無獨立 API)
  - 資料庫查詢: < 50ms (p95) ✅ (使用索引最佳化)
- **資源限制**:
  - 每個請求記憶體: < 50MB ✅
  - CPU 使用率: < 30% ✅
  - Azure SQL Database 連線池: 最小 10、最大 100 ✅
- **最佳化要求**:
  - 資料庫索引: 為常用查詢欄位建立索引 (處理狀態、緊急程度、日期)
  - 避免 N+1: 使用 `.Include()` Eager Loading 載入關聯資料
  - 靜態資產: 使用 ASP.NET Core bundling & minification,啟用 Response Compression
  - 圖片最佳化: 使用壓縮圖片,考慮 WebP 格式
  - Async/Await: 所有 I/O 操作使用非同步模式
- **監控**: 整合 Application Insights 監控效能與錯誤
- **負載測試**: 使用 Apache JMeter 或 k6 進行負載測試 (模擬 50-100 並發使用者)

**快取策略說明** (不使用 Redis):
- 使用 ASP.NET Core 內建 `IMemoryCache` 進行應用程式層快取 (參考資料、單位清單)
- 使用 `ResponseCacheAttribute` 進行回應快取 (靜態內容)
- 會話狀態使用 Cookie-based Session (適合 IIS 環境)

**評估**: ✅ 完全合規 (已調整快取策略以符合不使用 Redis 的需求)

### V. 文件與溝通 ✅

- **文件語言**: 所有規格、計畫、研究文件使用繁體中文 (zh-TW) ✅
- **程式碼註解**: XML 文件註解與重要邏輯註解使用繁體中文 ✅
- **使用者介面**: 全站繁體中文 UI,錯誤訊息、提示文字皆為中文 ✅
- **API 文件**: 服務介面與資料模型使用 XML 註解記錄 (繁體中文) ✅
- **技術文件**: 架構決策、操作手冊使用繁體中文 ✅
- **範本一致性**: 遵循 `.specify/templates/` 範本結構 ✅
- **版本控制**: 文件與程式碼同步更新 ✅

**評估**: ✅ 完全合規

### 總體評估

**狀態**: ✅ **通過所有憲法檢查**

所有五大核心原則均符合專案憲法要求。無需記錄任何違規或權衡決策。專案架構選擇 (ASP.NET Core Razor Pages + EF Core + Azure SQL) 完全符合憲法框架,並已針對不使用 Redis 的需求調整快取策略。

## Project Structure

### Documentation (this feature)

```text
specs/001-customer-issue-tracker/
├── spec.md              # 功能規格說明
├── plan.md              # 本檔案 (實作計畫)
├── research.md          # Phase 0 輸出 (技術研究)
├── data-model.md        # Phase 1 輸出 (資料模型)
├── quickstart.md        # Phase 1 輸出 (快速開始指南)
├── contracts/           # Phase 1 輸出 (服務合約)
│   ├── IIssueReportService.cs
│   ├── IAuthenticationService.cs
│   ├── IUserManagementService.cs
│   └── IDepartmentService.cs
└── checklists/
    └── requirements.md  # 需求檢查清單
```

### Source Code (repository root)

```text
ClarityDesk/
├── Pages/                          # Razor Pages (UI 層)
│   ├── Issues/                     # 回報單管理頁面
│   │   ├── Index.cshtml           # 回報單列表
│   │   ├── Index.cshtml.cs        # 回報單列表 PageModel
│   │   ├── Create.cshtml          # 建立回報單
│   │   ├── Create.cshtml.cs       # 建立回報單 PageModel
│   │   ├── Edit.cshtml            # 編輯回報單
│   │   ├── Edit.cshtml.cs         # 編輯回報單 PageModel
│   │   ├── Details.cshtml         # 回報單詳情
│   │   └── Details.cshtml.cs      # 回報單詳情 PageModel
│   ├── Admin/                      # 管理功能頁面
│   │   ├── Users/                 # 使用者權限管理
│   │   │   ├── Index.cshtml
│   │   │   └── Index.cshtml.cs
│   │   └── Departments/           # 單位維護
│   │       ├── Index.cshtml
│   │       ├── Index.cshtml.cs
│   │       ├── Create.cshtml
│   │       ├── Create.cshtml.cs
│   │       ├── Edit.cshtml
│   │       └── Edit.cshtml.cs
│   ├── Account/                    # 身份驗證頁面
│   │   ├── Login.cshtml
│   │   ├── Login.cshtml.cs
│   │   ├── Logout.cshtml.cs
│   │   └── AccessDenied.cshtml
│   ├── Shared/                     # 共享版面配置與元件
│   │   ├── _Layout.cshtml
│   │   ├── _Layout.cshtml.css
│   │   ├── _ValidationScriptsPartial.cshtml
│   │   └── Components/            # ViewComponents
│   │       └── PriorityBadge/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Index.cshtml                # 首頁
│   ├── Index.cshtml.cs
│   ├── Privacy.cshtml
│   ├── Privacy.cshtml.cs
│   ├── Error.cshtml
│   └── Error.cshtml.cs
│
├── Models/                         # 資料模型 (領域層)
│   ├── Entities/                   # EF Core 實體
│   │   ├── User.cs
│   │   ├── IssueReport.cs
│   │   ├── Department.cs
│   │   └── DepartmentAssignment.cs
│   ├── Enums/                      # 列舉型別
│   │   ├── UserRole.cs
│   │   ├── IssueStatus.cs
│   │   └── PriorityLevel.cs
│   ├── ViewModels/                 # 頁面顯示模型
│   │   ├── IssueReportViewModel.cs
│   │   ├── IssueListViewModel.cs
│   │   ├── UserManagementViewModel.cs
│   │   └── DepartmentViewModel.cs
│   └── DTOs/                       # 資料傳輸物件 (POCO)
│       ├── CreateIssueReportDto.cs
│       ├── UpdateIssueReportDto.cs
│       ├── IssueFilterDto.cs
│       └── LineUserProfileDto.cs
│
├── Services/                       # 服務層 (業務邏輯)
│   ├── Interfaces/                 # 服務介面
│   │   ├── IIssueReportService.cs
│   │   ├── IAuthenticationService.cs
│   │   ├── IUserManagementService.cs
│   │   └── IDepartmentService.cs
│   ├── IssueReportService.cs
│   ├── AuthenticationService.cs
│   ├── UserManagementService.cs
│   └── DepartmentService.cs
│
├── Data/                           # 資料存取層
│   ├── ApplicationDbContext.cs     # EF Core DbContext
│   ├── Configurations/             # EF Core Entity Configurations
│   │   ├── UserConfiguration.cs
│   │   ├── IssueReportConfiguration.cs
│   │   ├── DepartmentConfiguration.cs
│   │   └── DepartmentAssignmentConfiguration.cs
│   ├── Migrations/                 # EF Core Migrations
│   └── Repositories/               # 資料存取介面 (若需要)
│       ├── IRepository.cs
│       └── Repository.cs
│
├── Infrastructure/                 # 基礎設施層
│   ├── Authentication/             # 身份驗證相關
│   │   ├── LineAuthenticationHandler.cs
│   │   └── LineAuthenticationOptions.cs
│   ├── Middleware/                 # 自訂 Middleware
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Extensions/                 # 擴充方法
│   │   └── ServiceCollectionExtensions.cs
│   └── Filters/                    # 自訂 Filters
│       └── AuthorizationFilter.cs
│
├── wwwroot/                        # 靜態資源
│   ├── css/
│   │   └── site.css
│   ├── js/
│   │   └── site.js
│   ├── lib/                        # 前端套件
│   │   ├── bootstrap/
│   │   ├── jquery/
│   │   └── jquery-validation/
│   └── images/                     # 圖片資源
│
├── Tests/                          # 測試專案
│   ├── ClarityDesk.UnitTests/
│   │   ├── Services/
│   │   │   ├── IssueReportServiceTests.cs
│   │   │   ├── AuthenticationServiceTests.cs
│   │   │   ├── UserManagementServiceTests.cs
│   │   │   └── DepartmentServiceTests.cs
│   │   ├── Models/
│   │   └── Helpers/
│   │       └── TestDataBuilder.cs
│   └── ClarityDesk.IntegrationTests/
│       ├── Pages/
│       │   ├── IssuesPageTests.cs
│       │   └── AdminPageTests.cs
│       ├── Data/
│       │   └── ApplicationDbContextTests.cs
│       └── Infrastructure/
│           └── LineAuthenticationTests.cs
│
├── Program.cs                      # 應用程式進入點
├── appsettings.json                # 組態檔
├── appsettings.Development.json    # 開發環境組態
├── ClarityDesk.csproj             # 專案檔
├── ClarityDesk.sln                # 解決方案檔
└── web.config                      # IIS 部署設定 (將新增)
```

**結構說明**:

1. **採用 Razor Pages 架構**: 選擇 ASP.NET Core Razor Pages 而非 MVC,因為此專案以頁面為中心的 CRUD 操作較多,Razor Pages 更簡潔直觀
2. **清晰的分層架構**: Pages (UI) → Services (業務邏輯) → Data (資料存取)
3. **領域驅動設計**: Models 分為 Entities (EF Core 實體)、ViewModels (UI 展示)、DTOs (資料傳輸,POCO 風格)
4. **測試分離**: 獨立的測試專案,包含單元測試與整合測試
5. **基礎設施層**: 包含身份驗證、Middleware、擴充方法等跨領域關注點
6. **符合憲法要求**: 遵循關注點分離、依賴注入、測試優先原則

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

