using System.ComponentModel.DataAnnotations;

namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// 回報單處理狀態
    /// </summary>
    public enum IssueStatus
    {
        /// <summary>
        /// 未處理
        /// </summary>
        [Display(Name = "未處理")]
        Pending,
        
        /// <summary>
        /// 已處理
        /// </summary>
        [Display(Name = "已處理")]
        Completed
    }
}
