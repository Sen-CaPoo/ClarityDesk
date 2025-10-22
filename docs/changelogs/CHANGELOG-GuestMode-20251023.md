# 變更日誌 - 遊客模式功能

**日期**: 2025-10-23  
**版本**: Phase 5 (Guest Mode Feature)  
**變更類型**: 功能新增 (Feature)

## 概要

新增「遊客模式」功能,允許使用者無需 LINE 登入即可以一般使用者權限快速體驗系統。

## 變更詳情

### 新增功能

#### 1. 服務層擴充

**檔案**: `Services/Interfaces/IAuthenticationService.cs`
- 新增 `LoginAsGuestAsync()` 方法定義
- 文檔說明: 以遊客身份登入,使用通用遊客帳號

**檔案**: `Services/AuthenticationService.cs`
- 實作 `LoginAsGuestAsync()` 方法
- 邏輯:
  - 查詢 `LineUserId = "guest"` 的使用者
  - 不存在時自動建立遊客帳號
  - 已存在時直接返回
  - 確保系統中只有一個遊客通用帳號

遊客帳號屬性:
```csharp
LineUserId: "guest"
DisplayName: "訪客"
Role: UserRole.User
IsActive: true
PictureUrl: null
```

#### 2. 登入頁面更新

**檔案**: `Pages/Account/Login.cshtml`

UI 變更:
- 新增「以訪客身份繼續」按鈕 (灰色外框樣式)
- 加入分隔線樣式 (顯示「或」字樣)
- 更新說明文字,提示兩種登入方式

CSS 樣式新增:
```css
.divider {
    display: flex;
    align-items: center;
    text-align: center;
}
.divider::before, .divider::after {
    content: '';
    flex: 1;
    border-bottom: 1px solid #dee2e6;
}
.divider-text {
    padding: 0 10px;
    color: #6c757d;
    font-size: 0.875rem;
}
```

**檔案**: `Pages/Account/Login.cshtml.cs`

變更:
- 注入 `Services.Interfaces.IAuthenticationService` 依賴
- 新增 `OnPostGuestAsync()` Handler 方法
- 處理流程:
  1. 呼叫 `LoginAsGuestAsync()` 取得遊客帳號
  2. 建立包含使用者資訊的 Claims
  3. 使用 Cookie Authentication 建立會話
  4. 會話期限設為 365 天
  5. 重導向至首頁或原始請求頁面

Claims 包含:
- `ClaimTypes.NameIdentifier`: "guest"
- `ClaimTypes.Name`: "訪客"
- `"UserId"`: 遊客帳號 ID
- `ClaimTypes.Role`: "User"

#### 3. 測試案例新增

**檔案**: `Tests/ClarityDesk.UnitTests/Services/AuthenticationServiceTests.cs`

新增測試:
1. `LoginAsGuestAsync_FirstTime_CreatesGuestUser`
   - 驗證首次呼叫建立遊客帳號
   - 確認屬性正確設定
   - 驗證已儲存至資料庫

2. `LoginAsGuestAsync_ExistingGuest_ReturnsExistingUser`
   - 驗證重複呼叫返回現有帳號
   - 確認資料庫中只有一個遊客帳號

#### 4. 文檔新增

**檔案**: `docs/guest-mode-implementation.md`
- 完整的遊客模式實作說明
- 功能特色、實作細節、使用流程
- 安全性考量、測試案例
- 未來改進方向

## 影響範圍

### 資料庫
- 自動建立一筆 `LineUserId = "guest"` 的使用者記錄
- 不需要 Migration,使用既有的 User 資料表

### 身份驗證
- 新增遊客身份驗證路徑
- 與現有 LINE OAuth 並行,互不影響

### 使用者體驗
- 降低使用門檻
- 提供快速體驗入口
- 保留 LINE 登入作為正式註冊方式

### 權限控制
- 遊客擁有 `User` 角色
- 可存取所有一般使用者功能
- 無法存取 Admin 管理功能

## 相容性

- ✅ 向後相容: 不影響現有 LINE 登入功能
- ✅ 現有使用者: 不受影響
- ✅ 資料庫: 使用現有結構,無需 Migration

## 安全性考量

### 已實施的保護措施
- 遊客帳號權限限制為 `User` 角色
- 通過 Cookie Authentication 驗證身份
- 會話管理與 LINE 使用者相同

### 已知限制
- 所有遊客共用同一帳號,無法區分個別使用者
- 遊客建立的資料會標記為同一個使用者 ID
- 建議在生產環境考慮增加操作頻率限制

### 建議措施
- 定期檢視遊客帳號建立的資料
- 在重要操作前提示遊客註冊正式帳號
- 考慮增加遊客轉為正式使用者的功能

## 測試狀態

- ✅ 單元測試: 已新增並通過
- ✅ 建置: 成功編譯
- ⏳ 整合測試: 待執行
- ⏳ 使用者驗收測試: 待執行

## 後續工作

1. 執行整合測試驗證完整登入流程
2. 在生產環境測試遊客登入功能
3. 監控遊客使用行為和轉換率
4. 根據使用情況決定是否實作個人化遊客模式

## 相關檔案清單

### 修改的檔案
- `Services/Interfaces/IAuthenticationService.cs`
- `Services/AuthenticationService.cs`
- `Pages/Account/Login.cshtml`
- `Pages/Account/Login.cshtml.cs`
- `Tests/ClarityDesk.UnitTests/Services/AuthenticationServiceTests.cs`

### 新增的檔案
- `docs/guest-mode-implementation.md`
- `CHANGELOG-GuestMode-20251023.md`

## 開發者備註

### 命名空間衝突處理
在 `Login.cshtml.cs` 中遇到命名空間衝突:
- `Microsoft.AspNetCore.Authentication.IAuthenticationService`
- `ClarityDesk.Services.Interfaces.IAuthenticationService`

解決方案: 使用完整命名空間 `Services.Interfaces.IAuthenticationService`

### Nullable 警告處理
處理 `LocalRedirect()` 方法的 nullable 警告:
```csharp
var redirectUrl = returnUrl ?? Url.Page("/Index") ?? "/";
return LocalRedirect(redirectUrl);
```

## 版本資訊

- **專案版本**: Phase 5
- **ASP.NET Core**: 8.0
- **EF Core**: 8.0
- **.NET Runtime**: 8.0

---

**變更提交者**: GitHub Copilot  
**審核狀態**: 待審核  
**部署狀態**: 待部署
