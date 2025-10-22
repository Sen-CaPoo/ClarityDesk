# 回報單功能調整變更紀錄 (2025-10-22)

## 變更概要

本次更新針對回報單的建立、編輯和詳情頁面進行多項功能調整，包括欄位限制、即時字數提示、時間格式優化，以及使用者體驗改善。

---

## 一、建立回報單 (/Issues/Create) 功能調整

### 1.1 標題欄位調整
- **字數限制**：從 200 字元調整為 **1-30 字元**
- **即時字數提示**：顯示格式 `0/30`，超過 25 字元時變為橘色警告
- **檔案變更**：
  - `Models/DTOs/CreateIssueReportDto.cs`
  - `Pages/Issues/Create.cshtml`

### 1.2 內容欄位調整
- **字數限制**：從最少 10 字元調整為 **1-150 字元**
- **即時字數提示**：顯示格式 `0/150`，超過 130 字元時變為橘色警告
- **檔案變更**：
  - `Models/DTOs/CreateIssueReportDto.cs`
  - `Pages/Issues/Create.cshtml`

### 1.3 紀錄日期欄位調整
- **輸入類型**：從 `type="date"` 改為 `type="datetime-local"`
- **預設值**：自動帶入當前系統時間（包含時分秒），格式 `yyyy-MM-ddTHH:mm`
- **顯示格式**：支援 24 小時制
- **檔案變更**：
  - `Pages/Issues/Create.cshtml`
  - `Pages/Issues/Create.cshtml.cs`

### 1.4 欄位標籤重新命名
| 舊標籤 | 新標籤 |
|--------|--------|
| 回報人姓名 | **回報人** |
| 顧客聯絡人姓名 | **聯絡人** |
| 顧客連絡電話 | **連絡電話** |

- **檔案變更**：`Pages/Issues/Create.cshtml`

---

## 二、編輯回報單 (/Issues/Edit) 功能調整

### 2.1 變更偵測機制
**實作目標**：區分「無異動儲存」與「有異動儲存」

#### 無異動儲存
- 允許提交表單，但 **不更新 `UpdatedAt` 時間戳記**
- 適用於使用者誤觸儲存按鈕的情境

#### 有異動儲存
- 僅在以下任一欄位變更時更新時間戳記：
  - Title（標題）
  - Content（內容）
  - RecordDate（紀錄日期）
  - Status（處理狀態）
  - Priority（緊急程度）
  - ReporterName（回報人）
  - CustomerName（聯絡人）
  - CustomerPhone（連絡電話）
  - AssignedUserId（指派處理人員）
  - DepartmentIds（問題所屬單位）

**實作細節**：
- 修改 `IssueReportExtensions.UpdateFromDto()` 方法，返回 `bool` 以指示是否有變更
- 在 `IssueReportService.UpdateIssueReportAsync()` 中進行變更偵測
- 單位指派變更也會觸發時間戳記更新

**檔案變更**：
- `Models/Extensions/IssueReportExtensions.cs`
- `Services/IssueReportService.cs`

### 2.2 欄位調整（與建立頁面一致）
- 標題字數限制：1-30 字元 + 即時字數提示
- 內容字數限制：1-150 字元 + 即時字數提示
- 紀錄日期改為 `datetime-local` 輸入
- 欄位標籤重新命名

**檔案變更**：
- `Models/DTOs/UpdateIssueReportDto.cs`
- `Pages/Issues/Edit.cshtml`

---

## 三、回報單詳情 (/Issues/Details) 功能調整

### 3.1 連絡電話點擊行為
**變更前**：使用 `<a href="tel:...">`，在某些裝置會觸發撥號或應用程式選擇器

**變更後**：點擊複製電話號碼到剪貼簿
- **桌面裝置**：顯示「已複製電話」提示訊息（3 秒後自動消失）
- **行動裝置**：同樣顯示「已複製電話」提示，不會彈出「要開啟『挑選應用程式』嗎？」
- **實作方式**：
  - 優先使用 `navigator.clipboard.writeText()`（現代瀏覽器）
  - 備用方案：使用 `document.execCommand('copy')`（舊版瀏覽器）
  - 錯誤處理：複製失敗時顯示「複製失敗，請手動複製」

