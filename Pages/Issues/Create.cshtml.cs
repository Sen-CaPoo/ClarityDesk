using ClarityDesk.Infrastructure.Helpers;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Issues
{
    /// <summary>
    /// 建立新回報單頁面模型
    /// </summary>
    public class CreateModel : PageModel
    {
        private readonly IIssueReportService _issueReportService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            IIssueReportService issueReportService,
            IDepartmentService departmentService,
            IUserManagementService userManagementService,
            ILogger<CreateModel> logger)
        {
            _issueReportService = issueReportService;
            _departmentService = departmentService;
            _userManagementService = userManagementService;
            _logger = logger;
        }

        /// <summary>
        /// 回報單資料綁定
        /// </summary>
        [BindProperty]
        public CreateIssueReportDto IssueReport { get; set; } = new CreateIssueReportDto();

        /// <summary>
        /// 可用單位清單
        /// </summary>
        public List<Department> Departments { get; set; } = new List<Department>();

        /// <summary>
        /// 可用使用者清單（處理人員）
        /// </summary>
        public List<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// 載入建立頁面
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await LoadReferenceDataAsync();
                
                // 設定預設值 - 使用台北時間顯示，但不指定Kind，讓瀏覽器當作本地時間處理
                // 去除毫秒部分，只保留到秒
                var now = TimeZoneHelper.GetTaipeiNow();
                IssueReport.RecordDate = DateTime.SpecifyKind(
                    new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second),
                    DateTimeKind.Unspecified);
                
                // 設定回報人姓名為當前登入使用者
                var currentUserName = User.Identity?.Name;
                if (!string.IsNullOrEmpty(currentUserName))
                {
                    // 從 Claims 中取得顯示名稱
                    var displayNameClaim = User.Claims.FirstOrDefault(c => c.Type == "DisplayName");
                    IssueReport.ReporterName = displayNameClaim?.Value ?? currentUserName;
                }
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入建立回報單頁面時發生錯誤");
                TempData["ErrorMessage"] = "載入頁面時發生錯誤，請稍後再試。";
                return RedirectToPage("Index");
            }
        }

        /// <summary>
        /// 處理表單提交
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadReferenceDataAsync();
                return Page();
            }

            try
            {
                _logger.LogInformation("建立新回報單: {@IssueReport}", IssueReport);

                // 驗證至少選擇一個單位
                if (IssueReport.DepartmentIds == null || IssueReport.DepartmentIds.Count == 0)
                {
                    ModelState.AddModelError("IssueReport.DepartmentIds", "請至少選擇一個問題所屬單位");
                    await LoadReferenceDataAsync();
                    return Page();
                }
                
                // 將瀏覽器傳來的本地時間轉換為 UTC 時間儲存
                if (IssueReport.RecordDate.Kind == DateTimeKind.Unspecified)
                {
                    IssueReport.RecordDate = TimeZoneHelper.ConvertToUtc(IssueReport.RecordDate);
                }

                // 建立回報單
                int issueId = await _issueReportService.CreateIssueReportAsync(IssueReport);

                _logger.LogInformation("成功建立回報單，ID: {IssueId}", issueId);
                TempData["SuccessMessage"] = "回報單已成功建立！";

                return RedirectToPage("Details", new { id = issueId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "建立回報單時發生驗證錯誤");
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadReferenceDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立回報單時發生錯誤");
                ModelState.AddModelError(string.Empty, "建立回報單時發生錯誤，請稍後再試。");
                await LoadReferenceDataAsync();
                return Page();
            }
        }

        /// <summary>
        /// 載入參考資料（單位、使用者）
        /// </summary>
        private async Task LoadReferenceDataAsync()
        {
            try
            {
                // 載入啟用的單位清單
                var departments = await _departmentService.GetAllDepartmentsAsync(activeOnly: true);
                Departments = departments.Select(d => new Department 
                { 
                    Id = d.Id, 
                    Name = d.Name,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                }).ToList();
                
                // 載入啟用的使用者清單
                var users = await _userManagementService.GetAllUsersAsync(includeInactive: false);
                Users = users.Select(u => new User
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "載入參考資料時發生錯誤");
                // 不影響主要功能，僅記錄警告
                Departments = new List<Department>();
                Users = new List<User>();
            }
        }
    }
}
