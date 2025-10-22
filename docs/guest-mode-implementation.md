# 遊客模式實作說明

## 概述

ClarityDesk 系統新增了「遊客模式」功能,允許使用者無需 LINE 登入即可以一般使用者權限體驗系統功能。

## 功能特色

- **無需註冊**: 使用者點擊「以訪客身份繼續」即可立即登入
- **一般使用者權限**: 遊客帳號擁有 `User` 角色,可執行所有一般使用者操作
- **單一通用帳號**: 所有遊客使用者共用同一個帳號 (LineUserId: "guest")
- **持久化會話**: 遊客登入後會話期限為 365 天

## 實作細節

### 1. 資料庫層

遊客帳號在首次使用時自動建立,具有以下特徵:

```csharp
LineUserId: "guest"         // 唯一識別碼
DisplayName: "訪客"         // 顯示名稱
Role: UserRole.User        // 一般使用者權限
IsActive: true             // 帳號啟用狀態
PictureUrl: null           // 無頭像
```

### 2. 服務層實作

#### IAuthenticationService 介面新增

```csharp
/// <summary>
/// 以遊客身份登入(使用通用遊客帳號)
/// </summary>
/// <returns>遊客使用者 DTO</returns>
Task<UserDto> LoginAsGuestAsync();
```

#### AuthenticationService 實作

- 查詢資料庫中 `LineUserId = "guest"` 的使用者
- 若不存在則自動建立遊客帳號
- 若已存在則直接返回現有帳號
- 確保系統中只有一個遊客帳號

### 3. 登入頁面更新

#### UI 變更

在 `Pages/Account/Login.cshtml`:

1. **新增遊客登入按鈕**
   ```html
   <form method="post" asp-page-handler="Guest">
       <button type="submit" class="btn btn-outline-secondary btn-lg w-100">
           <i class="bi bi-person me-2"></i>
           以訪客身份繼續
       </button>
   </form>
   ```

2. **分隔線樣式**
   - 在 LINE 登入按鈕和遊客按鈕之間加入「或」分隔線
   - 使用 flexbox 實現對稱的分隔線效果

#### PageModel 處理器

在 `Pages/Account/Login.cshtml.cs` 新增:

```csharp
public async Task<IActionResult> OnPostGuestAsync(string? returnUrl = null)
{
    // 取得遊客帳號
    var guestUser = await _authenticationService.LoginAsGuestAsync();
    
    // 建立 Claims
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, guestUser.LineUserId),
        new Claim(ClaimTypes.Name, guestUser.DisplayName),
        new Claim("UserId", guestUser.Id.ToString()),
        new Claim(ClaimTypes.Role, guestUser.Role.ToString())
    };
    
    // 建立身份驗證票證並登入
    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    
    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        claimsPrincipal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365)
        });
    
    return LocalRedirect(returnUrl ?? "/");
}
```

## 使用流程

1. 使用者訪問登入頁面 (`/Account/Login`)
2. 選擇「以訪客身份繼續」按鈕
3. 系統執行 `OnPostGuestAsync` Handler
4. 呼叫 `LoginAsGuestAsync()` 取得/建立遊客帳號
5. 建立包含使用者資訊的 Claims
6. 使用 Cookie Authentication 建立會話
7. 重導向至首頁或原始請求頁面

## 安全性考量

### 優點
- 降低使用門檻,提升使用者體驗
- 仍然需要身份驗證,可追蹤遊客操作
- 遊客權限限制為一般使用者,無法存取管理功能

### 限制
- 所有遊客共用同一帳號,無法區分個別使用者
- 遊客建立的資料會標記為同一個使用者 ID
- 若需要區分遊客,可考慮為每個工作階段建立臨時帳號

### 建議
- 在重要操作前提示遊客註冊正式帳號
- 定期清理遊客帳號建立的測試資料
- 考慮對遊客帳號增加操作頻率限制

## 測試案例

### 單元測試

在 `Tests/ClarityDesk.UnitTests/Services/AuthenticationServiceTests.cs`:

1. **LoginAsGuestAsync_FirstTime_CreatesGuestUser**
   - 驗證首次呼叫時建立遊客帳號
   - 確認帳號屬性正確 (LineUserId, DisplayName, Role)

2. **LoginAsGuestAsync_ExistingGuest_ReturnsExistingUser**
   - 驗證重複呼叫時返回現有帳號
   - 確認資料庫中只有一個遊客帳號

### 整合測試建議

1. 測試遊客登入完整流程
2. 驗證遊客權限限制 (無法存取 Admin 功能)
3. 測試遊客建立回報單功能
4. 驗證會話持久性

## 未來改進方向

1. **個人化遊客模式**
   - 為每個遊客建立唯一臨時帳號 (使用 GUID)
   - 在一定期限後自動清理

2. **遊客轉正式使用者**
   - 提供遊客升級為 LINE 使用者的功能
   - 保留遊客期間建立的資料

3. **操作限制**
   - 對遊客帳號增加速率限制
   - 限制某些進階功能僅限正式使用者

4. **分析追蹤**
   - 記錄遊客使用行為
   - 分析轉換率 (遊客 → 正式使用者)

## 相關檔案

- `Services/Interfaces/IAuthenticationService.cs`
- `Services/AuthenticationService.cs`
- `Pages/Account/Login.cshtml`
- `Pages/Account/Login.cshtml.cs`
- `Tests/ClarityDesk.UnitTests/Services/AuthenticationServiceTests.cs`

## 變更日期

- **2025-10-23**: 初始實作遊客模式功能
