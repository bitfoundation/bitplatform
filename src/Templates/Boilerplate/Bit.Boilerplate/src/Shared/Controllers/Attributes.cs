namespace Boilerplate.Shared.Controllers;

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

/// <summary>
/// Avoid retrying the request upon failure.
/// <see cref="Services.HttpMessageHandlers.RetryDelegatingHandler" />
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NoRetryPolicyAttribute : Attribute
{

}

/// <summary>
/// Ensure the authorization header is not set for the action.
/// <see cref="Services.HttpMessageHandlers.AuthDelegatingHandler" />
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NoAuthorizeHeaderPolicyAttribute : Attribute
{

}
