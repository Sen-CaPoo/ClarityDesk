# 回報單功能驗收調整 (2025-10-22)

## 變更摘要

根據驗收人員的需求，進行以下功能調整：

---

## 一、回報單管理（/Issues/Index）

### 1.1 篩選條件重設行為調整

**變更前**：點擊「重設」會導向 `/Issues/Index`，但可能保留部分查詢參數

**變更後**：
- 改用 JavaScript 函式 `resetFilters()` 清空所有篩選欄位
- 包含：關鍵字、處理狀態、緊急程度、日期區間、指派人員、所屬單位
- 清空後重新提交表單，確保所有條件都是空值

**檔案變更**：
- `Pages/Issues/Index.cshtml` - 重設按鈕改為 `<button>` 並呼叫 `resetFilters()`
- `Pages/Issues/Index.cshtml` Scripts 區塊新增 `resetFilters()` 函式

### 1.2 下載 Excel 功能

**新增功能**：
- 在回報單列表標題旁新增「下載 Excel」按鈕
- 匯出當前查詢結果為 `.xlsx` 檔案
- 使用 EPPlus 7.0.5 套件處理 Excel 生成

**Excel 欄位**：
1. 單號 (IssueReport.Id)
2. 主旨 (Title)
3. 處理狀態 (StatusText)
4. 回報人 (ReporterName)
5. 聯絡人 (CustomerName)
6. 連絡電話 (CustomerPhone)
7. 紀錄日期 (RecordDate, yyyy/MM/dd HH:mm:ss)
8. 緊急程度 (PriorityText)
9. 指派人員 (AssignedUserName)
10. 所屬單位 (DepartmentNames, 逗號分隔)
11. 建立時間 (CreatedAt, yyyy/MM/dd HH:mm:ss)
12. 最後修改時間 (UpdatedAt, yyyy/MM/dd HH:mm:ss)
13. 最後修改人 (LastModifiedByUserName)
14. 備註 (Content 欄位)

**技術實作**：
- 新增 `OnGetExportExcelAsync()` 處理程序
- 設定 `ExcelPackage.LicenseContext = LicenseContext.NonCommercial`
- 取得所有符合篩選條件的資料（`pageSize: int.MaxValue`）
- 自動調整欄寬
- 檔名格式：`回報單列表_yyyyMMddHHmmss.xlsx`

**檔案變更**：
- `ClarityDesk.csproj` - 新增 EPPlus 套件參考
- `Pages/Issues/Index.cshtml.cs` - 新增 `using OfficeOpenXml` 和 `OnGetExportExcelAsync` 方法
- `Pages/Issues/Index.cshtml` - 新增下載按鈕

---

## 二、編輯回報單（/Issues/Edit）

### 2.1 最後修改人記錄

**變更內容**：
- 在 `IssueReport` 實體新增 `LastModifiedByUserId` 欄位（nullable int）
- 新增 `LastModifiedBy` 導覽屬性
- 僅在實際內容變更時更新最後修改人

**資料庫變更**：
- 新增欄位：`IssueReports.LastModifiedByUserId (int, nullable)`
- 新增外鍵：`FK_IssueReports_Users_LastModifiedByUserId`
- 新增索引：`IX_IssueReports_LastModifiedByUserId`
- Delete Behavior: Restrict

**服務層變更**：
- `IIssueReportService.UpdateIssueReportAsync` 新增 `int currentUserId` 參數
- 在 `IssueReportService.UpdateIssueReportAsync` 中，只有在 `hasEntityChanges || hasDepartmentChanges` 時才設定 `LastModifiedByUserId`
- 查詢時使用 `.Include(i => i.LastModifiedBy)` 載入最後修改人資訊

**PageModel 變更**：
- `Edit.cshtml.cs` 在 `OnPostAsync` 中從 Claims 取得當前使用者 ID
- 呼叫 Service 時傳遞 `currentUserId` 參數
- 若無法取得使用者 ID，顯示錯誤訊息並返回頁面

**DTO 變更**：
- `IssueReportDto` 新增 `LastModifiedByUserId` 和 `LastModifiedByUserName` 屬性
- `IssueReportExtensions.ToDto()` 映射最後修改人資訊

**檔案變更**：
- `Models/Entities/IssueReport.cs`
- `Models/DTOs/IssueReportDto.cs`
- `Models/Extensions/IssueReportExtensions.cs`
- `Services/Interfaces/IIssueReportService.cs`
- `Services/IssueReportService.cs`
- `Pages/Issues/Edit.cshtml.cs`
- `Data/Configurations/IssueReportConfiguration.cs`
- `Migrations/20251022_AddLastModifiedByUser.cs` (新增)

---

## 三、回報單詳情（/Issues/Details）

### 3.1 最後修改紀錄顯示

**變更內容**：
- 在時間資訊區塊新增第三欄顯示最後修改人
- 格式：`最後修改人：{DisplayName}`
- 若尚未修改過（`LastModifiedByUserName == null`），顯示「尚未修改」

