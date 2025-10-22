using System.ComponentModel.DataAnnotations;

namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// 回報單緊急程度
    /// </summary>
    public enum PriorityLevel
    {
        /// <summary>
        /// 低 (綠色標示)
        /// </summary>
        [Display(Name = "低")]
        Low,
        
        /// <summary>
        /// 中 (橙色標示)
        /// </summary>
        [Display(Name = "中")]
        Medium,
        
        /// <summary>
        /// 高 (紅色標示)
        /// </summary>
        [Display(Name = "高")]
        High
    }
}
