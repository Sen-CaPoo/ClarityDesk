using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClarityDesk.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // 檢查用戶是否已登入
            if (User.Identity?.IsAuthenticated == true)
            {
                // 已登入，導向回報單列表頁面
                return RedirectToPage("/Issues/Index");
            }
            else
            {
                // 未登入，導向登入頁面
                return RedirectToPage("/Account/Login");
            }
        }
    }
}
