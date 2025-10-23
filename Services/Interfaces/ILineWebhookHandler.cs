namespace ClarityDesk.Services.Interfaces
{
    /// <summary>
    /// LINE Webhook 事件處理介面
    /// </summary>
    public interface ILineWebhookHandler
    {
        /// <summary>
        /// 處理 Webhook 事件 (主要入口方法)
        /// </summary>
        /// <param name="webhookPayload">LINE Platform 傳送的 JSON Payload</param>
        /// <param name="signature">X-Line-Signature 標頭值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>HTTP 狀態碼 (200: 成功, 401: 簽章驗證失敗, 500: 處理錯誤)</returns>
        Task<int> HandleWebhookAsync(
            string webhookPayload,
            string signature,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 驗證 Webhook 請求的簽章
        /// </summary>
        /// <param name="payload">請求 Body (原始字串)</param>
        /// <param name="signature">X-Line-Signature 標頭值</param>
        /// <returns>true: 簽章有效; false: 簽章無效</returns>
        bool ValidateSignature(string payload, string signature);

        /// <summary>
        /// 處理 Follow 事件 (使用者加入好友)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="replyToken">回覆令牌</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandleFollowEventAsync(
            string lineUserId,
            string replyToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理 Unfollow 事件 (使用者封鎖或刪除好友)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandleUnfollowEventAsync(
            string lineUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理 Message 事件 (使用者傳送訊息)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="messageText">訊息內容</param>
        /// <param name="replyToken">回覆令牌</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandleMessageEventAsync(
            string lineUserId,
            string messageText,
            string replyToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 處理 Postback 事件 (使用者點擊按鈕)
        /// </summary>
        /// <param name="lineUserId">LINE 使用者 ID</param>
        /// <param name="postbackData">按鈕攜帶的資料</param>
        /// <param name="replyToken">回覆令牌</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HandlePostbackEventAsync(
            string lineUserId,
            string postbackData,
            string replyToken,
            CancellationToken cancellationToken = default);
    }
}
