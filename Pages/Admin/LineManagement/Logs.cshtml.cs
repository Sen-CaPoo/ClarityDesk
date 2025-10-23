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
    /// LINE 訊息日誌查詢頁面
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class LogsModel : PageModel
    {
        private readonly ILineMessagingService _lineMessagingService;
        private readonly ILogger<LogsModel> _logger;

        public LogsModel(ILineMessagingService lineMessagingService, ILogger<LogsModel> logger)
        {
            _lineMessagingService = lineMessagingService;
            _logger = logger;
        }

        /// <summary>
        /// 訊息日誌列表
        /// </summary>
        public PagedResult<LineMessageLogDto> MessageLogs { get; set; } = new PagedResult<LineMessageLogDto>
        {
            Items = new List<LineMessageLogDto>(),
            TotalCount = 0,
            CurrentPage = 1,
            PageSize = 50
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
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// 篩選條件：LINE User ID
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? LineUserIdFilter { get; set; }

        /// <summary>
        /// 篩選條件：訊息方向
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public MessageDirection? DirectionFilter { get; set; }

        /// <summary>
        /// 篩選條件：發送狀態
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool? SuccessFilter { get; set; }

        /// <summary>
        /// 篩選條件：起始日期
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 篩選條件：結束日期
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 方向選項列表
        /// </summary>
        public List<SelectListItem> DirectionOptions { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// 狀態選項列表
        /// </summary>
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("載入 LINE 訊息日誌，頁碼: {PageNumber}，起始日期: {StartDate}，結束日期: {EndDate}",
                    PageNumber, StartDate, EndDate);

                // 預設日期範圍為最近7天
                if (!StartDate.HasValue && !EndDate.HasValue)
                {
                    EndDate = DateTime.Today;
                    StartDate = DateTime.Today.AddDays(-7);
                }

                // 載入分頁資料
                MessageLogs = await _lineMessagingService.GetMessageLogsAsync(
                    lineUserId: LineUserIdFilter,
                    direction: DirectionFilter,
                    isSuccess: SuccessFilter,
                    startDate: StartDate,
                    endDate: EndDate,
                    pageNumber: PageNumber,
                    pageSize: PageSize
                );

                // 準備方向篩選選項
                DirectionOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "全部方向" },
                    new SelectListItem 
                    { 
                        Value = MessageDirection.Outbound.ToString(), 
                        Text = "發送",
                        Selected = DirectionFilter == MessageDirection.Outbound
                    },
                    new SelectListItem 
                    { 
                        Value = MessageDirection.Inbound.ToString(), 
                        Text = "接收",
                        Selected = DirectionFilter == MessageDirection.Inbound
                    }
                };

                // 準備狀態篩選選項
                StatusOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "全部狀態" },
                    new SelectListItem 
                    { 
                        Value = "true", 
                        Text = "成功",
                        Selected = SuccessFilter == true
                    },
                    new SelectListItem 
                    { 
                        Value = "false", 
                        Text = "失敗",
                        Selected = SuccessFilter == false
                    }
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入 LINE 訊息日誌失敗");
                TempData["ErrorMessage"] = "載入訊息日誌失敗，請稍後再試";

                // 返回空列表
                MessageLogs = new PagedResult<LineMessageLogDto>
                {
                    Items = new List<LineMessageLogDto>(),
                    TotalCount = 0,
                    CurrentPage = PageNumber,
                    PageSize = PageSize
                };

                DirectionOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "全部方向" }
                };

                StatusOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "全部狀態" }
                };

                return Page();
            }
        }
    }
}
