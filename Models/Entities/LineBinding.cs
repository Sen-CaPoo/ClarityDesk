namespace ClarityDesk.Models.Entities
{
    /// <summary>
    /// LINE 綁定記錄
    /// 儲存系統使用者帳號與 LINE 使用者識別碼的對應關係
    /// </summary>
    public class LineBinding
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 系統使用者 ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// LINE 使用者唯一識別碼（由 LINE 提供）
        /// </summary>
        public string LineUserId { get; set; } = string.Empty;

        /// <summary>
        /// LINE 顯示名稱（綁定時擷取）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// LINE 頭像 URL（綁定時擷取）
        /// </summary>
        public string? PictureUrl { get; set; }

        /// <summary>
        /// 綁定是否有效
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 綁定時間
        /// </summary>
        public DateTime BoundAt { get; set; }

        /// <summary>
        /// 解除綁定時間
        /// </summary>
        public DateTime? UnboundAt { get; set; }

        /// <summary>
        /// 建立時間（自動管理）
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新時間（自動管理）
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        /// <summary>
        /// 關聯的系統使用者
        /// </summary>
        public User? User { get; set; }
    }
}
