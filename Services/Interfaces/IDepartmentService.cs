using ClarityDesk.Models.DTOs;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// 單位管理服務介面
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 建立單位
    /// </summary>
    /// <param name="dto">建立單位 DTO</param>
    /// <returns>新建立的單位 ID</returns>
    Task<int> CreateDepartmentAsync(CreateDepartmentDto dto);

    /// <summary>
    /// 更新單位
    /// </summary>
    /// <param name="id">單位 ID</param>
    /// <param name="dto">更新單位 DTO</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto);

    /// <summary>
    /// 刪除單位 (軟刪除)
    /// </summary>
    /// <param name="id">單位 ID</param>
    /// <returns>是否刪除成功</returns>
    Task<bool> DeleteDepartmentAsync(int id);

    /// <summary>
    /// 取得所有單位
    /// </summary>
    /// <param name="activeOnly">是否只取得啟用的單位</param>
    /// <returns>單位清單</returns>
    Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(bool activeOnly = true);

    /// <summary>
    /// 依 ID 取得單位
    /// </summary>
    /// <param name="id">單位 ID</param>
    /// <returns>單位 DTO,若不存在則回傳 null</returns>
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id);

    /// <summary>
    /// 指派使用者到單位
    /// </summary>
    /// <param name="departmentId">單位 ID</param>
    /// <param name="userIds">使用者 ID 清單</param>
    /// <returns>是否指派成功</returns>
    Task<bool> AssignUsersToDepartmentAsync(int departmentId, List<int> userIds);

    /// <summary>
    /// 取得單位的處理人員清單
    /// </summary>
    /// <param name="departmentId">單位 ID</param>
    /// <returns>使用者清單</returns>
    Task<IEnumerable<UserDto>> GetDepartmentUsersAsync(int departmentId);
}
