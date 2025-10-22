# Research Report: 顧客問題紀錄追蹤系統

**Feature**: 001-customer-issue-tracker  
**Date**: 2025-10-20  
**Purpose**: 解決技術選型、整合模式與最佳實踐的研究

## 研究概述

本研究報告針對顧客問題紀錄追蹤系統的關鍵技術決策進行深入分析,確保所有架構選擇符合專案需求、效能目標與憲法原則。

## 研究主題

### 1. LINE Login OAuth 2.0 整合策略

**決策**: 使用 ASP.NET Core 的 OAuth 2.0 中介軟體整合 LINE Login

**理由**:
- LINE 提供完整的 OAuth 2.0 Provider,支援授權碼流程 (Authorization Code Flow)
- ASP.NET Core 內建 `Microsoft.AspNetCore.Authentication.OAuth` 套件,提供標準化的 OAuth 整合模式
- 不需要第三方套件,減少依賴項並提升安全性與可維護性

**實作方式**:
1. 註冊 LINE Developers Console 取得 `Channel ID` 和 `Channel Secret`
2. 設定 Callback URL: `https://yourdomain.com/signin-line`
3. 使用 `AddOAuth` 註冊 LINE OAuth Handler:
   ```csharp
   services.AddAuthentication(options =>
   {
       options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
       options.DefaultChallengeScheme = "LINE";
   })
   .AddCookie()
   .AddOAuth("LINE", options =>
   {
       options.ClientId = Configuration["LineLogin:ChannelId"];
       options.ClientSecret = Configuration["LineLogin:ChannelSecret"];
       options.AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
       options.TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
       options.UserInformationEndpoint = "https://api.line.me/v2/profile";
       options.CallbackPath = "/signin-line";
       options.SaveTokens = true;
   });
   ```

4. 取得使用者資料後,建立或更新本地使用者記錄 (UserID, DisplayName, PictureUrl)

**安全性考量**:
- 使用 HTTPS 傳輸所有 OAuth 請求
- 驗證 `state` 參數防止 CSRF 攻擊
- 儲存 LINE User ID (加密或雜湊) 而非明文 Access Token
- 實作 Token 更新機制 (Refresh Token)

**替代方案評估**:
- ❌ **AspNet.Security.OAuth.Line** (第三方套件): 雖然簡化設定,但增加外部依賴,且更新頻率不如官方套件
- ❌ **手動實作 OAuth 流程**: 增加開發複雜度與安全風險
- ✅ **選擇官方 OAuth 中介軟體**: 平衡安全性、可維護性與開發效率

