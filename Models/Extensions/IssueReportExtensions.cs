using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Entities;

namespace ClarityDesk.Models.Extensions;

/// <summary>
/// IssueReport 實體的擴充方法，用於資料映射
/// </summary>
public static class IssueReportExtensions
{
    /// <summary>
    /// 將 IssueReport 實體轉換為 IssueReportDto
    /// </summary>
    /// <param name="entity">IssueReport 實體</param>
    /// <returns>IssueReportDto</returns>
    public static IssueReportDto ToDto(this IssueReport entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new IssueReportDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Content = entity.Content,
            RecordDate = entity.RecordDate,
            Status = entity.Status,
            Priority = entity.Priority,
            ReporterName = entity.ReporterName,
            CustomerName = entity.CustomerName,
            CustomerPhone = entity.CustomerPhone,
            AssignedUserId = entity.AssignedUserId,
            AssignedUserName = entity.AssignedUser?.DisplayName ?? "",
            DepartmentNames = entity.DepartmentAssignments?
                .Select(da => da.Department?.Name ?? "")
                .ToList() ?? new List<string>(),
            DepartmentIds = entity.DepartmentAssignments?
                .Select(da => da.DepartmentId)
                .ToList() ?? new List<int>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            LastModifiedByUserId = entity.LastModifiedByUserId,
            LastModifiedByUserName = entity.LastModifiedBy?.DisplayName
        };
    }

    /// <summary>
    /// 將 CreateIssueReportDto 轉換為 IssueReport 實體
    /// </summary>
    /// <param name="dto">CreateIssueReportDto</param>
    /// <returns>IssueReport 實體</returns>
    public static IssueReport ToEntity(this CreateIssueReportDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new IssueReport
        {
            Title = dto.Title,
            Content = dto.Content,
            RecordDate = dto.RecordDate,
            Status = dto.Status,
            Priority = dto.Priority,
            ReporterName = dto.ReporterName,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            AssignedUserId = dto.AssignedUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 使用 UpdateIssueReportDto 更新 IssueReport 實體
    /// </summary>
    /// <param name="entity">要更新的 IssueReport 實體</param>
    /// <param name="dto">UpdateIssueReportDto</param>
    /// <returns>是否有實際變更</returns>
    public static bool UpdateFromDto(this IssueReport entity, UpdateIssueReportDto dto)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        bool hasChanges = false;

        if (entity.Title != dto.Title)
        {
            entity.Title = dto.Title;
            hasChanges = true;
        }

        if (entity.Content != dto.Content)
        {
            entity.Content = dto.Content;
            hasChanges = true;
        }

        if (entity.RecordDate != dto.RecordDate)
        {
            entity.RecordDate = dto.RecordDate;
            hasChanges = true;
        }

        if (entity.Status != dto.Status)
        {
            entity.Status = dto.Status;
            hasChanges = true;
        }

        if (entity.Priority != dto.Priority)
        {
            entity.Priority = dto.Priority;
            hasChanges = true;
        }

        if (entity.ReporterName != dto.ReporterName)
        {
            entity.ReporterName = dto.ReporterName;
            hasChanges = true;
        }

        if (entity.CustomerName != dto.CustomerName)
        {
            entity.CustomerName = dto.CustomerName;
            hasChanges = true;
        }

        if (entity.CustomerPhone != dto.CustomerPhone)
        {
            entity.CustomerPhone = dto.CustomerPhone;
            hasChanges = true;
        }

        if (entity.AssignedUserId != dto.AssignedUserId)
        {
            entity.AssignedUserId = dto.AssignedUserId;
            hasChanges = true;
        }

        // 只有在有實際變更時才更新 UpdatedAt
        if (hasChanges)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }

        return hasChanges;
    }
}
