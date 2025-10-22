# 貢獻指南

感謝您對 ClarityDesk 專案感興趣!我們歡迎所有形式的貢獻,包括但不限於:

- 回報問題與錯誤
- 提出新功能建議
- 撰寫與改進文件
- 提交程式碼修正與新功能

## 開發流程

### 1. Fork 專案

點擊 GitHub 頁面右上角的 "Fork" 按鈕,建立自己的副本。

### 2. 複製到本地

```bash
git clone https://github.com/your-username/ClarityDesk.git
cd ClarityDesk
```

### 3. 建立功能分支

```bash
git checkout -b feature/your-feature-name
```

分支命名慣例:
- `feature/功能名稱` - 新功能
- `bugfix/問題描述` - 錯誤修正
- `docs/文件內容` - 文件更新
- `refactor/重構內容` - 程式碼重構

### 4. 進行開發

遵循專案的程式碼風格和架構設計原則 (詳見下方)。

### 5. 提交變更

```bash
git add .
git commit -m "feat: 新增使用者匯出功能"
```

Commit 訊息格式:
- `feat:` - 新功能
- `fix:` - 錯誤修正
- `docs:` - 文件更新
- `style:` - 程式碼格式調整 (不影響功能)
- `refactor:` - 程式碼重構
- `test:` - 測試相關
- `chore:` - 建置流程或輔助工具變動

### 6. 推送到 GitHub

```bash
git push origin feature/your-feature-name
```

### 7. 建立 Pull Request

1. 前往 GitHub 上您的 Fork 專案
2. 點擊 "Compare & pull request"
3. 填寫詳細的變更說明
4. 等待程式碼審查

## 程式碼風格

### C# 程式碼規範

