using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ClarityDesk.Pages.Account;

/// <summary>
/// LINE 帳號綁定管理頁面
/// </summary>
[Authorize]
public class LineBindingModel : PageModel
{
    private readonly ILineBindingService _lineBindingService;
    private readonly ILogger<LineBindingModel> _logger;

    public LineBindingModel(
        ILineBindingService lineBindingService,
        ILogger<LineBindingModel> logger)
    {
        _lineBindingService = lineBindingService;
        _logger = logger;
    }

    /// <summary>
    /// 綁定資訊
    /// </summary>
    public LineBindingDto? Binding { get; set; }

    /// <summary>
    /// 是否為訪客帳號
    /// </summary>
    public bool IsGuestAccount { get; set; }

    /// <summary>
    /// 成功訊息
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// GET: 載入綁定狀態
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("無法取得使用者 ID from Claims");
                return RedirectToPage("/Account/Login");
            }

            // 檢查是否為訪客帳號
            var lineUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            IsGuestAccount = lineUserIdClaim?.Value == "guest";

            // 載入綁定資訊
            Binding = await _lineBindingService.GetBindingByUserIdAsync(userId);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入 LINE 綁定資訊時發生錯誤");
            ErrorMessage = "載入綁定資訊失敗,請稍後再試";
            return Page();
        }
    }

    /// <summary>
    /// POST: 解除 LINE 綁定
    /// </summary>
    public async Task<IActionResult> OnPostUnbindAsync()
    {
        try
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("無法取得使用者 ID from Claims");
                ErrorMessage = "系統錯誤:無法識別使用者身份";
                return RedirectToPage();
            }

            var success = await _lineBindingService.UnbindAsync(userId);

            if (success)
            {
                _logger.LogInformation("使用者 {UserId} 已解除 LINE 綁定", userId);
                SuccessMessage = "已成功解除 LINE 帳號綁定";
            }
            else
            {
                _logger.LogWarning("使用者 {UserId} 嘗試解除不存在的綁定", userId);
                ErrorMessage = "您尚未綁定 LINE 帳號";
            }

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除 LINE 綁定時發生錯誤");
            ErrorMessage = "解除綁定失敗,請稍後再試";
            return RedirectToPage();
        }
    }

    /// <summary>
    /// POST: 觸發 LINE OAuth 綁定流程
    /// </summary>
    public IActionResult OnPostBindAsync()
    {
        // 檢查是否為訪客帳號
        var lineUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (lineUserIdClaim?.Value == "guest")
        {
            ErrorMessage = "訪客帳號無法綁定 LINE 官方帳號";
            return RedirectToPage();
        }

        // 觸發 LINE OAuth 流程
        var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = Url.Page("/Account/LineBinding")
        };

        return Challenge(properties, "LINE");
    }
}
