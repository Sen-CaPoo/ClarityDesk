using ClarityDesk.Models.Enums;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ClarityDesk.Infrastructure.TagHelpers;

/// <summary>
/// 狀態徽章 Tag Helper
/// 使用方式: &lt;status-badge status="@issue.Status"&gt;&lt;/status-badge&gt;
/// </summary>
[HtmlTargetElement("status-badge")]
public class StatusBadgeTagHelper : TagHelper
{
    [HtmlAttributeName("status")]
    public IssueStatus Status { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;

        var (badgeClass, displayText) = Status switch
        {
            IssueStatus.Pending => ("badge-status-pending", "未處理"),
            IssueStatus.Completed => ("badge-status-completed", "已處理"),
            _ => ("badge-secondary", "未知")
        };

        output.Attributes.SetAttribute("class", $"badge {badgeClass}");
        output.Content.SetContent(displayText);
    }
}
