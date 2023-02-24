namespace AdminPanel.Shared.Exceptions;

public class RestErrorInfo
{
    public string? ExceptionType { get; set; }

    public string? Key { get; set; }

    public string? Message { get; set; }

    public ErrorResourcePayload Payload { get; set; } = new();
}
