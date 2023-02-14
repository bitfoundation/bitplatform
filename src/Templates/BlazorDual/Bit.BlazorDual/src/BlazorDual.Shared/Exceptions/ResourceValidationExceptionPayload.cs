namespace BlazorDual.Shared.Exceptions;

public class ResourceValidationExceptionPayload
{
    public string? ResourceTypeName { get; set; } = "*";

    public string? Property { get; set; } = "*";

    public IEnumerable<ResourceValidationExceptionPayloadError> Errors { get; set; } = Array.Empty<ResourceValidationExceptionPayloadError>();
}

public class ResourceValidationExceptionPayloadError
{
    public string? Key { get; set; }

    public string? Message { get; set; }
}
