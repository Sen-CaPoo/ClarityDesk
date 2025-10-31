namespace ClarityDesk.Models.Enums
{
    /// <summary>
    /// LINE 對話步驟
    /// </summary>
    public enum ConversationStep
    {
        /// <summary>
        /// 詢問問題標題
        /// </summary>
        AskTitle,

        /// <summary>
        /// 詢問問題內容
        /// </summary>
        AskContent,

        /// <summary>
        /// 詢問所屬單位
        /// </summary>
        AskDepartment,

        /// <summary>
        /// 詢問緊急程度
        /// </summary>
        AskPriority,

        /// <summary>
        /// 詢問聯絡人姓名
        /// </summary>
        AskCustomerName,

        /// <summary>
        /// 詢問聯絡電話
        /// </summary>
        AskCustomerPhone,

        /// <summary>
        /// 詢問是否上傳圖片
        /// </summary>
        AskImages,

        /// <summary>
        /// 確認摘要
        /// </summary>
        Confirm,

        /// <summary>
        /// 完成（等待清理）
        /// </summary>
        Completed
    }
}
