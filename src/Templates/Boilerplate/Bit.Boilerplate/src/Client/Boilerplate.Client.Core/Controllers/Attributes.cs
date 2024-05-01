namespace Boilerplate.Client.Core.Controllers;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
internal class RouteAttribute(string template) : Attribute
{
    public string Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal class HttpGetAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal class HttpPostAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal class HttpPutAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal class HttpDeleteAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal class HttpPatchAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}
