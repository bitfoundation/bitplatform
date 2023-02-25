namespace BlazorDual.Shared.Exceptions;

public class ErrorResourcePayload
{
    public string? ResourceTypeName { get; set; } = "*";

    public List<PropertyErrorResourceCollection> Details { get; set; } = new();
}

public class PropertyErrorResourceCollection
{
    public string? Name { get; set; } = "*";

    public List<ErrorResource> Errors { get; set; } = new();
}

public class ErrorResource
{
    public string? Key { get; set; }

    public string? Message { get; set; }
}
