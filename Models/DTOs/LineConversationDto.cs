using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// LINE 對話 Session DTO
/// </summary>
public record LineConversationSessionDto
{
    public Guid Id { get; init; }
    public string LineUserId { get; init; } = string.Empty;
    public int UserId { get; init; }
    public ConversationStep CurrentStep { get; init; }
    public Dictionary<string, object> SessionData { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// 對話回應 DTO (包含訊息文字與快速回覆選項)
/// </summary>
public record ConversationResponse
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public ConversationStep? NextStep { get; init; }
    public IEnumerable<QuickReplyOption>? QuickReplyOptions { get; init; }
}

/// <summary>
/// 驗證結果 DTO
/// </summary>
public record ValidationResult(bool IsValid, string? ErrorMessage = null);

/// <summary>
/// Session 資料結構 (對應 SessionData JSON)
/// </summary>
public record SessionData
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public int? DepartmentId { get; init; }
    public string? Urgency { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
}
