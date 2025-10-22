using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.Entities
{
    /// <summary>
    /// 回報單實體
    /// </summary>
    public class IssueReport
    {
        /// <summary>
        /// 主鍵ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 問題標題
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// 問題內容詳述
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 紀錄日期
        /// </summary>
        public DateTime RecordDate { get; set; }
        
        /// <summary>
        /// 處理狀態
        /// </summary>
        public IssueStatus Status { get; set; }
        
        /// <summary>
        /// 緊急程度
        /// </summary>
        public PriorityLevel Priority { get; set; }
        
        /// <summary>
        /// 回報人姓名
        /// </summary>
        public string ReporterName { get; set; } = string.Empty;
        
        /// <summary>
        /// 顧客聯絡人姓名
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;
        
        /// <summary>
        /// 顧客連絡電話
        /// </summary>
        public string CustomerPhone { get; set; } = string.Empty;
        
        /// <summary>
        /// 指派處理人員 ID
        /// </summary>
        public int AssignedUserId { get; set; }
        
        /// <summary>
        /// 指派處理人員 (導覽屬性)
        /// </summary>
        public User? AssignedUser { get; set; }
        
        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 最後修改時間
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// 最後修改人 ID (可為 null，表示尚未修改過)
        /// </summary>
        public int? LastModifiedByUserId { get; set; }
        
        /// <summary>
        /// 最後修改人 (導覽屬性)
        /// </summary>
        public User? LastModifiedBy { get; set; }
        
        /// <summary>
        /// 部門指派關聯 (導覽屬性)
        /// </summary>
        public ICollection<DepartmentAssignment> DepartmentAssignments { get; set; } = new List<DepartmentAssignment>();
    }
}
