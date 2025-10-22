using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Admin.Users;

/// <summary>
/// 使用者權限管理頁面 PageModel
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IUserManagementService userManagementService,
        ILogger<IndexModel> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// 使用者清單
    /// </summary>
    public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();

    /// <summary>
    /// 是否包含停用使用者
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; }

    /// <summary>
    /// 載入使用者清單
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Users = await _userManagementService.GetAllUsersAsync(IncludeInactive);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入使用者清單時發生錯誤");
            TempData["ErrorMessage"] = "載入使用者清單失敗,請稍後再試";
            Users = new List<UserDto>();
            return Page();
        }
    }

    /// <summary>
    /// 處理權限變更
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="newRole">新角色</param>
    public async Task<IActionResult> OnPostUpdateRoleAsync(int userId, UserRole newRole)
    {
        try
        {
            var result = await _userManagementService.UpdateUserRoleAsync(userId, newRole);

            if (result)
            {
                var roleName = newRole == UserRole.Admin ? "管理人員" : "普通使用者";
                TempData["SuccessMessage"] = $"已成功將使用者權限變更為{roleName}";
                _logger.LogInformation("使用者 {UserId} 的角色已變更為 {Role}", userId, newRole);
            }
            else
            {
                TempData["ErrorMessage"] = "更新使用者權限失敗,請確認使用者是否存在";
                _logger.LogWarning("嘗試更新不存在的使用者 {UserId} 的角色", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新使用者 {UserId} 權限時發生錯誤", userId);
            TempData["ErrorMessage"] = "更新使用者權限時發生錯誤,請稍後再試";
        }

        return RedirectToPage(new { IncludeInactive });
    }

    /// <summary>
    /// 處理啟用/停用切換
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <param name="isActive">是否啟用</param>
    public async Task<IActionResult> OnPostToggleActiveAsync(int userId, bool isActive)
    {
        try
        {
            var result = await _userManagementService.SetUserActiveStatusAsync(userId, isActive);

            if (result)
            {
                var statusText = isActive ? "啟用" : "停用";
                TempData["SuccessMessage"] = $"已成功{statusText}使用者";
                _logger.LogInformation("使用者 {UserId} 已被{Status}", userId, statusText);
            }
            else
            {
                TempData["ErrorMessage"] = "更新使用者狀態失敗,請確認使用者是否存在";
                _logger.LogWarning("嘗試更新不存在的使用者 {UserId} 的狀態", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新使用者 {UserId} 狀態時發生錯誤", userId);
            TempData["ErrorMessage"] = "更新使用者狀態時發生錯誤,請稍後再試";
        }

        return RedirectToPage(new { IncludeInactive });
    }
}
