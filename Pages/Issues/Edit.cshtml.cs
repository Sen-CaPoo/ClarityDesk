using ClarityDesk.Infrastructure.Helpers;
using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages.Issues
{
    /// <summary>
    /// 編輯回報單頁面模型
    /// </summary>
    public class EditModel : PageModel
    {
        private readonly IIssueReportService _issueReportService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            IIssueReportService issueReportService,
            IDepartmentService departmentService,
            IUserManagementService userManagementService,
            ILogger<EditModel> logger)
        {
            _issueReportService = issueReportService;
            _departmentService = departmentService;
            _userManagementService = userManagementService;
            _logger = logger;
        }

        /// <summary>
        /// 回報單 ID
        /// </summary>
        [BindProperty]
        public int Id { get; set; }

        /// <summary>
        /// 回報單資料綁定
        /// </summary>
        [BindProperty]
        public UpdateIssueReportDto IssueReport { get; set; } = new UpdateIssueReportDto();

        /// <summary>
        /// 可用單位清單
        /// </summary>
        public List<Department> Departments { get; set; } = new List<Department>();

        /// <summary>
        /// 可用使用者清單（處理人員）
        /// </summary>
        public List<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// 載入編輯頁面
        /// </summary>
        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                Id = id;
                
                // 載入回報單資料
                var issueDto = await _issueReportService.GetIssueReportByIdAsync(id);
                if (issueDto == null)
                {
                    _logger.LogWarning("找不到回報單 ID: {IssueId}", id);
                    TempData["ErrorMessage"] = "找不到指定的回報單。";
                    return RedirectToPage("Index");
                }

                // 轉換為 UpdateDto
                IssueReport = new UpdateIssueReportDto
                {
                    Title = issueDto.Title,
                    Content = issueDto.Content,
                    // 使用 CreatedAt 而非 RecordDate，因為 CreatedAt 有完整的時間資訊
                    // 將 UTC 時間轉換為台北時間，並設為 Unspecified 讓瀏覽器當作本地時間
                    // 去除毫秒部分，只保留到秒
                    RecordDate = DateTime.SpecifyKind(
                        new DateTime(
                            TimeZoneHelper.ConvertToTaipeiTime(issueDto.CreatedAt).Year,
                            TimeZoneHelper.ConvertToTaipeiTime(issueDto.CreatedAt).Month,
                            TimeZoneHelper.ConvertToTaipeiTime(issueDto.CreatedAt).Day,
                            TimeZoneHelper.ConvertToTaipeiTime(issueDto.CreatedAt).Hour,
                            TimeZoneHelper.ConvertToTaipeiTime(issueDto.CreatedAt).Minute,
                            TimeZoneHelper.ConvertToTaipeiTime(issueDto.CreatedAt).Second),
                        DateTimeKind.Unspecified),
                    Status = issueDto.Status,
                    Priority = issueDto.Priority,
                    ReporterName = issueDto.ReporterName,
                    CustomerName = issueDto.CustomerName,
                    CustomerPhone = issueDto.CustomerPhone,
                    AssignedUserId = issueDto.AssignedUserId,
                    DepartmentIds = issueDto.DepartmentIds ?? new List<int>()
                };

                // 載入參考資料
                await LoadReferenceDataAsync();
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯回報單頁面時發生錯誤，ID: {IssueId}", id);
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
                _logger.LogInformation("更新回報單 ID: {IssueId}, 資料: {@IssueReport}", Id, IssueReport);

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

                // 更新回報單
                // 取得當前使用者 ID
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    _logger.LogError("無法取得當前使用者 ID");
                    ModelState.AddModelError(string.Empty, "系統錯誤，無法識別使用者身份。");
                    await LoadReferenceDataAsync();
                    return Page();
                }

                bool success = await _issueReportService.UpdateIssueReportAsync(Id, IssueReport, currentUserId);
                
                if (!success)
                {
                    _logger.LogWarning("更新回報單失敗，ID: {IssueId}", Id);
                    ModelState.AddModelError(string.Empty, "更新回報單失敗，請確認資料是否正確。");
                    await LoadReferenceDataAsync();
                    return Page();
                }

                _logger.LogInformation("成功更新回報單，ID: {IssueId}", Id);
                TempData["SuccessMessage"] = "回報單已成功更新！";

                return RedirectToPage("Details", new { id = Id });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "更新回報單時發生驗證錯誤，ID: {IssueId}", Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadReferenceDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新回報單時發生錯誤，ID: {IssueId}", Id);
                ModelState.AddModelError(string.Empty, "更新回報單時發生錯誤，請稍後再試。");
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
                var departmentDtos = await _departmentService.GetAllDepartmentsAsync(activeOnly: true);
                Departments = departmentDtos.Select(d => new Department
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    IsActive = d.IsActive
                }).ToList();
                
                // 載入啟用的使用者清單
                var userDtos = await _userManagementService.GetAllUsersAsync(includeInactive: false);
                Users = userDtos.Select(u => new User
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    Role = u.Role
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "載入參考資料時發生錯誤");
                // 如果載入失敗，使用空清單
                Departments = new List<Department>();
                Users = new List<User>();
            }
        }
    }
}
