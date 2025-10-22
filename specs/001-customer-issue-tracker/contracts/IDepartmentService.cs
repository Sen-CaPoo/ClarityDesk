using ClarityDesk.Models.DTOs;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 問題所屬單位服務介面
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 建立新單位
    /// </summary>
    /// <param name="dto">單位建立資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>建立的單位 ID</returns>
    Task<int> CreateDepartmentAsync(CreateDepartmentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新單位資訊
    /// </summary>
    /// <param name="id">單位 ID</param>
    /// <param name="dto">單位更新資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 軟刪除單位 (標記為已停用)
    /// </summary>
    /// <param name="id">單位 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否刪除成功</returns>
    Task<bool> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得所有單位列表
    /// </summary>
    /// <param name="activeOnly">是否只取得啟用的單位</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>單位列表</returns>
    Task<List<DepartmentDto>> GetAllDepartmentsAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 ID 取得單位詳細資料
    /// </summary>
    /// <param name="id">單位 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>單位詳細資料</returns>
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 為單位指派預設處理人員
    /// </summary>
    /// <param name="departmentId">單位 ID</param>
    /// <param name="userIds">使用者 ID 列表</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否指派成功</returns>
    Task<bool> AssignUsersTooltipDepartmentAsync(int departmentId, List<int> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得單位的預設處理人員列表
    /// </summary>
    /// <param name="departmentId">單位 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>使用者列表</returns>
    Task<List<UserDto>> GetDepartmentUsersAsync(int departmentId, CancellationToken cancellationToken = default);
}