**檔案變更**：`Pages/Issues/Details.cshtml`

### 3.2 最後修改紀錄顯示
- **時間格式**：統一使用 `yyyy/MM/dd HH:mm:ss`（24 小時制）
- **顯示位置**：詳情頁面底部
- **顯示內容**：
  - 建立時間：轉換為本地時間顯示
  - 最後修改時間：轉換為本地時間顯示

**檔案變更**：`Pages/Issues/Details.cshtml`

### 3.3 欄位標籤一致性
- 紀錄日期顯示格式：`yyyy/MM/dd HH:mm:ss`（含時分秒）
- 標籤名稱：
  - 「顧客姓名」→ **「聯絡人」**
  - 「連絡電話」保持一致

**檔案變更**：`Pages/Issues/Details.cshtml`

---

## 技術實作細節

### 變更偵測演算法 (UpdateFromDto)

```csharp
public static bool UpdateFromDto(this IssueReport entity, UpdateIssueReportDto dto)
{
    bool hasChanges = false;

    // 逐一比較每個欄位，只有值不同時才更新
    if (entity.Title != dto.Title)
    {
        entity.Title = dto.Title;
        hasChanges = true;
    }
    // ... 其他欄位

    // 只有在有實際變更時才更新 UpdatedAt
    if (hasChanges)
    {
        entity.UpdatedAt = DateTime.UtcNow;
    }

    return hasChanges;
}
```

### 單位指派變更偵測

```csharp
var currentDepartmentIds = issue.DepartmentAssignments.Select(da => da.DepartmentId).OrderBy(id => id).ToList();
var newDepartmentIds = dto.DepartmentIds.OrderBy(id => id).ToList();

// 使用 SequenceEqual 比較兩個已排序的清單
if (!currentDepartmentIds.SequenceEqual(newDepartmentIds))
{
    hasDepartmentChanges = true;
    // 更新單位指派...
}
```

### JavaScript 字數計數器

```javascript
titleInput.addEventListener('input', function() {
    const length = this.value.length;
    titleCounter.textContent = `${length}/30`;
    if (length > 25) {
        titleCounter.classList.add('text-warning');
    } else {
        titleCounter.classList.remove('text-warning');
    }
});
```

### 電話號碼複製功能

```javascript
function copyPhoneNumber() {
    const phoneNumber = '@Model.IssueReport.CustomerPhone';
    
    // 使用 Clipboard API
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(phoneNumber).then(() => {
            showCopyMessage('已複製電話');
        });
    } else {
        // 備用方法
        fallbackCopyTextToClipboard(phoneNumber);
    }
}
```

---

## 測試建議

### 建立回報單測試案例
1. ✅ 標題輸入 1 字元（最小值）
2. ✅ 標題輸入 30 字元（最大值）
3. ✅ 標題輸入 31 字元（應被限制為 30）
4. ✅ 內容輸入 1 字元（最小值）
5. ✅ 內容輸入 150 字元（最大值）
6. ✅ 內容輸入 151 字元（應被限制為 150）
7. ✅ 字數提示在輸入時即時更新
8. ✅ 紀錄日期自動填入當前時間（包含時分秒）

### 編輯回報單測試案例
1. ✅ 不修改任何欄位直接儲存 → `UpdatedAt` 不變
2. ✅ 修改標題後儲存 → `UpdatedAt` 更新
3. ✅ 修改單位指派後儲存 → `UpdatedAt` 更新
4. ✅ 修改多個欄位後儲存 → `UpdatedAt` 只更新一次
5. ✅ 將欄位改為相同的值 → `UpdatedAt` 不變

### 詳情頁面測試案例
1. ✅ 點擊電話號碼 → 複製到剪貼簿，顯示「已複製電話」
2. ✅ 在行動裝置上點擊電話 → 不觸發撥號介面
3. ✅ 在不支援 Clipboard API 的瀏覽器測試 → 備用方法正常運作
4. ✅ 時間顯示格式為 `yyyy/MM/dd HH:mm:ss`（24 小時制）
5. ✅ 最後修改時間正確反映編輯時的變更

