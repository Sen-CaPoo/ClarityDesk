using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Dashboard;

/// <summary>
/// 統計儀表板頁面模型
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IDashboardService dashboardService,
        ILogger<IndexModel> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// 綜合統計資訊
    /// </summary>
    public DashboardSummaryDto Summary { get; set; } = new();

    /// <summary>
    /// 回報人統計資料
    /// </summary>
    public List<ReporterStatisticsDto> ReporterStatistics { get; set; } = new();

    /// <summary>
    /// 處理人統計資料
    /// </summary>
    public List<AssigneeStatisticsDto> AssigneeStatistics { get; set; } = new();

    /// <summary>
    /// 問題所屬單位統計資料
    /// </summary>
    public List<DepartmentStatisticsDto> DepartmentStatistics { get; set; } = new();

    public async Task OnGetAsync()
    {
        _logger.LogInformation("載入統計儀表板頁面");

        try
        {
            // 並行取得所有統計資料
            var summaryTask = _dashboardService.GetDashboardSummaryAsync();
            var reportersTask = _dashboardService.GetReporterStatisticsAsync(10);
            var assigneesTask = _dashboardService.GetAssigneeStatisticsAsync(10);
            var departmentsTask = _dashboardService.GetDepartmentStatisticsAsync(10);

            await Task.WhenAll(summaryTask, reportersTask, assigneesTask, departmentsTask);

            Summary = await summaryTask;
            ReporterStatistics = await reportersTask;
            AssigneeStatistics = await assigneesTask;
            DepartmentStatistics = await departmentsTask;

            _logger.LogInformation("成功載入統計儀表板資料");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入統計儀表板資料時發生錯誤");
            TempData["ErrorMessage"] = "載入統計資料時發生錯誤，請稍後再試";
        }
    }
}
