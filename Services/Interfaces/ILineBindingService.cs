using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;

namespace ClarityDesk.Services.Interfaces;

/// <summary>
/// LINE 帳號綁定服務介面
/// 管理 ClarityDesk 使用者與 LINE 帳號的綁定關係
/// </summary>
public interface ILineBindingService
{
    /// <summary>
    /// 建立或更新使用者的 LINE 綁定關係
    /// </summary>
    /// <param name="userId">ClarityDesk 使用者 ID</param>
    /// <param name="lineUserId">LINE User ID</param>
    /// <param name="displayName">LINE 顯示名稱</param>
    /// <param name="pictureUrl">LINE 頭像 URL (可選)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>綁定記錄 ID</returns>
    /// <exception cref="Exceptions.LineBindingException">當 LINE User ID 已被其他帳號綁定時拋出</exception>
    /// <exception cref="InvalidOperationException">當使用者為訪客帳號時拋出</exception>
    Task<int> CreateOrUpdateBindingAsync(
        int userId,
        string lineUserId,
        string displayName,
        string? pictureUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得使用者的 LINE 綁定資訊
    /// </summary>
    /// <param name="userId">ClarityDesk 使用者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>綁定資訊 DTO,若未綁定則回傳 null</returns>
    Task<LineBindingDto?> GetBindingByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 依 LINE User ID 查詢綁定資訊
    /// </summary>
    /// <param name="lineUserId">LINE User ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>綁定資訊 DTO,若未找到則回傳 null</returns>
    Task<LineBindingDto?> GetBindingByLineUserIdAsync(
        string lineUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 檢查使用者是否已綁定 LINE 帳號
    /// </summary>
    /// <param name="userId">ClarityDesk 使用者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true: 已綁定且狀態為 Active; false: 未綁定或狀態非 Active</returns>
    Task<bool> IsUserBoundAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 解除使用者的 LINE 綁定
    /// </summary>
    /// <param name="userId">ClarityDesk 使用者 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true: 成功解除; false: 使用者未綁定</returns>
    Task<bool> UnbindAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 標記綁定狀態為「已封鎖」(使用者封鎖 LINE 官方帳號)
    /// </summary>
    /// <param name="lineUserId">LINE User ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkAsBlockedAsync(
        string lineUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 標記綁定狀態為「正常」(使用者解除封鎖或重新互動)
    /// </summary>
    /// <param name="lineUserId">LINE User ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MarkAsActiveAsync(
        string lineUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新最後互動時間
    /// </summary>
    /// <param name="lineUserId">LINE User ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateLastInteractionAsync(
        string lineUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得所有已綁定的使用者列表 (管理員功能)
    /// </summary>
    /// <param name="status">篩選狀態 (可選,null 表示全部)</param>
    /// <param name="pageNumber">頁碼 (從 1 開始)</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <param name="searchKeyword">搜尋關鍵字 (使用者名稱或 LINE 顯示名稱)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分頁結果</returns>
    Task<PagedResult<LineBindingDto>> GetAllBindingsAsync(
        BindingStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? searchKeyword = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 依綁定 ID 解除綁定 (管理員功能)
    /// </summary>
    /// <param name="bindingId">綁定記錄 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true: 成功解除; false: 綁定不存在</returns>
    Task<bool> UnbindByIdAsync(
        int bindingId,
        CancellationToken cancellationToken = default);
}