---

## 相容性考量

### 瀏覽器支援
- **Clipboard API**：Chrome 66+, Firefox 63+, Safari 13.1+, Edge 79+
- **備用方法（execCommand）**：支援所有主流瀏覽器
- **datetime-local 輸入**：Chrome 20+, Firefox 57+, Safari 14.1+, Edge 12+

### 行動裝置
- **iOS Safari**：完整支援 Clipboard API 和 datetime-local
- **Android Chrome**：完整支援所有功能
- **舊版瀏覽器**：使用備用複製方法，功能完整

---

## 未來擴展建議

### 歷史變更紀錄（可選功能）
若需實作「歷史變更紀錄」功能，建議架構：

1. **新增 ChangeLog 實體**
```csharp
public class IssueReportChangeLog
{
    public int Id { get; set; }
    public int IssueReportId { get; set; }
    public string FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int ModifiedByUserId { get; set; }
    public DateTime ModifiedAt { get; set; }
}
```

2. **在 UpdateFromDto 中記錄變更**
```csharp
if (entity.Title != dto.Title)
{
    changeLogs.Add(new IssueReportChangeLog
    {
        FieldName = "Title",
        OldValue = entity.Title,
        NewValue = dto.Title,
        ModifiedByUserId = currentUserId,
        ModifiedAt = DateTime.UtcNow
    });
    entity.Title = dto.Title;
}
```

3. **詳情頁面顯示變更歷史**
- 使用折疊式面板（Accordion）顯示變更紀錄
- 按時間倒序排列
- 顯示欄位名稱、舊值 → 新值、操作者、時間

### 最後修改人追蹤
若需顯示「最後修改人」，需新增：
1. IssueReport 實體新增 `LastModifiedByUserId` 欄位
2. 建立 Migration
3. 在編輯時記錄當前使用者 ID
4. 詳情頁面顯示最後修改人姓名

---

## 檔案變更清單

### 模型層 (Models)
- ✅ `Models/DTOs/CreateIssueReportDto.cs` - 調整 Title 和 Content 驗證規則
- ✅ `Models/DTOs/UpdateIssueReportDto.cs` - 調整 Title 和 Content 驗證規則
- ✅ `Models/Extensions/IssueReportExtensions.cs` - UpdateFromDto 返回 bool

### 服務層 (Services)
- ✅ `Services/IssueReportService.cs` - 實作變更偵測邏輯

### 頁面層 (Pages)
- ✅ `Pages/Issues/Create.cshtml` - UI 更新、字數提示、datetime-local
- ✅ `Pages/Issues/Create.cshtml.cs` - 預設時間改為 DateTime.Now
- ✅ `Pages/Issues/Edit.cshtml` - UI 更新、字數提示、datetime-local
- ✅ `Pages/Issues/Details.cshtml` - 電話複製功能、時間格式調整

---

## 驗證檢查清單

- [ ] 執行 `dotnet build` 確認編譯無誤
- [ ] 執行單元測試 `dotnet test`
- [ ] 建立新回報單並驗證：
  - [ ] 標題限制 1-30 字元
  - [ ] 內容限制 1-150 字元
  - [ ] 即時字數提示正常運作
  - [ ] 紀錄日期自動填入當前時間
- [ ] 編輯回報單並驗證：
  - [ ] 無變更時 UpdatedAt 不更新
  - [ ] 有變更時 UpdatedAt 正確更新
- [ ] 詳情頁面驗證：
  - [ ] 電話複製功能正常
  - [ ] 時間顯示格式正確（24 小時制）
- [ ] 跨瀏覽器測試：
  - [ ] Chrome
  - [ ] Firefox
  - [ ] Safari
  - [ ] Edge
- [ ] 行動裝置測試：
  - [ ] iOS Safari
  - [ ] Android Chrome

---

**變更作者**：GitHub Copilot  
**變更日期**：2025-10-22  
**專案版本**：Phase 1-5 功能調整
