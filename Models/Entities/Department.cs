namespace ClarityDesk.Models.Entities
{
    /// <summary>
    /// 問題所屬單位實體
    /// </summary>
    public class Department
    {
        /// <summary>
        /// 主鍵ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 單位名稱
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 單位描述
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// 單位狀態 (啟用/停用)
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
