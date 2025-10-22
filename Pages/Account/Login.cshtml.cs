using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Account;

/// <summary>
/// 登入頁面 PageModel
/// </summary>
public class LoginModel : PageModel
{
    private readonly Services.Interfaces.IAuthenticationService _authenticationService;

    public LoginModel(Services.Interfaces.IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }
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

    /// <summary>
    /// POST: 以遊客身份登入
    /// </summary>
    public async Task<IActionResult> OnPostGuestAsync(string? returnUrl = null)
    {
        try
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

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // 登入
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(365)
                });

            var redirectUrl = returnUrl ?? Url.Page("/Index") ?? "/";
            return LocalRedirect(redirectUrl);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "遊客登入失敗,請稍後再試";
            return Page();
        }
    }
}
