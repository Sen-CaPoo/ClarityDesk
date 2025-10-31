using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.Entities
{
    /// <summary>
    /// LINE 對話狀態
    /// 儲存使用者在 LINE 端回報問題時的對話進度和暫存資料
    /// </summary>
    public class LineConversationState
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// LINE 使用者 ID
        /// </summary>
        public string LineUserId { get; set; } = string.Empty;

        /// <summary>
        /// 系統使用者 ID（從綁定帶入）
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 當前對話步驟
        /// </summary>
        public ConversationStep CurrentStep { get; set; }

        /// <summary>
        /// 暫存：問題標題
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 暫存：問題內容
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// 暫存：所屬單位 ID
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// 暫存：緊急程度（Low/Medium/High）
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// 暫存：聯絡人姓名
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 暫存：聯絡電話
        /// </summary>
        public string? CustomerPhone { get; set; }

        /// <summary>
        /// 暫存：圖片附件 URL（JSON 陣列）
        /// </summary>
        public string? ImageUrls { get; set; }

        /// <summary>
        /// 過期時間（建立後 24 小時）
        /// </summary>
        public DateTime ExpiresAt { get; set; }

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
