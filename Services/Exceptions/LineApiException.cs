namespace ClarityDesk.Services.Exceptions;

/// <summary>
/// LINE Messaging API 呼叫失敗時拋出的例外
/// </summary>
public class LineApiException : Exception
{
    /// <summary>
    /// LINE API 回應的 HTTP 狀態碼
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// LINE API 回應的錯誤代碼
    /// </summary>
    public string? ErrorCode { get; set; }

    public LineApiException()
    {
    }

    public LineApiException(string message) : base(message)
    {
    }

    public LineApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
    public LineApiException(string message, int statusCode, string? errorCode = null) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
