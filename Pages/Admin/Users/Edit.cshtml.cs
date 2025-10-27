using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Admin.Users;

/// <summary>
/// 編輯使用者頁面 PageModel
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IUserManagementService userManagementService,
        ILogger<EditModel> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// 使用者資料
    /// </summary>
    public UserDto UserData { get; set; } = null!;

    /// <summary>
    /// 表單輸入
    /// </summary>
    [BindProperty]
    public UpdateUserDto Input { get; set; } = null!;

    /// <summary>
    /// 載入編輯頁面
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "找不到指定的使用者";
                return RedirectToPage("./Index");
            }

            UserData = user;
            Input = new UpdateUserDto
            {
                DisplayName = user.DisplayName,
                Email = user.Email
            };

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入使用者 {UserId} 編輯頁面時發生錯誤", id);
            TempData["ErrorMessage"] = "載入使用者資料失敗,請稍後再試";
            return RedirectToPage("./Index");
        }
    }

    /// <summary>
    /// 處理更新使用者資料
    /// </summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            // 重新載入使用者資料以顯示頁面
            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "找不到指定的使用者";
                return RedirectToPage("./Index");
            }
            UserData = user;
            return Page();
        }

        try
        {
            var result = await _userManagementService.UpdateUserAsync(id, Input);

            if (result)
            {
                TempData["SuccessMessage"] = "已成功更新使用者資料";
                _logger.LogInformation("使用者 {UserId} 的資料已更新", id);
                return RedirectToPage("./Index");
            }
            else
            {
                TempData["ErrorMessage"] = "更新使用者資料失敗,請確認使用者是否存在";
                _logger.LogWarning("嘗試更新不存在的使用者 {UserId}", id);
                return RedirectToPage("./Index");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新使用者 {UserId} 資料時發生錯誤", id);
            ModelState.AddModelError(string.Empty, "更新使用者資料時發生錯誤,請稍後再試");
            
            // 重新載入使用者資料以顯示頁面
            var user = await _userManagementService.GetUserByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "找不到指定的使用者";
                return RedirectToPage("./Index");
            }
            UserData = user;
            return Page();
        }
    }
}
