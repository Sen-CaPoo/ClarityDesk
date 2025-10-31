using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.Entities
{
    /// <summary>
    /// LINE 推送記錄
    /// 記錄每次 LINE Push Message 推送的詳細資訊，用於追蹤和除錯
    /// </summary>
    public class LinePushLog
    {
        /// <summary>
        /// 主鍵
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 關聯的問題回報單 ID
        /// </summary>
        public int IssueReportId { get; set; }

        /// <summary>
        /// 推送目標 LINE 使用者 ID
        /// </summary>
        public string LineUserId { get; set; } = string.Empty;

        /// <summary>
        /// 訊息類型（NewIssue, StatusChanged, AssignmentChanged）
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        /// <summary>
        /// 推送狀態
        /// </summary>
        public LinePushStatus Status { get; set; }

        /// <summary>
        /// 重試次數
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 錯誤訊息（推送失敗時記錄）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 推送時間
        /// </summary>
        public DateTime PushedAt { get; set; }

        /// <summary>
        /// 建立時間（自動管理）
        /// </summary>
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        /// <summary>
        /// 關聯的問題回報單
        /// </summary>
        public IssueReport? IssueReport { get; set; }
    }
}
