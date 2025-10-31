namespace ClarityDesk.Models.DTOs
{
    /// <summary>
    /// LINE Webhook 事件訊息 DTO
    /// 用於解析 LINE Webhook 傳來的事件資料
    /// </summary>
    public class LineMessageDto
    {
        public string Type { get; set; } = string.Empty;
        public string ReplyToken { get; set; } = string.Empty;
        public LineSourceDto? Source { get; set; }
        public LineMessageContent? Message { get; set; }
        public LinePostbackData? Postback { get; set; }
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// LINE 訊息來源
    /// </summary>
    public class LineSourceDto
    {
        public string Type { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    /// <summary>
    /// LINE 訊息內容
    /// </summary>
    public class LineMessageContent
    {
        public string Type { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string? Text { get; set; }
    }

    /// <summary>
    /// LINE Postback 資料
    /// </summary>
    public class LinePostbackData
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// LINE Webhook 請求
    /// </summary>
    public class LineWebhookRequest
    {
        public string Destination { get; set; } = string.Empty;
        public List<LineMessageDto> Events { get; set; } = new();
    }
}
