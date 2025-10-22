using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Admin.Departments;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IDepartmentService departmentService, ILogger<CreateModel> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    [BindProperty]
    public CreateDepartmentDto Input { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var departmentId = await _departmentService.CreateDepartmentAsync(Input);
            
            TempData["SuccessMessage"] = $"單位「{Input.Name}」已成功建立";
            
            return RedirectToPage("./Edit", new { id = departmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立單位時發生錯誤: {DepartmentName}", Input.Name);
            ModelState.AddModelError(string.Empty, "建立單位時發生錯誤,請稍後再試");
            return Page();
        }
    }
}
