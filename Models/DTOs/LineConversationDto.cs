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
    public string SessionData { get; init; } = "{}";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// 對話回應 DTO (包含訊息文字與快速回覆選項)
/// </summary>
public record ConversationResponse
{
    public string MessageText { get; init; } = string.Empty;
    public IEnumerable<QuickReplyOption>? QuickReplyOptions { get; init; }
    public ConversationStep NextStep { get; init; }
    public bool IsComplete { get; init; }
    public int? CreatedIssueId { get; init; }
}

/// <summary>
/// 驗證結果 DTO
/// </summary>
public record ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static ValidationResult Valid() => new() { IsValid = true };
    public static ValidationResult Invalid(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
}

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
