# 使用者管理功能修復與增強 - 2025-10-28

## 修復問題

### 1. 停用使用者後狀態欄位未即時更新 (Bug #1)

**問題描述**:
- 在 `Admin/Users` 使用者權限管理頁面中,停用某個使用者後,"狀態"欄位依然顯示為"啟用"
- "操作"欄位也沒有出現對應的"啟用"按鈕
- 原因是 `UserManagementService` 使用了 10 分鐘的記憶體快取,導致資料不即時

**解決方案**:
- 移除 `GetAllUsersAsync` 方法中的快取機制,改為直接從資料庫讀取
- 加入 `.AsNoTracking()` 以提升唯讀查詢效能
- 確保每次查詢都能取得最新的使用者狀態

**修改檔案**:
- `Services/UserManagementService.cs`
  - 移除快取查詢邏輯
  - 使用 `AsNoTracking()` 進行唯讀查詢

**測試方式**:
1. 登入管理後台
2. 進入 `Admin/Users` 頁面
3. 點擊"停用"按鈕停用某個使用者
4. 確認頁面重新載入後,"狀態"欄位顯示為"停用"
5. 確認"操作"欄位顯示"啟用"按鈕

### 1.1. "顯示停用使用者"選項無法正常運作 (Bug #1.1)

**問題描述**:
- 勾選"顯示停用使用者"選項後,停用的使用者依然沒有顯示
- checkbox 的 `onchange` 事件無法觸發表單提交
- 原因是 checkbox 沒有包裹在 form 標籤內

**解決方案**:
- 將 checkbox 包裹在 `<form method="get">` 標籤內
- 加入 `name="IncludeInactive"` 屬性讓參數可以正確傳遞
- 加入 `value="true"` 確保勾選時傳送正確的值

**修改檔案**:
- `Pages/Admin/Users/Index.cshtml`
  - 在 checkbox 外層加入 form 標籤
  - 設定正確的 name 和 value 屬性

**測試方式**:
1. 進入 `Admin/Users` 頁面
2. 勾選"顯示停用使用者"選項
3. 確認頁面重新載入並顯示所有使用者(包含停用的)
4. 取消勾選選項
5. 確認頁面只顯示啟用的使用者

### 2. 無法編輯使用者 Email 或其他資料 (Bug #2)

**問題描述**:
- `Admin/Users` 頁面有 Email 欄位,但沒有提供編輯功能
- 無法維護使用者的 Email 或其他基本資料

**解決方案**:
- 建立新的 DTO: `UpdateUserDto` 用於更新使用者資料
- 在 `IUserManagementService` 介面新增 `UpdateUserAsync` 方法
- 實作 `UpdateUserAsync` 方法於 `UserManagementService`
- 建立編輯頁面: `Pages/Admin/Users/Edit.cshtml` 和 `Edit.cshtml.cs`
- 在使用者列表頁面的操作欄加入"編輯"按鈕

**新增檔案**:
- `Models/DTOs/UpdateUserDto.cs` - 更新使用者資料的 DTO
- `Pages/Admin/Users/Edit.cshtml` - 編輯使用者頁面視圖
- `Pages/Admin/Users/Edit.cshtml.cs` - 編輯使用者頁面 PageModel

**修改檔案**:
- `Services/Interfaces/IUserManagementService.cs` - 新增 `UpdateUserAsync` 方法介面
- `Services/UserManagementService.cs` - 實作 `UpdateUserAsync` 方法
- `Pages/Admin/Users/Index.cshtml` - 在操作欄加入"編輯"按鈕

**測試方式**:
1. 登入管理後台
2. 進入 `Admin/Users` 頁面
3. 點擊任一使用者的"編輯"按鈕
4. 進入編輯頁面,確認可以看到使用者的基本資訊(唯讀)和可編輯欄位
5. 修改顯示名稱或 Email
6. 點擊"儲存變更"按鈕
7. 確認回到列表頁面並顯示成功訊息
8. 確認使用者資料已更新