**參考資源**:
- [LINE Login v2.1 API Reference](https://developers.line.biz/en/docs/line-login/integrate-line-login/)
- [ASP.NET Core OAuth Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/)

---

### 2. Entity Framework Core Code First 最佳實踐

**決策**: 使用 EF Core 8.0 Code First + Fluent API 組態

**理由**:
- Code First 提供版本控制友善的資料庫結構 (Migrations)
- Fluent API 比 Data Annotations 更強大,支援複雜關聯與索引設定
- EF Core 8.0 包含效能改善 (Compiled Models, Query Enhancements)

**最佳實踐**:

1. **分離 Entity Configuration**:
   ```csharp
   // Data/Configurations/UserConfiguration.cs
   public class UserConfiguration : IEntityTypeConfiguration<User>
   {
       public void Configure(EntityTypeBuilder<User> builder)
       {
           builder.HasKey(u => u.Id);
           builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
           builder.HasIndex(u => u.LineUserId).IsUnique();
       }
   }
   ```

2. **使用 Repository Pattern** (選擇性):
   - ⚠️ 專案規模較小,直接使用 DbContext 即可
   - 若未來需要切換資料來源或複雜查詢邏輯,再引入 Repository

3. **效能最佳化**:
   - 為常用查詢欄位建立索引 (IssueStatus, PriorityLevel, RecordDate)
   - 使用 `.AsNoTracking()` 進行唯讀查詢
   - 使用 `.Include()` 避免 N+1 查詢問題
   - 實作分頁 (Skip/Take) 限制資料載入量

4. **Migration 管理**:
   ```bash
   # 新增 Migration
   dotnet ef migrations add InitialCreate
   
   # 套用至資料庫
   dotnet ef database update
   
   # 正式環境使用 SQL Script
   dotnet ef migrations script --idempotent --output migration.sql
   ```

5. **Connection String 安全**:
   - 使用 Azure Key Vault 或 User Secrets 儲存敏感資訊
   - 開發環境: `appsettings.Development.json`
   - 正式環境: Azure App Configuration 或環境變數

**Azure SQL Database 組態**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:claritydesk.database.windows.net,1433;Initial Catalog=ClarityDeskDB;Persist Security Info=False;User ID={your_username};Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

**替代方案評估**:
- ❌ **Database First**: 需要維護 .edmx 檔案,不適合團隊協作
- ❌ **Dapper (Micro ORM)**: 雖然效能更好,但失去 Migrations、Change Tracking 等 ORM 優勢
- ✅ **EF Core Code First**: 平衡開發速度與可維護性

**參考資源**:
- [EF Core Best Practices](https://learn.microsoft.com/en-us/ef/core/miscellaneous/best-practices)
- [EF Core Performance](https://learn.microsoft.com/en-us/ef/core/performance/)

---

### 3. IIS 部署與組態最佳化

**決策**: 使用 In-Process Hosting Model 部署於 IIS 10.0+

**理由**:
- In-Process 模式比 Out-of-Process 效能提升 ~4-5 倍
- 簡化部署流程,單一 IIS Application Pool 管理
- 支援 Windows 整合驗證 (若未來需要)

**部署步驟**:

1. **安裝 ASP.NET Core Runtime**:
   - 下載 [.NET 8.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
   - 安裝後重啟 IIS: `iisreset`

2. **發佈應用程式**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. **建立 IIS Application**:
   - 建立應用程式池 (Application Pool): `ClarityDeskPool`
   - .NET CLR Version: **No Managed Code** (使用 .NET Core Runtime)
   - Managed Pipeline Mode: **Integrated**
   - Identity: **ApplicationPoolIdentity** (最小權限原則)

4. **設定 web.config**:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <location path="." inheritInChildApplications="false">
       <system.webServer>
         <handlers>
           <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
         </handlers>
         <aspNetCore processPath="dotnet"
                     arguments=".\ClarityDesk.dll"
                     stdoutLogEnabled="true"
                     stdoutLogFile=".\logs\stdout"
                     hostingModel="inprocess" />
       </system.webServer>
     </location>
   </configuration>
   ```

5. **HTTPS 設定**:
   - 使用 IIS 內建 SSL Certificate 綁定
   - 或使用 Let's Encrypt 免費憑證 (透過 Win-ACME)

**效能調校**:
- **回應壓縮**: 啟用 `UseResponseCompression()` (Gzip/Brotli)
- **靜態檔案快取**: 設定 `Cache-Control` 標頭 (365天)
- **Minification**: 使用 `WebOptimizer` 或 `BuildBundlerMinifier` 壓縮 CSS/JS
- **Connection Pool**: Azure SQL 連線池設定 `Min Pool Size=10; Max Pool Size=100`

**監控與日誌**:
- 啟用 Application Insights SDK
- 使用 Serilog 寫入結構化日誌 (檔案 + Azure Log Analytics)
- 設定 IIS Failed Request Tracing 診斷問題

**安全性強化**:
- 移除不必要的 HTTP Headers (`X-Powered-By`, `Server`)
- 設定 Content Security Policy (CSP) Headers
- 啟用 HSTS (HTTP Strict Transport Security)
- 設定 Application Pool Recycling (每日 3:00 AM)

**替代方案評估**:
- ❌ **Kestrel Standalone**: 需要額外的反向代理 (Nginx),增加維運複雜度
- ❌ **Azure App Service**: 成本較高,且專案明確要求 Windows IIS
- ✅ **IIS + In-Process Hosting**: 符合需求且效能最佳

**參考資源**:
- [Host ASP.NET Core on Windows with IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [ASP.NET Core In-Process Hosting](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/in-process-hosting)

---

### 4. 不使用 Redis 的替代快取策略

**決策**: 使用 ASP.NET Core 內建 `IMemoryCache` + Cookie-Based Session

**理由**:
- 專案規模較小 (~50 使用者),不需要分散式快取
- 部署於單一 IIS 伺服器,無需跨機器共享快取
- 減少外部依賴與維運成本

**快取策略**:

1. **應用程式層快取 (IMemoryCache)**:
   ```csharp
   // 快取參考資料 (單位清單、使用者清單)
   public async Task<List<Department>> GetDepartmentsAsync()
   {
       return await _memoryCache.GetOrCreateAsync("departments", async entry =>
       {
           entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
           return await _dbContext.Departments.Where(d => d.IsActive).ToListAsync();
       });
   }
   ```

2. **回應快取 (Response Caching)**:
   ```csharp
   // 快取靜態內容或不常變動的頁面
   [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "page", "filter" })]
   public async Task<IActionResult> OnGetAsync()
   {
       // ...
   }
   ```

3. **會話管理 (Cookie-Based Session)**:
   ```csharp
   services.AddSession(options =>
   {
       options.IdleTimeout = TimeSpan.FromDays(365); // 永久會話
       options.Cookie.HttpOnly = true;
       options.Cookie.IsEssential = true;
       options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
   });
   ```

**限制與考量**:
- ⚠️ **記憶體限制**: 監控應用程式記憶體使用量,避免 OutOfMemory
- ⚠️ **應用程式重啟**: IMemoryCache 在應用程式重啟後會清空,需重新載入
- ⚠️ **負載平衡**: 若未來擴展至多台伺服器,需使用 Sticky Sessions 或改用 Redis

**何時需要升級至 Redis**:
- 並發使用者 > 100 人
- 需要負載平衡 (多台 IIS 伺服器)
- 需要跨應用程式共享快取
- 會話資料 > 50MB

**替代方案評估**:
- ❌ **SQL Server Session State**: 效能較差,增加資料庫負擔
- ❌ **Redis**: 過度設計,增加維運複雜度與成本
- ✅ **IMemoryCache + Cookie Session**: 簡單且符合專案規模

**參考資源**:
- [ASP.NET Core Memory Cache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [ASP.NET Core Session State](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state#session-state)

---

### 5. POCO 資料映射策略 (不使用 AutoMapper)

**決策**: 使用擴充方法 (Extension Methods) 手動映射 Entity ↔ DTO

**理由**:
- AutoMapper 增加複雜度與學習曲線
- 手動映射提供更明確的控制與可讀性
- 效能更好 (無反射開銷)
- 專案實體數量少 (~6-8 個),手動映射成本可控

**實作模式**:

1. **Entity → DTO 映射**:
   ```csharp
   // Models/Extensions/IssueReportExtensions.cs
   public static class IssueReportExtensions
   {
       public static IssueReportDto ToDto(this IssueReport entity)
       {
           return new IssueReportDto
           {
               Id = entity.Id,
               Title = entity.Title,
               Content = entity.Content,
               RecordDate = entity.RecordDate,
               Status = entity.Status.ToString(),
               Priority = entity.Priority.ToString(),
               AssignedUserName = entity.AssignedUser?.DisplayName,
               ReporterName = entity.ReporterName,
               CustomerName = entity.CustomerName,
               CustomerPhone = entity.CustomerPhone,
               DepartmentNames = entity.Departments.Select(d => d.Name).ToList(),
               CreatedAt = entity.CreatedAt,
               UpdatedAt = entity.UpdatedAt
           };
       }
   }
   ```

2. **DTO → Entity 映射** (Create):
   ```csharp
   public static IssueReport ToEntity(this CreateIssueReportDto dto)
   {
       return new IssueReport
       {
           Title = dto.Title,
           Content = dto.Content,
           RecordDate = dto.RecordDate,
           Status = Enum.Parse<IssueStatus>(dto.Status),
           Priority = Enum.Parse<PriorityLevel>(dto.Priority),
           ReporterName = dto.ReporterName,
           CustomerName = dto.CustomerName,
           CustomerPhone = dto.CustomerPhone,
           CreatedAt = DateTime.UtcNow
       };
   }
   ```

3. **部分更新映射** (Update):
   ```csharp
   public static void UpdateFromDto(this IssueReport entity, UpdateIssueReportDto dto)
   {
       entity.Title = dto.Title;
       entity.Content = dto.Content;
       entity.RecordDate = dto.RecordDate;
       entity.Status = Enum.Parse<IssueStatus>(dto.Status);
       entity.Priority = Enum.Parse<PriorityLevel>(dto.Priority);
       entity.AssignedUserId = dto.AssignedUserId;
       entity.UpdatedAt = DateTime.UtcNow;
   }
   ```

**測試策略**:
- 為每個映射方法撰寫單元測試
- 驗證所有欄位正確映射
- 測試邊界情況 (null 值、空集合)

**替代方案評估**:
- ❌ **AutoMapper**: 過度設計,增加依賴與複雜度
- ❌ **Mapster**: 雖然效能更好,但仍為第三方依賴
- ✅ **手動映射 (擴充方法)**: 簡單、明確、可測試

**參考資源**:
- [C# Extension Methods Best Practices](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)

---

### 6. 響應式設計實作策略

**決策**: 使用 Bootstrap 5 + 自訂 CSS 變數

**理由**:
- Bootstrap 5 提供完整的 RWD Grid System 與元件
- 移除 jQuery 依賴 (Bootstrap 5 使用原生 JavaScript)
- 支援 CSS 變數,方便自訂主題色彩
- 符合 WCAG 2.1 AA 無障礙標準

**實作方式**:

1. **自訂 Bootstrap 主題** (商務白風格):
   ```css
   /* wwwroot/css/site.css */
   :root {
       --primary-color: #2563eb;      /* 淺藍色 */
       --secondary-color: #64748b;    /* 灰藍色 */
       --success-color: #10b981;      /* 綠色 */
       --warning-color: #f59e0b;      /* 橙色 */
       --danger-color: #ef4444;       /* 紅色 */
       --background-color: #ffffff;   /* 白色背景 */
       --card-background: #f8fafc;    /* 淺灰背景 */
       --border-radius: 0.75rem;      /* 圓角 */
       --box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
   }
   
   .card {
       background: var(--card-background);
       border-radius: var(--border-radius);
       box-shadow: var(--box-shadow);
       border: none;
   }
   ```

2. **中斷點 (Breakpoints)**:
   - Extra Small: < 576px (手機直向)
   - Small: ≥ 576px (手機橫向)
   - Medium: ≥ 768px (平板)
   - Large: ≥ 992px (桌面)
   - Extra Large: ≥ 1200px (大螢幕)

3. **表格響應式**:
   ```html
   <!-- 使用 Bootstrap 響應式表格 -->
   <div class="table-responsive">
       <table class="table">
           <!-- ... -->
       </table>
   </div>
   ```

4. **表單響應式**:
   ```html
   <!-- 使用 Grid System -->
   <div class="row">
       <div class="col-12 col-md-6">
           <label>標題</label>
           <input type="text" class="form-control" />
       </div>
       <div class="col-12 col-md-6">
           <label>日期</label>
           <input type="date" class="form-control" />
       </div>
   </div>
   ```

**測試工具**:
- Chrome DevTools Device Mode
- BrowserStack (跨瀏覽器測試)
- Lighthouse Accessibility Audit

**替代方案評估**:
- ❌ **Tailwind CSS**: 學習曲線較陡,且需要建置工具
- ❌ **Material Design**: 風格與商務白風格不符
- ✅ **Bootstrap 5**: 成熟、文件完整、社群支援強

**參考資源**:
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.3/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

---

## 研究結論

### 技術堆疊總結

| 層級 | 技術選擇 | 理由 |
|------|---------|------|
| 前端框架 | Bootstrap 5 + Vanilla JS | 簡單、成熟、符合響應式需求 |
| 後端框架 | ASP.NET Core 8 Razor Pages | 頁面導向架構,適合 CRUD 操作 |
| 資料存取 | Entity Framework Core 8 | Code First + Migrations,開發效率高 |
| 資料庫 | Azure SQL Database | 雲端託管,高可用性與可擴展性 |
| 身份驗證 | LINE Login OAuth 2.0 | 官方支援,安全且使用者友善 |
| 快取策略 | IMemoryCache + Cookie Session | 簡單且符合專案規模 |
| 測試框架 | xUnit + FluentAssertions + Moq | 憲法要求,社群標準 |
| 部署環境 | Windows IIS 10+ (In-Process) | 符合需求,效能最佳 |

### 關鍵風險與緩解策略

| 風險 | 影響 | 緩解策略 |
|------|------|---------|
| LINE Login API 變更 | 高 | 使用官方 SDK,定期檢查更新通知 |
| Azure SQL 連線失敗 | 高 | 實作 Retry Policy,監控連線池 |
| IIS 應用程式池回收 | 中 | 設定預熱 (Warm-up),使用 Application Initialization |
| 記憶體快取不足 | 低 | 設定快取過期時間,監控記憶體使用量 |
| 瀏覽器相容性 | 低 | 使用 Polyfill,瀏覽器測試自動化 |

### 下一步行動

Phase 0 研究已完成,所有技術決策已確認。接下來進入 **Phase 1: 設計與合約**階段:

1. ✅ 建立 `data-model.md` 定義資料庫結構
2. ✅ 建立 `contracts/` 定義服務介面
3. ✅ 建立 `quickstart.md` 提供開發環境設定指南
4. ✅ 執行 `update-agent-context.ps1` 更新 Copilot 上下文

---

**研究人員**: GitHub Copilot  
**審查狀態**: ✅ 已完成  
**最後更新**: 2025-10-20
