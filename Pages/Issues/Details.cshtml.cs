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
        private readonly IIssueReportTokenService? _tokenService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            IIssueReportService issueReportService,
            ILogger<DetailsModel> logger,
            IIssueReportTokenService? tokenService = null)
        {
            _issueReportService = issueReportService;
            _logger = logger;
            _tokenService = tokenService;
        }

        /// <summary>
        /// 回報單詳情資料
        /// </summary>
        public IssueReportDto? IssueReport { get; set; }

        /// <summary>
        /// 載入回報單詳情
        /// </summary>
        public async Task<IActionResult> OnGetAsync(int id, string? token = null)
        {
            try
            {
                _logger.LogInformation("載入回報單詳情，ID: {IssueId}", id);

                // 如果有提供 Token，進行驗證 (從 LINE 通知連結進入)
                if (!string.IsNullOrEmpty(token) && _tokenService != null)
                {
                    var validatedId = _tokenService.ValidateToken(token);
                    
                    if (validatedId == null || validatedId.Value != id)
                    {
                        _logger.LogWarning("Token 驗證失敗: IssueId={IssueId}", id);
                        TempData["ErrorMessage"] = "無效的存取連結，請重新從 LINE 通知進入。";
                        return RedirectToPage("/Account/AccessDenied");
                    }

                    _logger.LogInformation("Token 驗證成功: IssueId={IssueId}", id);
                }

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

        /// <summary>
        /// 快速調整回報單狀態
        /// </summary>
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            try
            {
                _logger.LogInformation("快速調整回報單狀態，ID: {IssueId}", id);

                // 取得當前使用者 ID
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    _logger.LogError("無法取得當前使用者 ID");
                    TempData["ErrorMessage"] = "系統錯誤，無法識別使用者身份。";
                    return RedirectToPage("Details", new { id });
                }

                // 先取得目前狀態
                var issue = await _issueReportService.GetIssueReportByIdAsync(id);
                if (issue == null)
                {
                    _logger.LogWarning("找不到回報單，ID: {IssueId}", id);
                    TempData["ErrorMessage"] = "找不到指定的回報單。";
                    return RedirectToPage("Index");
                }

                // 切換狀態: 未處理 ⇄ 已處理
                var newStatus = issue.Status == Models.Enums.IssueStatus.Pending 
                    ? Models.Enums.IssueStatus.Completed 
                    : Models.Enums.IssueStatus.Pending;

                bool success = await _issueReportService.UpdateIssueStatusAsync(id, newStatus, currentUserId);
                
                if (!success)
                {
                    _logger.LogWarning("更新回報單狀態失敗，ID: {IssueId}", id);
                    TempData["ErrorMessage"] = "更新回報單狀態失敗。";
                    return RedirectToPage("Details", new { id });
                }

                var statusText = newStatus == Models.Enums.IssueStatus.Completed ? "已處理" : "未處理";
                _logger.LogInformation("成功更新回報單狀態為 {Status}，ID: {IssueId}", statusText, id);
                TempData["SuccessMessage"] = $"回報單狀態已更新為「{statusText}」！";

                return RedirectToPage("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速調整回報單狀態時發生錯誤，ID: {IssueId}", id);
                TempData["ErrorMessage"] = "更新回報單狀態時發生錯誤，請稍後再試。";
                return RedirectToPage("Details", new { id });
            }
        }
    }
}
