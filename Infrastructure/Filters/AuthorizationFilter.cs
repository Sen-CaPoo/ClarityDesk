using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClarityDesk.Infrastructure.Filters;

/// <summary>
/// 自訂授權過濾器,驗證使用者登入狀態與權限
/// </summary>
public class AuthorizationFilter : IAuthorizationFilter
{
    private readonly ILogger<AuthorizationFilter> _logger;

    public AuthorizationFilter(ILogger<AuthorizationFilter> logger)
    {
        _logger = logger;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // 檢查使用者是否已驗證
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("未驗證的使用者嘗試訪問受保護資源: {Path}", 
                context.HttpContext.Request.Path);

            context.Result = new RedirectToPageResult("/Account/Login", 
                new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        // 檢查使用者是否為停用狀態
        var isActiveClaim = context.HttpContext.User.FindFirst("IsActive")?.Value;
        if (isActiveClaim == "False")
        {
            _logger.LogWarning("已停用的使用者嘗試訪問系統: {UserId}", 
                context.HttpContext.User.Identity?.Name ?? "Unknown");

            context.Result = new RedirectToPageResult("/Account/AccessDenied");
        }
    }
}
