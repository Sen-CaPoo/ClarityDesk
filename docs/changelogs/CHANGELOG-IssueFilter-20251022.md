# 回報單篩選功能調整 - 變更摘要

## 實作日期
2025年10月22日

## 變更概述

### 1. 處理狀態簡化

**變更前：**
- 待處理 (Pending)
- 處理中 (InProgress)  
- 已完成 (Completed)

**變更後：**
- 未處理 (Pending)
- 已處理 (Completed)

### 2. 日期區間元件改進

**變更前：**
- 兩個獨立的日期欄位（開始日期、結束日期）
- 各自獨立的標籤和輸入框

**變更後：**
- 單一日期區間元件，包含起始和結束日期
- 使用 Bootstrap 的 input-group 來視覺上結合兩個日期欄位
- 中間顯示「至」字樣連接

### 3. 重設行為改進

**變更前：**
- 重設時自動帶入「近七天」的日期區間
- 在 OnGetAsync 中有預設日期邏輯

**變更後：**
- 點擊「重設」按鈕完全清空所有篩選條件
- 移除自動帶入日期的邏輯
- 使用者可自由選擇所需的日期區間

## 修改的檔案清單

### 1. 核心模型
- `Models/Enums/IssueStatus.cs` - 移除 InProgress 狀態
- `Models/DTOs/IssueStatisticsDto.cs` - 移除 InProgressIssues 屬性

### 2. 服務層
- `Services/IssueReportService.cs` - 更新統計邏輯，移除 InProgress 計算

### 3. UI 層
- `Pages/Issues/Index.cshtml` - 更新篩選表單和統計卡片顯示
- `Pages/Issues/Index.cshtml.cs` - 移除自動帶入日期的邏輯
- `Pages/Issues/Details.cshtml` - 更新狀態顯示

### 4. 基礎建設
- `Infrastructure/TagHelpers/StatusBadgeTagHelper.cs` - 更新狀態徽章

### 5. 測試
- `Tests/ClarityDesk.UnitTests/Services/IssueReportServiceTests.cs` - 更新單元測試

### 6. 資料庫遷移
- `Migrations/20251022000000_SimplifyIssueStatus.cs` - 資料遷移腳本

## 資料庫影響

### Schema 變更
- 無 schema 變更（狀態 enum 以字串形式儲存）

### 資料遷移
建立了遷移腳本將現有的 "InProgress" 記錄更新為 "Completed"：

```sql
UPDATE IssueReports 
SET Status = 'Completed' 
WHERE Status = 'InProgress'
```

**注意：** 此遷移不可逆，如需回復請從備份還原。

## 使用者體驗改進

1. **簡化的狀態選擇** - 使用者現在只需要在「未處理」和「已處理」之間選擇，減少決策複雜度

2. **更清晰的日期區間** - 將開始和結束日期視覺上組合在一起，更容易理解是一個區間

3. **真正的重設功能** - 重設按鈕現在會清空所有條件，讓使用者可以從頭開始篩選

## 向後相容性

### 現有資料
- 所有「處理中」的回報單會被標記為「已處理」
- 歷史資料的語義保持一致（仍然是已經開始處理的單子）

### API 相容性
- `IssueStatus` enum 仍然存在，只是選項減少
- 所有使用 `IssueStatus` 的 API 仍然有效
- DTO 和服務介面保持相容

## 測試狀態

### 單元測試
- ✅ IssueReportServiceTests 已更新並通過
- ✅ 統計邏輯測試已調整
- ✅ 篩選測試已更新

### 建議的手動測試
1. 建立新回報單，驗證狀態選項
2. 編輯現有回報單，驗證狀態更新
3. 使用日期區間篩選器
4. 測試重設按鈕清空所有條件
5. 驗證統計卡片顯示正確數字

## 部署注意事項

1. **資料庫遷移**
   ```bash
   dotnet ef database update
   ```
   或在生產環境執行 SQL 腳本

2. **快取清除**
   - 建議清除統計資訊的快取
   - 如果使用分散式快取，記得同步清除

3. **使用者通知**
   - 建議通知使用者狀態選項的變更
   - 更新使用者手冊或說明文件

## 後續工作建議

1. 更新 `docs/user-manual.md` 反映新的篩選介面
2. 更新 API 文件（如果有的話）
3. 考慮增加日期區間的快速選擇按鈕（如：今天、本週、本月）
4. 檢查其他頁面是否也使用了狀態篩選，確保一致性
