using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Models.Extensions;

/// <summary>
/// Department 擴充方法
/// </summary>
public static class DepartmentExtensions
{
    /// <summary>
    /// 將 Department 實體轉換為 DepartmentDto
    /// </summary>
    public static DepartmentDto ToDto(this Department department)
    {
        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt,
            AssignedUsers = new List<UserDto>()
        };
    }

    /// <summary>
    /// 將 CreateDepartmentDto 轉換為 Department 實體
    /// </summary>
    public static Department ToEntity(this CreateDepartmentDto dto)
    {
        var now = DateTime.UtcNow;
        return new Department
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// 從 UpdateDepartmentDto 更新 Department 實體
    /// </summary>
    public static void UpdateFromDto(this Department department, UpdateDepartmentDto dto)
    {
        department.Name = dto.Name;
        department.Description = dto.Description;
        department.IsActive = dto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
    }
}
