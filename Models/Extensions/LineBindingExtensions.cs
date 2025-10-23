using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Models.Extensions;

/// <summary>
/// LineBinding 實體與 DTO 之間的轉換擴充方法
/// </summary>
public static class LineBindingExtensions
{
    /// <summary>
    /// 將 LineBinding 實體轉換為 DTO
    /// </summary>
    public static LineBindingDto ToDto(this LineBinding entity)
    {
        return new LineBindingDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            DisplayName = entity.User?.DisplayName ?? string.Empty,
            LineUserId = entity.LineUserId,
            LineDisplayName = entity.DisplayName,
            PictureUrl = entity.PictureUrl,
            BindingStatus = entity.BindingStatus,
            BoundAt = entity.BoundAt,
            LastInteractedAt = entity.LastInteractedAt
        };
    }
    
    /// <summary>
    /// 將 CreateBindingRequest 轉換為 LineBinding 實體
    /// </summary>
    public static LineBinding ToEntity(this CreateBindingRequest request)
    {
        var now = DateTime.UtcNow;
        return new LineBinding
        {
            UserId = request.UserId,
            LineUserId = request.LineUserId,
            DisplayName = request.DisplayName,
            PictureUrl = request.PictureUrl,
            BindingStatus = Enums.BindingStatus.Active,
            BoundAt = now,
            LastInteractedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
