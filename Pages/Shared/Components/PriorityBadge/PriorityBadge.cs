using ClarityDesk.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClarityDesk.Pages.Shared.Components.PriorityBadge;

/// <summary>
/// 優先級徽章 ViewComponent
/// 根據優先級顯示不同顏色的徽章 (High=紅色, Medium=橙色, Low=綠色)
/// </summary>
public class PriorityBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PriorityLevel priority)
    {
        return View(priority);
    }
}
