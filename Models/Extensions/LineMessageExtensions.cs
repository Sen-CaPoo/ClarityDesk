using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Models.Extensions;

/// <summary>
/// LineMessageLog 實體與 DTO 之間的轉換擴充方法
/// </summary>
public static class LineMessageExtensions
{
    /// <summary>
    /// 將 LineMessageLog 實體轉換為 DTO
    /// </summary>
    public static LineMessageLogDto ToDto(this LineMessageLog entity)
    {
        return new LineMessageLogDto
        {
            Id = entity.Id,
            LineUserId = entity.LineUserId,
            MessageType = entity.MessageType,
            Direction = entity.Direction,
            Content = entity.Content,
            IsSuccess = entity.IsSuccess,
            ErrorCode = entity.ErrorCode,
            ErrorMessage = entity.ErrorMessage,
            SentAt = entity.SentAt,
            IssueReportId = entity.IssueReportId
        };
    }
    
    /// <summary>
    /// 將 LineMessageLog 實體集合轉換為 DTO 集合
    /// </summary>
    public static IEnumerable<LineMessageLogDto> ToDtos(this IEnumerable<LineMessageLog> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}
