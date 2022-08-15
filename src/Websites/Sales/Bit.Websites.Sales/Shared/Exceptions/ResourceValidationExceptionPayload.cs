namespace Bit.Websites.Sales.Shared.Exceptions;

public class ResourceValidationExceptionPayload
{
    public string? ResourceTypeName { get; set; } = "*";

    public string? Property { get; set; } = "*";

    public IEnumerable<string> Messages { get; set; } = Array.Empty<string>();
}
