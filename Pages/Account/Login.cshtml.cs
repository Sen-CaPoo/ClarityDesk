using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Account;

/// <summary>
/// 登入頁面 PageModel
/// </summary>
public class LoginModel : PageModel
{
    /// <summary>
    /// GET: 顯示登入頁面
    /// </summary>
    public IActionResult OnGet(string? returnUrl = null)
    {
        // 如果已登入，重導向至首頁
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return Page();
    }

    /// <summary>
    /// POST: 觸發 LINE OAuth Challenge
    /// </summary>
    public IActionResult OnPost(string? returnUrl = null)
    {
        var redirectUrl = returnUrl ?? Url.Page("/Index");
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, "LINE");
    }
}
