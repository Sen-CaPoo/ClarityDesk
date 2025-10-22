using ClarityDesk.Models.Enums;

namespace ClarityDesk.Models.Entities
{
    /// <summary>
    /// 使用者實體
    /// </summary>
    public class User
    {
        /// <summary>
        /// 主鍵ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// LINE User ID (唯一識別碼)
        /// </summary>
        public string LineUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// 顯示名稱
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// 電子信箱
        /// </summary>
        public string? Email { get; set; }
        
        /// <summary>
        /// 權限角色
        /// </summary>
        public UserRole Role { get; set; }
        
        /// <summary>
        /// 帳號狀態
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// LINE 頭像 URL
        /// </summary>
        public string? PictureUrl { get; set; }
        
        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
