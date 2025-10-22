namespace ClarityDesk.Models.ViewModels;

/// <summary>
/// 單位維護 ViewModel
/// </summary>
public class DepartmentViewModel
{
    /// <summary>
    /// 單位資訊
    /// </summary>
    public DTOs.DepartmentDto Department { get; set; } = null!;

    /// <summary>
    /// 所有可用的使用者清單 (用於指派處理人員)
    /// </summary>
    public List<DTOs.UserDto> AvailableUsers { get; set; } = new();

    /// <summary>
    /// 已選擇的處理人員 ID 清單
    /// </summary>
    public List<int> SelectedUserIds { get; set; } = new();
}
