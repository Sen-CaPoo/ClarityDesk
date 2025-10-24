# 時區修正記錄 (2025-10-24)

## 問題描述

系統中的時間顯示存在時區問題:
1. LINE 推播訊息顯示的時間為 UTC 時間(例如 07:05),而非台北時間(應為 15:05)
2. 網頁上顯示的時間有些使用 `.ToLocalTime()`,有些直接顯示,不一致
3. 需要統一使用台灣台北時間 (UTC+8) 顯示所有時間

## 解決方案

### 1. 建立時區轉換輔助類別

**檔案**: `Infrastructure/Helpers/TimeZoneHelper.cs`

```csharp
public static class TimeZoneHelper
{
    private static readonly TimeZoneInfo TaipeiTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    // 將 UTC 時間轉換為台北時間
    public static DateTime ConvertToTaipeiTime(DateTime utcTime)
    
    // 將台北時間轉換為 UTC 時間
    public static DateTime ConvertToUtc(DateTime taipeiTime)
    
    // 取得當前台北時間
    public static DateTime GetTaipeiNow()
}
```

### 2. 建立 Tag Helper 統一時間顯示

**檔案**: `Infrastructure/TagHelpers/TaipeiTimeTagHelper.cs`

用法:
```html
<taipei-time value="@Model.CreatedAt" format="yyyy/MM/dd HH:mm:ss" />
```

### 3. 資料儲存策略

**統一策略**: 所有時間在資料庫中統一以 UTC 時間儲存

- `ApplicationDbContext.UpdateTimestamps()` 使用 `DateTime.UtcNow`
- `LineConversationService` 建立回報單時使用 `DateTime.UtcNow`
- `Pages/Issues/Create.cshtml.cs` 提交時轉換為 UTC
- `Pages/Issues/Edit.cshtml.cs` 提交時轉換為 UTC

### 4. 時間顯示策略

**統一策略**: 所有顯示給使用者的時間轉換為台北時間

#### LINE 推播訊息

**檔案**: `Services/LineMessagingService.cs`

- 使用 `TimeZoneHelper.ConvertToTaipeiTime(issueReport.CreatedAt)` 轉換後顯示
- 訊息格式: `📅 紀錄日期:2025/10/24 15:30` (台北時間)

#### 網頁顯示

所有 Razor 頁面統一使用 `<taipei-time>` Tag Helper:

- `Pages/Issues/Details.cshtml` - 回報單詳情
- `Pages/Issues/Index.cshtml` - 回報單列表
- `Pages/Admin/Users/Index.cshtml` - 使用者列表
- `Pages/Admin/Departments/Index.cshtml` - 單位列表

#### Excel 匯出

**檔案**: `Pages/Issues/Index.cshtml.cs`

- 所有時間欄位使用 `TimeZoneHelper.ConvertToTaipeiTime()` 轉換
- 檔案名稱使用 `TimeZoneHelper.GetTaipeiNow()` 生成

### 5. 表單輸入處理

**檔案**: 
- `Pages/Issues/Create.cshtml.cs`
- `Pages/Issues/Edit.cshtml.cs`

**流程**:
1. **OnGetAsync**: 將 UTC 時間轉換為台北時間,並設定 `DateTimeKind.Unspecified`,讓 `datetime-local` 輸入控制項正確顯示
2. **OnPostAsync**: 將瀏覽器傳來的本地時間(台北時間)轉換為 UTC 後再儲存

```csharp
// OnGetAsync - 顯示時轉換
IssueReport.RecordDate = DateTime.SpecifyKind(
    TimeZoneHelper.ConvertToTaipeiTime(issueDto.RecordDate), 
    DateTimeKind.Unspecified
);

// OnPostAsync - 提交時轉換
if (IssueReport.RecordDate.Kind == DateTimeKind.Unspecified)
{
    IssueReport.RecordDate = TimeZoneHelper.ConvertToUtc(IssueReport.RecordDate);
}
```

## 修改的檔案清單

