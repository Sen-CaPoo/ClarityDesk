using ClarityDesk.Models.DTOs;
using ClarityDesk.Models.Enums;

namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE 對話管理服務介面
    /// </summary>
    public interface ILineConversationService
    {
        /// <summary>
        /// 開始新的回報問題對話流程
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="userId">ClarityDesk 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Session ID</returns>
        /// <exception cref="InvalidOperationException">當使用者已有進行中的 Session 時拋出</exception>
        Task<Guid> StartConversationAsync(
            string lineUserId,
            int userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取得使用者當前的對話 Session
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>Session DTO,若無進行中 Session 則回傳 null</returns>
        Task<LineConversationSessionDto?> GetActiveSessionAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理使用者輸入並推進對話流程
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="userInput">使用者輸入的文字</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>對話回應 (包含下一步提示與驗證結果)</returns>
        Task<ConversationResponse> ProcessUserInputAsync(
            string lineUserId,
            string userInput,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新 Session 的暫存資料
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="fieldName">欄位名稱 (例如 "title", "departmentId")</param>
        /// <param name="value">欄位值</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task UpdateSessionDataAsync(
            Guid sessionId,
            string fieldName,
            object value,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 推進對話至下一步驟
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="nextStep">下一步驟</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task AdvanceToNextStepAsync(
            Guid sessionId,
            ConversationStep nextStep,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 完成對話流程並建立回報單
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>建立的回報單 ID</returns>
        Task<int> CompleteConversationAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消對話流程並清除 Session
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task CancelConversationAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 驗證使用者輸入的資料格式
        /// </summary>
        /// <param name="step">當前步驟</param>
        /// <param name="input">使用者輸入</param>
        /// <returns>驗證結果</returns>
        ValidationResult ValidateInput(ConversationStep step, string input);

        /// <summary>
        /// 清理過期的 Session (由背景服務呼叫)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理的 Session 數量</returns>
        Task<int> CleanupExpiredSessionsAsync(
            CancellationToken cancellationToken = default);
    }
}
