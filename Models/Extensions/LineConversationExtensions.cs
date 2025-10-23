using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;
using System.Text.Json;

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
        var sessionData = new Dictionary<string, object>();
        
        try
        {
            if (!string.IsNullOrEmpty(entity.SessionData))
            {
                var jsonElement = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entity.SessionData);
                if (jsonElement != null)
                {
                    sessionData = jsonElement.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ValueKind switch
                        {
                            JsonValueKind.String => (object)kvp.Value.GetString()!,
                            JsonValueKind.Number => kvp.Value.GetInt32(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => kvp.Value.ToString()
                        }
                    );
                }
            }
        }
        catch
        {
            // 如果解析失敗,保持空字典
        }

        return new LineConversationSessionDto
        {
            Id = entity.Id,
            LineUserId = entity.LineUserId,
            UserId = entity.UserId,
            CurrentStep = entity.CurrentStep,
            SessionData = sessionData,
            CreatedAt = entity.CreatedAt,
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
