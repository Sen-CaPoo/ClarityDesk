using ClarityDesk.Models.DTOs;

namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE Messaging 服務介面
    /// 提供 LINE Push Message、Webhook 處理和對話管理功能
    /// </summary>
    public interface ILineMessagingService
    {
        /// <summary>
        /// 發送 LINE Push Message
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="message">訊息內容（物件將序列化為 JSON）</param>
        /// <returns>是否成功發送</returns>
        Task<bool> PushMessageAsync(string lineUserId, object message);

        /// <summary>
        /// 推送新問題回報單通知（Flex Message 格式）
        /// </summary>
        /// <param name="issue">問題回報單 DTO</param>
        /// <param name="targetLineUserId">目標 LINE 使用者 ID</param>
        /// <returns>是否成功推送</returns>
        Task<bool> PushNewIssueNotificationAsync(IssueReportDto issue, string targetLineUserId);

        /// <summary>
        /// 推送問題狀態變更通知
        /// </summary>
        /// <param name="issue">問題回報單 DTO</param>
        /// <param name="oldStatus">舊狀態</param>
        /// <param name="newStatus">新狀態</param>
        /// <param name="targetLineUserId">目標 LINE 使用者 ID</param>
        /// <returns>是否成功推送</returns>
        Task<bool> PushStatusChangedNotificationAsync(IssueReportDto issue, string oldStatus, string newStatus, string targetLineUserId);

        /// <summary>
        /// 推送問題指派變更通知（Flex Message 格式）
        /// </summary>
        /// <param name="issue">問題回報單 DTO</param>
        /// <param name="newAssigneeName">新指派人員名稱</param>
        /// <param name="targetLineUserId">目標 LINE 使用者 ID</param>
        /// <returns>是否成功推送</returns>
        Task<bool> PushAssignmentChangedNotificationAsync(IssueReportDto issue, string newAssigneeName, string targetLineUserId);

        /// <summary>
        /// 處理 LINE Webhook 事件
        /// </summary>
        /// <param name="webhookRequest">Webhook 請求資料</param>
        /// <returns>處理結果</returns>
        Task HandleWebhookEventAsync(LineWebhookRequest webhookRequest);

        /// <summary>
        /// 回覆 LINE 訊息
        /// </summary>
        /// <param name="replyToken">Reply Token</param>
        /// <param name="message">訊息內容</param>
        /// <returns>是否成功回覆</returns>
        Task<bool> ReplyMessageAsync(string replyToken, object message);
    }
}
