namespace ClarityDesk.Services.Exceptions;

/// <summary>
/// LINE 帳號綁定相關的業務邏輯例外
/// </summary>
public class LineBindingException : Exception
{
    public LineBindingException()
    {
    }

    public LineBindingException(string message) : base(message)
    {
    }

    public LineBindingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
