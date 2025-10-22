using ClarityDesk.Data;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace ClarityDesk.Pages.Issues
{
    /// <summary>
    /// 回報單列表頁面模型
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IIssueReportService _issueReportService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IIssueReportService issueReportService,
            ApplicationDbContext context,
            ILogger<IndexModel> logger)
        {
            _issueReportService = issueReportService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 分頁回報單資料
        /// </summary>
        public PagedResult<IssueReportDto> PagedIssues { get; set; } = new PagedResult<IssueReportDto>();

        /// <summary>
        /// 篩選條件
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public IssueFilterDto Filter { get; set; } = new IssueFilterDto();

        /// <summary>
        /// 當前頁碼
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public new int Page { get; set; } = 1;

        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 統計資訊
        /// </summary>
        public IssueStatisticsDto? Statistics { get; set; }

        /// <summary>
        /// 可用單位清單（用於篩選）
        /// </summary>
        public List<Department> Departments { get; set; } = new List<Department>();

        /// <summary>
        /// 可用使用者清單（用於篩選）
        /// </summary>
        public List<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// 載入回報單列表
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("載入回報單列表,頁碼: {Page}, 篩選條件: {@Filter}", Page, Filter);

                // 載入統計資訊
                Statistics = await _issueReportService.GetIssueStatisticsAsync();

                // 載入分頁資料
                PagedIssues = await _issueReportService.GetIssueReportsAsync(
                    filter: Filter,
                    page: Page,
                    pageSize: PageSize
                );

                // 載入篩選選項資料（單位、使用者）
                await LoadFilterOptionsAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入回報單列表時發生錯誤");
                TempData["ErrorMessage"] = "載入回報單列表時發生錯誤，請稍後再試。";
                return Page();
            }
        }

        /// <summary>
        /// 載入篩選選項資料
        /// </summary>
        private async Task LoadFilterOptionsAsync()
        {
            try
            {
                // 載入單位清單
                var departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .AsNoTracking()
                    .ToListAsync();
                Departments = departments;

                // 載入使用者清單
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.DisplayName)
                    .AsNoTracking()
                    .ToListAsync();
                Users = users;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "載入篩選選項資料時發生錯誤");
                // 不影響主要功能，僅記錄警告
                Departments = new List<Department>();
                Users = new List<User>();
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try
            {
                _logger.LogInformation("匯出回報單 Excel，篩選條件: {@Filter}", Filter);

                // 設定 EPPlus 授權 (非商業用途)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // 取得所有符合條件的資料（不分頁）
                var allIssues = await _issueReportService.GetIssueReportsAsync(
                    filter: Filter,
                    page: 1,
                    pageSize: int.MaxValue // 取得所有資料
                );

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("回報單列表");

                // 設定標題列
                var headers = new[]
                {
                    "單號", "主旨", "處理狀態", "回報人", "聯絡人", "連絡電話",
                    "紀錄日期", "緊急程度", "指派人員", "所屬單位",
                    "建立時間", "最後修改時間", "最後修改人", "備註"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 填入資料
                int row = 2;
                foreach (var issue in allIssues.Items)
                {
                    worksheet.Cells[row, 1].Value = issue.Id;
                    worksheet.Cells[row, 2].Value = issue.Title;
                    worksheet.Cells[row, 3].Value = issue.StatusText;
                    worksheet.Cells[row, 4].Value = issue.ReporterName;
                    worksheet.Cells[row, 5].Value = issue.CustomerName;
                    worksheet.Cells[row, 6].Value = issue.CustomerPhone;
                    worksheet.Cells[row, 7].Value = issue.RecordDate.ToString("yyyy/MM/dd HH:mm:ss");
                    worksheet.Cells[row, 8].Value = issue.PriorityText;
                    worksheet.Cells[row, 9].Value = issue.AssignedUserName;
                    worksheet.Cells[row, 10].Value = string.Join(", ", issue.DepartmentNames);
                    worksheet.Cells[row, 11].Value = issue.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
                    worksheet.Cells[row, 12].Value = issue.UpdatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
                    worksheet.Cells[row, 13].Value = issue.LastModifiedByUserName ?? "";
                    worksheet.Cells[row, 14].Value = issue.Content; // 備註使用內容欄位

                    row++;
                }

                // 自動調整欄寬
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // 產生檔案
                var fileName = $"回報單列表_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var fileBytes = package.GetAsByteArray();

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出 Excel 時發生錯誤");
                TempData["ErrorMessage"] = "匯出 Excel 時發生錯誤，請稍後再試。";
                return RedirectToPage();
            }
        }
    }
}
