namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// 使用者權限角色
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// 普通使用者 (可建立、編輯、刪除回報單)
        /// </summary>
        User,
        
        /// <summary>
        /// 管理人員 (擁有所有權限 + 系統管理功能)
        /// </summary>
        Admin
    }
}
