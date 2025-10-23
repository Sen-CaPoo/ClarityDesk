using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.DTOs;

/// <summary>
/// LINE 帳號綁定 DTO
/// </summary>
public record LineBindingDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string LineUserId { get; init; } = string.Empty;
    public string LineDisplayName { get; init; } = string.Empty;
    public string? PictureUrl { get; init; }
    public BindingStatus BindingStatus { get; init; }
    public DateTime BoundAt { get; init; }
    public DateTime? LastInteractedAt { get; init; }
}

/// <summary>
/// 建立綁定請求 DTO
/// </summary>
public record CreateBindingRequest
{
    public int UserId { get; init; }
    public string LineUserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? PictureUrl { get; init; }
}