**版面配置**：
```
[建立時間] [最後修改時間] [最後修改人]
  col-md-4      col-md-4       col-md-4
```

**檔案變更**：
- `Pages/Issues/Details.cshtml` - 時間資訊區塊調整為三欄布局

---

## 技術細節

### Migration 檔案

檔名：`Migrations/20251022_AddLastModifiedByUser.cs`

```sql
-- Up
ALTER TABLE IssueReports ADD LastModifiedByUserId INT NULL;
CREATE INDEX IX_IssueReports_LastModifiedByUserId ON IssueReports(LastModifiedByUserId);
ALTER TABLE IssueReports ADD CONSTRAINT FK_IssueReports_Users_LastModifiedByUserId 
    FOREIGN KEY (LastModifiedByUserId) REFERENCES Users(Id) ON DELETE RESTRICT;

-- Down
ALTER TABLE IssueReports DROP CONSTRAINT FK_IssueReports_Users_LastModifiedByUserId;
DROP INDEX IX_IssueReports_LastModifiedByUserId ON IssueReports;
ALTER TABLE IssueReports DROP COLUMN LastModifiedByUserId;
```

### EPPlus 授權設定

```csharp
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

此設定表示以非商業用途使用 EPPlus 套件。如果專案為商業用途，需購買授權。

### 變更偵測邏輯

編輯回報單時的變更偵測流程：
1. 呼叫 `issue.UpdateFromDto(dto)` 返回 `hasEntityChanges`
2. 比較單位指派變更返回 `hasDepartmentChanges`
3. 只有在 `hasEntityChanges || hasDepartmentChanges == true` 時：
   - 更新 `UpdatedAt`
   - 設定 `LastModifiedByUserId = currentUserId`
4. 儲存變更

### 使用者識別取得

從 ASP.NET Core Claims 取得當前使用者 ID：

```csharp
var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
{
    // 處理錯誤
}
```

---

## 驗收檢查清單

### 一、回報單管理
- [ ] 點擊「重設」後所有篩選欄位為空值
- [ ] 日期區間不會自動帶入任何預設值
- [ ] 點擊「下載 Excel」可成功下載 .xlsx 檔案
- [ ] Excel 包含所有必要欄位（14 欄）
- [ ] Excel 標題列有灰色背景和粗體字
- [ ] Excel 自動調整欄寬
- [ ] 無資料時也可下載（僅含標題列）
- [ ] Excel 檔名包含時間戳記

### 二、編輯回報單
- [ ] 無異動儲存時，`UpdatedAt` 和 `LastModifiedByUserId` 不更新
- [ ] 有異動儲存時，兩者都正確更新
- [ ] 單位指派變更也會觸發修改紀錄
- [ ] 無法取得使用者 ID 時顯示錯誤訊息

### 三、回報單詳情
- [ ] 顯示最後修改人姓名
- [ ] 顯示最後修改時間（yyyy/MM/dd HH:mm:ss 格式）
- [ ] 尚未修改過的回報單顯示「尚未修改」
- [ ] 時間資訊三欄平均分布

### 四、資料庫
- [ ] Migration 成功執行
- [ ] `LastModifiedByUserId` 欄位為 nullable
- [ ] 外鍵約束正確設定
- [ ] 索引已建立

---

## 相依套件

### 新增套件
- **EPPlus** 7.0.5 - MIT License (NonCommercial use)
  - 用途：Excel 檔案生成與操作
  - NuGet: https://www.nuget.org/packages/EPPlus

---

## 部署注意事項

1. **資料庫 Migration**
   ```bash
   dotnet ef database update
   ```

2. **EPPlus 授權**
   - 確認專案使用性質是否符合 NonCommercial 授權
   - 商業用途需購買授權

3. **使用者 Claims**
   - 確認 LINE Login 驗證流程有設定 `UserId` Claim
   - 檢查 `AuthenticationService.LoginOrRegisterWithLineAsync` 是否正確添加 Claim

4. **效能考量**
   - Excel 匯出功能會載入所有符合條件的資料
   - 建議在大量資料情況下考慮分批處理或背景作業

---

## 檔案變更清單

### 新增檔案
- `Migrations/20251022_AddLastModifiedByUser.cs`

### 修改檔案
1. `ClarityDesk.csproj`
2. `Models/Entities/IssueReport.cs`
3. `Models/DTOs/IssueReportDto.cs`
4. `Models/Extensions/IssueReportExtensions.cs`
5. `Services/Interfaces/IIssueReportService.cs`
6. `Services/IssueReportService.cs`
7. `Pages/Issues/Index.cshtml`
8. `Pages/Issues/Index.cshtml.cs`
9. `Pages/Issues/Edit.cshtml.cs`
10. `Pages/Issues/Details.cshtml`
11. `Data/Configurations/IssueReportConfiguration.cs`

---

**變更日期**：2025-10-22  
**變更原因**：功能驗收調整  
**預期完成時間**：2025-10-22 EOD