## 功能特性

### 編輯使用者頁面功能

**可編輯欄位**:
- 顯示名稱 (必填,最多 100 字元)
- 電子信箱 (選填,需符合 Email 格式,最多 200 字元)

**唯讀資訊**:
- LINE User ID
- 頭像
- 角色 (管理人員/普通使用者)
- 狀態 (啟用/停用)

**驗證規則**:
- 顯示名稱為必填欄位
- Email 需符合有效的電子信箱格式
- 使用客戶端與伺服器端雙重驗證

**使用者體驗**:
- 使用 Bootstrap 5 響應式設計
- 麵包屑導覽列方便返回
- 清楚的說明文字
- 操作按鈕有圖示輔助識別
- 成功/失敗訊息提示

## 技術細節

### 資料驗證

**UpdateUserDto 驗證規則**:
```csharp
[Required(ErrorMessage = "顯示名稱為必填欄位")]
[StringLength(100, ErrorMessage = "顯示名稱長度不可超過 100 個字元")]
public string DisplayName { get; set; }

[EmailAddress(ErrorMessage = "請輸入有效的電子信箱格式")]
[StringLength(200, ErrorMessage = "電子信箱長度不可超過 200 個字元")]
public string? Email { get; set; }
```

### 服務層實作

**UpdateUserAsync 方法**:
```csharp
public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto updateDto)
{
    var user = await _context.Users.FindAsync(userId);
    if (user == null)
    {
        return false;
    }

    user.DisplayName = updateDto.DisplayName;
    user.Email = updateDto.Email;
    user.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    return true;
}
```

### 效能最佳化

**查詢最佳化**:
- 移除快取機制以確保資料即時性
- 使用 `AsNoTracking()` 提升唯讀查詢效能
- 避免不必要的實體追蹤

**權限控制**:
- 所有管理頁面都需要 Admin 角色
- 使用 `[Authorize(Roles = "Admin")]` 進行保護

## 未來改進建議

1. **批次操作**: 支援批次啟用/停用使用者
2. **進階搜尋**: 新增搜尋功能(按名稱、Email、角色篩選)
3. **分頁功能**: 當使用者數量很多時,加入分頁避免效能問題
4. **稽核記錄**: 記錄所有使用者資料的變更歷史
5. **Email 驗證**: 新增 Email 驗證機制確保信箱有效性
6. **匯出功能**: 支援匯出使用者清單為 CSV 或 Excel

## 影響範圍

### 資料庫變更
- 無需資料庫 Migration

### API 變更
- 新增 `IUserManagementService.UpdateUserAsync` 方法
- 修改 `UserManagementService.GetAllUsersAsync` 實作(移除快取)

### UI 變更
- `Admin/Users/Index.cshtml` - 新增編輯按鈕,調整按鈕排列
- 新增 `Admin/Users/Edit.cshtml` - 編輯使用者頁面

### 相依性
- 無新增外部套件相依性

## 測試項目

### 功能測試
- [x] 停用使用者後狀態即時更新
- [x] 停用使用者後顯示啟用按鈕
- [x] 啟用使用者後顯示停用按鈕
- [x] 編輯按鈕導向編輯頁面
- [x] 編輯頁面正確顯示使用者資料
- [x] 修改顯示名稱成功儲存
- [x] 修改 Email 成功儲存
- [x] Email 格式驗證正常運作
- [x] 必填欄位驗證正常運作

### 權限測試
- [x] 只有 Admin 角色可以存取管理頁面
- [x] 未登入使用者會被導向登入頁面

### 效能測試
- [x] 查詢使用者列表效能正常
- [x] 更新使用者資料效能正常

## 版本資訊

- **版本**: 1.1.0
- **日期**: 2025-10-28
- **類型**: Bug 修復 + 功能增強
- **相關 Issue**: #1, #2
