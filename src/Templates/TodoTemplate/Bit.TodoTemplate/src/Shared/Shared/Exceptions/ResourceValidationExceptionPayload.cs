namespace TodoTemplate.Shared.Exceptions;

public class ResourceValidationExceptionPayload
{
    public string? ResourceTypeName { get; set; } = "*";

    public List<ResourceValidationExceptionPropertyErrors> Details { get; set; } = new();
}

public class ResourceValidationExceptionPropertyErrors
{
    public string? Property { get; set; } = "*";

    public List<ResourceValidationExceptionPropertyError> Messages { get; set; } = new();
}

public class ResourceValidationExceptionPropertyError
{
    public string? Key { get; set; }

    public string? Message { get; set; }
}