- 遵循 [Microsoft C# 編碼慣例](https://docs.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 PascalCase 命名 public 成員
- 使用 camelCase 命名 private 欄位,並加上 `_` 前綴 (如 `_context`)
- 使用 `var` 當型別明顯時
- 一律加上存取修飾詞 (`public`, `private`, `protected`)

```csharp
// ✅ 好的做法
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserService> _logger;

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user?.ToDto();
    }
}

// ❌ 不好的做法
public class userService
{
    ApplicationDbContext context;
    
    public UserDto GetUserById(int id)
    {
        UserDto user = context.Users.Find(id).ToDto();
        return user;
    }
}
```

### Razor Pages 規範

- 使用 Bootstrap 5 類別進行樣式設計
- 保持 PageModel 簡潔,業務邏輯放在 Service 層
- 使用 ViewModels 傳遞複雜資料
- 表單驗證同時使用客戶端與伺服器端驗證

```csharp
// ✅ 好的做法
public class IndexModel : PageModel
{
    private readonly IIssueReportService _service;

    public IndexModel(IIssueReportService service)
    {
        _service = service;
    }

    public PagedResult<IssueReportDto> Issues { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        var filter = new IssueFilterDto { CurrentPage = page };
        Issues = await _service.GetIssueReportsAsync(filter);
    }
}

// ❌ 不好的做法
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public async Task OnGetAsync(int page = 1)
    {
        // 直接在 PageModel 中查詢資料庫
        var issues = await _context.IssueReports.ToListAsync();
    }
}
```

### 命名規範

- **檔案名稱**: 與類別名稱一致
- **資料夾**: PascalCase (如 `Services`, `Models`)
- **變數**: camelCase
- **常數**: UPPER_CASE
- **介面**: I 開頭 + PascalCase (如 `IUserService`)

## 測試要求

### 單元測試

- 所有 Service 層的 public 方法都必須有單元測試
- 使用 xUnit + FluentAssertions + Moq
- 測試命名: `[MethodName]_[Scenario]_[ExpectedResult]`
- 目標覆蓋率: 80% 以上

```csharp
[Fact]
public async Task GetUserByIdAsync_ExistingUser_ReturnsUserDto()
{
    // Arrange
    var context = CreateInMemoryDbContext();
    var service = new UserService(context, Mock.Of<ILogger<UserService>>());
    
    // Act
    var result = await service.GetUserByIdAsync(1);
    
    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(1);
}
```

### 整合測試

- 測試完整的請求-回應流程
- 使用 In-Memory Database
- 驗證資料庫操作正確性

## Pull Request 檢查清單

提交 PR 前,請確認以下事項:

- [ ] 程式碼遵循專案風格規範
- [ ] 已新增/更新相關單元測試
- [ ] 所有測試通過 (`dotnet test`)
- [ ] 已更新相關文件 (如有需要)
- [ ] Commit 訊息清晰且遵循格式
- [ ] 無未解決的 merge conflicts
- [ ] PR 描述清楚說明變更內容
- [ ] 已自行測試變更功能

## 回報問題

### Bug 回報

請提供以下資訊:

1. **問題描述**: 清楚描述發生了什麼問題
2. **重現步驟**: 詳細列出重現問題的步驟
3. **預期行為**: 說明應該出現什麼結果
4. **實際行為**: 說明實際發生了什麼
5. **環境資訊**:
   - 作業系統 (Windows Server 版本)
   - .NET 版本
   - 瀏覽器 (如適用)
6. **螢幕截圖**: 如有必要,附上截圖
7. **錯誤訊息**: 完整的錯誤訊息與堆疊追蹤

### 功能請求

請說明:

1. **使用情境**: 誰會用到這個功能?
2. **問題陳述**: 目前有什麼不便?
3. **建議解決方案**: 您期望的功能如何運作?
4. **替代方案**: 是否有其他可行方案?

## 審查流程

1. **自動檢查**: PR 會自動執行測試與程式碼品質檢查
2. **程式碼審查**: 維護者會審查您的程式碼
3. **修正回饋**: 根據回饋進行修正
4. **合併**: 審查通過後合併至主分支

## 開發環境設定

### 必要工具

- Visual Studio 2022 或 VS Code
- .NET 8.0 SDK
- SQL Server 或 Azure SQL Database
- Git

### 推薦擴充功能 (VS Code)

- C# Dev Kit
- SQL Server (mssql)
- GitLens
- Prettier
- ESLint

### 設定開發環境

```bash
# 1. 安裝相依套件
dotnet restore

# 2. 設定使用者機密 (避免敏感資訊外洩)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "LineLogin:ChannelId" "your-channel-id"
dotnet user-secrets set "LineLogin:ChannelSecret" "your-channel-secret"

# 3. 執行 Migration
dotnet ef database update

# 4. 執行專案
dotnet run

# 5. 執行測試
dotnet test
```

## 專案架構

```
ClarityDesk/
├── Pages/          # Razor Pages (UI 層)
├── Models/         # 資料模型 (Entities, DTOs, ViewModels)
├── Services/       # 服務層 (業務邏輯)
├── Data/           # 資料存取層 (DbContext, Configurations)
├── Infrastructure/ # 基礎設施 (Authentication, Middleware)
└── Tests/          # 測試專案
```

### 分層架構原則

1. **UI 層 (Pages)**: 僅負責顯示與使用者互動,不包含業務邏輯
2. **服務層 (Services)**: 包含所有業務邏輯,透過介面提供服務
3. **資料層 (Data)**: 負責資料庫操作,使用 EF Core
4. **基礎設施層 (Infrastructure)**: 跨領域關注點 (身份驗證、日誌、例外處理)

### 依賴方向

```
Pages → Services → Data
  ↓         ↓
Infrastructure
```

- Pages 依賴 Services 介面
- Services 依賴 Data (DbContext)
- 所有層都可依賴 Infrastructure

## 社群準則

- **尊重**: 尊重所有貢獻者,保持友善與專業
- **建設性**: 提供建設性的回饋,避免純粹批評
- **耐心**: 理解大家都是志願貢獻,回應可能需要時間
- **開放**: 歡迎不同意見,透過討論達成共識

## 授權

貢獻到本專案的程式碼將採用 MIT 授權條款釋出。提交 PR 即表示您同意將您的貢獻以相同授權釋出。

## 聯絡方式

- **GitHub Issues**: 用於回報問題與功能請求
- **Pull Requests**: 用於提交程式碼變更
- **Email**: support@claritydesk.com (一般查詢)

---

再次感謝您的貢獻!🎉
