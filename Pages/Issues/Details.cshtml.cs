using ClarityDesk.Models.DTOs;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Issues
{
    /// <summary>
    /// 回報單詳情頁面模型
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly IIssueReportService _issueReportService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            IIssueReportService issueReportService,
            ILogger<DetailsModel> logger)
        {
            _issueReportService = issueReportService;
            _logger = logger;
        }

        /// <summary>
        /// 回報單詳情資料
        /// </summary>
        public IssueReportDto? IssueReport { get; set; }

        /// <summary>
        /// 載入回報單詳情
        /// </summary>
        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                _logger.LogInformation("載入回報單詳情，ID: {IssueId}", id);

                IssueReport = await _issueReportService.GetIssueReportByIdAsync(id);
                
                if (IssueReport == null)
                {
                    _logger.LogWarning("找不到回報單，ID: {IssueId}", id);
                    TempData["ErrorMessage"] = "找不到指定的回報單。";
                    return RedirectToPage("Index");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入回報單詳情時發生錯誤，ID: {IssueId}", id);
                TempData["ErrorMessage"] = "載入回報單詳情時發生錯誤，請稍後再試。";
                return RedirectToPage("Index");
            }
        }

        /// <summary>
        /// 處理刪除回報單
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("刪除回報單，ID: {IssueId}", id);

                bool success = await _issueReportService.DeleteIssueReportAsync(id);
                
                if (!success)
                {
                    _logger.LogWarning("刪除回報單失敗，ID: {IssueId}", id);
                    TempData["ErrorMessage"] = "刪除回報單失敗，請確認回報單是否存在。";
                    return RedirectToPage("Details", new { id });
                }

                _logger.LogInformation("成功刪除回報單，ID: {IssueId}", id);
                TempData["SuccessMessage"] = "回報單已成功刪除！";

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除回報單時發生錯誤，ID: {IssueId}", id);
                TempData["ErrorMessage"] = "刪除回報單時發生錯誤，請稍後再試。";
                return RedirectToPage("Details", new { id });
            }
        }
    }
}
