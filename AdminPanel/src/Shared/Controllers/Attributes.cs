namespace AdminPanel.Shared.Controllers;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
internal partial class RouteAttribute(string template) : Attribute
{
    public string Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal partial class HttpGetAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal partial class HttpPostAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal partial class HttpPutAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal partial class HttpDeleteAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
internal partial class HttpPatchAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

/// <summary>
/// Avoid retrying the request upon failure.
/// <see cref="Services.HttpMessageHandlers.RetryDelegatingHandler" />
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public partial class NoRetryPolicyAttribute : Attribute
{

}
