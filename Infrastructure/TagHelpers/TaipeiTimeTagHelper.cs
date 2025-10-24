using ClarityDesk.Infrastructure.Helpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace ClarityDesk.Infrastructure.TagHelpers
{
    /// <summary>
    /// 台北時間顯示 Tag Helper
    /// 用法: <taipei-time value="@Model.CreatedAt" format="yyyy/MM/dd HH:mm:ss" />
    /// </summary>
    [HtmlTargetElement("taipei-time")]
    public class TaipeiTimeTagHelper : TagHelper
    {
        /// <summary>
        /// UTC 時間值
        /// </summary>
        [HtmlAttributeName("value")]
        public DateTime Value { get; set; }

        /// <summary>
        /// 顯示格式 (預設: yyyy/MM/dd HH:mm:ss)
        /// </summary>
        [HtmlAttributeName("format")]
        public string Format { get; set; } = "yyyy/MM/dd HH:mm:ss";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // 轉換為台北時間
            var taipeiTime = TimeZoneHelper.ConvertToTaipeiTime(Value);
            
            // 輸出格式化的時間字串
            output.TagName = null; // 移除標籤本身
            output.Content.SetContent(taipeiTime.ToString(Format));
        }
    }
}
