using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// LINE 推送記錄 DTO
    /// </summary>
    public class LinePushLogDto
    {
        public int Id { get; set; }
        public int IssueReportId { get; set; }
        public string LineUserId { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public LinePushStatus Status { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime PushedAt { get; set; }
    }
}
