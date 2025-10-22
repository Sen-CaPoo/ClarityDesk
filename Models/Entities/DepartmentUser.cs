namespace ClarityDesk.Models.Entities;

/// <summary>
/// 單位處理人員關聯實體 (單位與使用者的多對多關聯)
/// </summary>
public class DepartmentUser
{
    /// <summary>
    /// 主鍵ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 單位 ID
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// 單位 (導覽屬性)
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// 使用者 ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 使用者 (導覽屬性)
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// 指派時間
    /// </summary>
    public DateTime AssignedAt { get; set; }
}
