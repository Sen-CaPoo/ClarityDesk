namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// LINE 推送訊息狀態
    /// </summary>
    public enum LinePushStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success,

        /// <summary>
        /// 失敗
        /// </summary>
        Failed,

        /// <summary>
        /// 重試中
        /// </summary>
        Retrying
    }
}
