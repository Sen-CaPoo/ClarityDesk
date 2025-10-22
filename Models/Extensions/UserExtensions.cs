using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Models.Extensions;

/// <summary>
/// User 實體擴充方法
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// 將 User 實體轉換為 UserDto
    /// </summary>
    /// <param name="entity">User 實體</param>
    /// <returns>UserDto</returns>
    public static UserDto ToDto(this User entity)
    {
        return new UserDto
        {
            Id = entity.Id,
            LineUserId = entity.LineUserId,
            DisplayName = entity.DisplayName,
            Email = entity.Email,
            Role = entity.Role,
            IsActive = entity.IsActive,
            PictureUrl = entity.PictureUrl,
            CreatedAt = entity.CreatedAt
        };
    }
}
