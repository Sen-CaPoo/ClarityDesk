using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;
using ClarityDesk.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClarityDesk.Pages.Admin.LineManagement
{
    /// <summary>
    /// LINE 帳號綁定管理頁面 - 顯示所有綁定列表
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ILineBindingService _lineBindingService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILineBindingService lineBindingService, ILogger<IndexModel> logger)
        {
            _lineBindingService = lineBindingService;
            _logger = logger;
        }

        /// <summary>
        /// 綁定列表
        /// </summary>
        public PagedResult<LineBindingDto> Bindings { get; set; } = new PagedResult<LineBindingDto>
        {
            Items = new List<LineBindingDto>(),
            TotalCount = 0,
            CurrentPage = 1,
            PageSize = 20
        };

        /// <summary>
        /// 當前頁碼
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每頁筆數
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 篩選條件：綁定狀態
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public BindingStatus? StatusFilter { get; set; }

        /// <summary>
        /// 篩選條件：搜尋關鍵字（使用者名稱或 LINE 顯示名稱）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// 排序欄位
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "BoundAt";

        /// <summary>
        /// 排序方向
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = true;

        /// <summary>
        /// 狀態選項列表
        /// </summary>
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("載入 LINE 綁定列表，頁碼: {PageNumber}，篩選狀態: {Status}，搜尋: {Keyword}",
                    PageNumber, StatusFilter, SearchKeyword);

                // 載入分頁資料
                Bindings = await _lineBindingService.GetAllBindingsAsync(
                    status: StatusFilter,
                    pageNumber: PageNumber,
                    pageSize: PageSize,
                    searchKeyword: SearchKeyword
                );

                // 準備狀態篩選選項
                StatusOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "全部狀態" },
                    new SelectListItem 
                    { 
                        Value = BindingStatus.Active.ToString(), 
                        Text = "已綁定",
                        Selected = StatusFilter == BindingStatus.Active
                    },
                    new SelectListItem 
                    { 
                        Value = BindingStatus.Blocked.ToString(), 
                        Text = "已封鎖",
                        Selected = StatusFilter == BindingStatus.Blocked
                    },
                    new SelectListItem 
                    { 
                        Value = BindingStatus.Unbound.ToString(), 
                        Text = "已解綁",
                        Selected = StatusFilter == BindingStatus.Unbound
                    }
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入 LINE 綁定列表失敗");
                TempData["ErrorMessage"] = "載入綁定列表失敗，請稍後再試";
                
                // 返回空列表
                Bindings = new PagedResult<LineBindingDto>
                {
                    Items = new List<LineBindingDto>(),
                    TotalCount = 0,
                    CurrentPage = PageNumber,
                    PageSize = PageSize
                };
                
                StatusOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "全部狀態" }
                };

                return Page();
            }
        }

        /// <summary>
        /// 強制解除綁定
        /// </summary>
        public async Task<IActionResult> OnPostUnbindAsync(int bindingId)
        {
            try
            {
                _logger.LogInformation("管理員強制解除綁定，BindingId: {BindingId}", bindingId);

                var success = await _lineBindingService.UnbindByIdAsync(bindingId);

                if (success)
                {
                    TempData["SuccessMessage"] = "已成功解除綁定";
                }
                else
                {
                    TempData["ErrorMessage"] = "解除綁定失敗，請確認綁定是否存在";
                }

                return RedirectToPage(new { PageNumber, PageSize, StatusFilter, SearchKeyword });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "管理員強制解除綁定失敗，BindingId: {BindingId}", bindingId);
                TempData["ErrorMessage"] = "解除綁定失敗，請稍後再試";
                return RedirectToPage(new { PageNumber, PageSize, StatusFilter, SearchKeyword });
            }
        }
    }
}
