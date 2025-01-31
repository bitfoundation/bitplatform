namespace Boilerplate.Shared.Exceptions;

public partial class ErrorResourcePayload
{
    public List<PropertyErrorResourceCollection> Details { get; set; } = [];
}

public partial class PropertyErrorResourceCollection
{
    public string? Name { get; set; } = "*";

    public List<ErrorResource> Errors { get; set; } = [];
}

public partial class ErrorResource
{
    public string? Key { get; set; }

    public string? Message { get; set; }
}
