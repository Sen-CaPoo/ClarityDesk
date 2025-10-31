using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// LINE 對話狀態 DTO
    /// </summary>
    public class LineConversationStateDto
    {
        public int Id { get; set; }
        public string LineUserId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public ConversationStep CurrentStep { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public int? DepartmentId { get; set; }
        public string? Priority { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public List<string>? ImageUrls { get; set; }  // 從 JSON 字串反序列化
        public DateTime ExpiresAt { get; set; }
    }
}
