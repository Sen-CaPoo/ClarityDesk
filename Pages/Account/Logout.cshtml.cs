using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Account;

/// <summary>
/// 登出頁面 PageModel
/// </summary>
public class LogoutModel : PageModel
{
    /// <summary>
    /// POST: 處理登出邏輯
    /// </summary>
    public async Task<IActionResult> OnPost()
    {
        // 清除 Cookie 認證 (包含 LINE OAuth 的認證資訊)
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // 清除 Session
        HttpContext.Session.Clear();
        
        return RedirectToPage("/Account/Login");
    }

    /// <summary>
    /// GET: 重導向至登入頁面
    /// </summary>
    public IActionResult OnGet()
    {
        return RedirectToPage("/Account/Login");
    }
}
