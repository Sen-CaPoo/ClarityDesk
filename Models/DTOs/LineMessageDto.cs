using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// LINE 訊息日誌 DTO
/// </summary>
public record LineMessageLogDto
{
    public Guid Id { get; init; }
    public string LineUserId { get; init; } = string.Empty;
    public string? LineDisplayName { get; init; }
    public LineMessageType MessageType { get; init; }
    public MessageDirection Direction { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAt { get; init; }
    public int? IssueReportId { get; init; }
}

/// <summary>
/// 快速回覆選項 (用於 LINE 訊息)
/// </summary>
public record QuickReplyOption
{
    public string Label { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
}

/// <summary>
/// 發送訊息請求 DTO
/// </summary>
public record SendMessageRequest
{
    public string LineUserId { get; init; } = string.Empty;
    public string MessageText { get; init; } = string.Empty;
    public IEnumerable<QuickReplyOption>? QuickReplyOptions { get; init; }
}

/// <summary>
/// 發送訊息回應 DTO
/// </summary>
public record SendMessageResponse
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? LogId { get; init; }
}