### 新增檔案
1. `Infrastructure/Helpers/TimeZoneHelper.cs` - 時區轉換輔助類別
2. `Infrastructure/TagHelpers/TaipeiTimeTagHelper.cs` - 時間顯示 Tag Helper
3. `docs/changelogs/TIMEZONE-FIX-20251024.md` - 本文件

### 修改檔案

#### Services
1. `Services/LineMessagingService.cs`
   - 引入 `TimeZoneHelper`
   - LINE 推播訊息使用台北時間顯示
   - 移除 URL 中的 token 參數

2. `Services/LineConversationService.cs`
   - `RecordDate` 改用 `DateTime.UtcNow` 儲存

#### Pages
3. `Pages/Issues/Create.cshtml.cs`
   - 引入 `TimeZoneHelper`
   - OnGetAsync 預設值使用台北時間顯示
   - OnPostAsync 轉換本地時間為 UTC

4. `Pages/Issues/Edit.cshtml.cs`
   - 引入 `TimeZoneHelper`
   - OnGetAsync 轉換 UTC 為台北時間顯示
   - OnPostAsync 轉換本地時間為 UTC

5. `Pages/Issues/Index.cshtml.cs`
   - 引入 `TimeZoneHelper`
   - Excel 匯出使用台北時間
   - 檔案名稱使用台北時間

6. `Pages/Issues/Details.cshtml`
   - 使用 `<taipei-time>` Tag Helper

7. `Pages/Issues/Index.cshtml`
   - 使用 `<taipei-time>` Tag Helper

8. `Pages/Admin/Users/Index.cshtml`
   - 使用 `<taipei-time>` Tag Helper

9. `Pages/Admin/Departments/Index.cshtml`
   - 使用 `<taipei-time>` Tag Helper

## 測試驗證

### 驗證項目

1. ✅ LINE 推播訊息顯示台北時間
   - 建立新回報單後,指派人員收到的 LINE 訊息應顯示 `📅 紀錄日期:2025/10/24 15:30` (台北時間)

2. ✅ 網頁顯示台北時間
   - 回報單詳情頁顯示正確的台北時間
   - 回報單列表顯示正確的台北時間
   - 管理頁面顯示正確的台北時間

3. ✅ 表單輸入正確
   - 建立回報單時,datetime-local 預設顯示當前台北時間
   - 編輯回報單時,datetime-local 顯示該回報單的台北時間
   - 提交後資料庫儲存為 UTC 時間

4. ✅ Excel 匯出正確
   - 匯出的 Excel 檔案中所有時間欄位為台北時間
   - 檔案名稱使用台北時間

## 注意事項

1. **資料庫時間統一為 UTC**: 所有時間在資料庫中以 UTC 儲存,確保跨時區一致性
2. **顯示時轉換**: 僅在顯示給使用者時才轉換為台北時間
3. **Tag Helper 自動註冊**: `<taipei-time>` Tag Helper 已在 `_ViewImports.cshtml` 中註冊,所有 Razor 頁面可直接使用
4. **datetime-local 處理**: 使用 `DateTimeKind.Unspecified` 確保瀏覽器正確處理本地時間輸入
5. **LINE 訊息**: 移除了 URL 中的 token 參數,直接使用 `/Issues/Details/{id}` 格式

## 時區邏輯總結

```
資料流向:
┌─────────────┐      UTC      ┌──────────────┐      UTC       ┌────────────┐
│ 使用者輸入   │ ─────────────> │  應用程式     │ ───────────────> │  資料庫     │
│ (台北時間)   │               │  (邏輯層)     │                 │ (UTC儲存)  │
└─────────────┘               └──────────────┘                 └────────────┘
      ↑                              ↓
      │                              │
      │    台北時間                   │ UTC
      │    (轉換顯示)                 │
      │                              ↓
      └──────────────────────────────┘
```

## 相關資源

- [TimeZoneInfo Class (Microsoft Docs)](https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo)
- [ASP.NET Core Tag Helpers](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro)
- [HTML datetime-local input](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/datetime-local)
