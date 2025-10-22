using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Admin.Departments;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IDepartmentService departmentService, ILogger<IndexModel> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    public List<DepartmentDto> Departments { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; } = false;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var departments = await _departmentService.GetAllDepartmentsAsync(activeOnly: !IncludeInactive);
            Departments = departments.ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入單位清單時發生錯誤");
            TempData["ErrorMessage"] = "載入單位清單時發生錯誤";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        try
        {
            var result = await _departmentService.DeleteDepartmentAsync(id);
            
            if (result)
            {
                TempData["SuccessMessage"] = "單位已成功停用";
            }
            else
            {
                TempData["ErrorMessage"] = "停用單位失敗,請稍後再試";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用單位時發生錯誤: DepartmentId {DepartmentId}", id);
            TempData["ErrorMessage"] = "停用單位時發生錯誤";
        }

        return RedirectToPage(new { includeInactive = IncludeInactive });
    }
}
