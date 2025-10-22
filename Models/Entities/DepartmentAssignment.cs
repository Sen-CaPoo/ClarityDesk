namespace ClarityDesk.Models.Entities
{
    /// <summary>
    /// 部門指派關聯實體 (多對多關聯表)
    /// </summary>
    public class DepartmentAssignment
    {
        /// <summary>
        /// 主鍵ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 回報單 ID
        /// </summary>
        public int IssueReportId { get; set; }
        
        /// <summary>
        /// 回報單 (導覽屬性)
        /// </summary>
        public IssueReport? IssueReport { get; set; }
        
        /// <summary>
        /// 單位 ID
        /// </summary>
        public int DepartmentId { get; set; }
        
        /// <summary>
        /// 單位 (導覽屬性)
        /// </summary>
        public Department? Department { get; set; }
        
        /// <summary>
        /// 指派時間
        /// </summary>
        public DateTime AssignedAt { get; set; }
    }
}
