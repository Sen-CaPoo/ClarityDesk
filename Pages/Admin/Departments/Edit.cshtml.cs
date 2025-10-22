using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Admin.Departments;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IDepartmentService _departmentService;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IDepartmentService departmentService,
        IUserManagementService userManagementService,
        ILogger<EditModel> logger)
    {
        _departmentService = departmentService;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    [BindProperty]
    public UpdateDepartmentDto Input { get; set; } = new();

    [BindProperty]
    public List<int> SelectedUserIds { get; set; } = new();

    public List<UserDto> AllUsers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id.Value);
            
            if (department == null)
            {
                return NotFound();
            }

            Input = new UpdateDepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                IsActive = department.IsActive
            };

            // 載入所有使用者 (包含停用的,方便查看已指派但停用的使用者)
            var users = await _userManagementService.GetAllUsersAsync(includeInactive: true);
            AllUsers = users.ToList();

            // 已指派的使用者 ID
            SelectedUserIds = department.AssignedUsers?.Select(u => u.Id).ToList() ?? new List<int>();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入單位資料時發生錯誤: DepartmentId {DepartmentId}", id);
            TempData["ErrorMessage"] = "載入單位資料時發生錯誤";
            return RedirectToPage("./Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // 重新載入使用者清單
            var users = await _userManagementService.GetAllUsersAsync(includeInactive: true);
            AllUsers = users.ToList();
            return Page();
        }

        try
        {
            // 更新單位基本資訊
            var updateResult = await _departmentService.UpdateDepartmentAsync(Input.Id, Input);
            
            if (!updateResult)
            {
                ModelState.AddModelError(string.Empty, "更新單位失敗");
                var users = await _userManagementService.GetAllUsersAsync(includeInactive: true);
                AllUsers = users.ToList();
                return Page();
            }

            // 更新處理人員指派
            await _departmentService.AssignUsersToDepartmentAsync(Input.Id, SelectedUserIds);

            TempData["SuccessMessage"] = $"單位「{Input.Name}」已成功更新";
            
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新單位時發生錯誤: DepartmentId {DepartmentId}", Input.Id);
            ModelState.AddModelError(string.Empty, "更新單位時發生錯誤,請稍後再試");
            
            var users = await _userManagementService.GetAllUsersAsync(includeInactive: true);
            AllUsers = users.ToList();
            return Page();
        }
    }
}
