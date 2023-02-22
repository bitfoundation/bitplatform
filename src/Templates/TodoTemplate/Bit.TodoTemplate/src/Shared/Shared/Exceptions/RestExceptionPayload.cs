namespace TodoTemplate.Shared.Exceptions;

public class RestExceptionPayload
{
    public string? ExceptionType { get; set; }

    public string? Key { get; set; }

    public string? Message { get; set; }

    public ResourceValidationExceptionPayload Payload { get; set; } = new();
}
