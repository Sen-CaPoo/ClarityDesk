using ClarityDesk.Models.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE 訊息發送服務介面
    /// </summary>
    public interface ILineMessagingService
    {
        /// <summary>
        /// 發送回報單通知給指派的處理人員
        /// </summary>
        /// <param name="lineUserId">目標 LINE 使用者 ID</param>
        /// <param name="issueReport">回報單資料 DTO</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 發送成功; false: 發送失敗 (會記錄日誌)</returns>
        Task<bool> SendIssueNotificationAsync(
            string lineUserId,
            IssueReportDto issueReport,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 回覆使用者的訊息 (Reply API,不計入配額)
        /// </summary>
        /// <param name="replyToken">LINE Platform 提供的回覆令牌</param>
        /// <param name="messages">訊息內容 (最多 5 則)</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 回覆成功; false: 回覆失敗</returns>
        Task<bool> ReplyMessageAsync(
            string replyToken,
            IEnumerable<string> messages,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 主動推送文字訊息給使用者
        /// </summary>
        /// <param name="lineUserId">目標 LINE 使用者 ID</param>
        /// <param name="message">訊息內容</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 發送成功; false: 發送失敗</returns>
        Task<bool> PushTextMessageAsync(
            string lineUserId,
            string message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 建構回報單通知的 Flex Message JSON
        /// </summary>
        /// <param name="issueReport">回報單資料 DTO</param>
        /// <returns>Flex Message JSON 字串</returns>
        string BuildIssueNotificationFlexMessage(IssueReportDto issueReport);

        /// <summary>
        /// 建構對話流程的快速回覆按鈕
        /// </summary>
        /// <param name="options">選項列表 (例如單位名稱、緊急程度)</param>
        /// <param name="maxOptions">最大選項數 (LINE 限制 13 個)</param>
        /// <returns>Quick Reply JSON</returns>
        string BuildQuickReplyButtons(
            IEnumerable<QuickReplyOption> options,
            int maxOptions = 13);

        /// <summary>
        /// 檢查是否可發送推送訊息 (配額檢查)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 可發送; false: 已達配額限制</returns>
        Task<bool> CanSendPushMessageAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 記錄訊息發送日誌
        /// </summary>
        /// <param name="log">訊息日誌 DTO</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task LogMessageAsync(
            LineMessageLogDto log,
            CancellationToken cancellationToken = default);
    }
}
