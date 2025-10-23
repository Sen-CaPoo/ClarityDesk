using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Models.Extensions;

/// <summary>
/// LineConversationSession 實體與 DTO 之間的轉換擴充方法
/// </summary>
public static class LineConversationExtensions
{
    /// <summary>
    /// 將 LineConversationSession 實體轉換為 DTO
    /// </summary>
    public static LineConversationSessionDto ToDto(this LineConversationSession entity)
    {
        return new LineConversationSessionDto
        {
            Id = entity.Id,
            LineUserId = entity.LineUserId,
            UserId = entity.UserId,
            CurrentStep = entity.CurrentStep,
            SessionData = entity.SessionData,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ExpiresAt = entity.ExpiresAt
        };
    }
    
    /// <summary>
    /// 將 LineConversationSession 實體集合轉換為 DTO 集合
    /// </summary>
    public static IEnumerable<LineConversationSessionDto> ToDtos(this IEnumerable<LineConversationSession> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}
