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

/// <summary>
/// Avoid retrying the request upon failure.
/// <see cref="Services.HttpMessageHandlers.RetryDelegatingHandler" />
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class NoRetryPolicyAttribute : Attribute
{

}

/// <summary>
/// Ensure the authorization header is not set for the action.
/// <see cref="Services.HttpMessageHandlers.AuthDelegatingHandler" />
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class NoAuthorizeHeaderPolicyAttribute : Attribute
{

}

/// <summary>
/// The generated HTTP client proxy by Bit.SourceGenerators will automatically include these request options in the constructed HttpRequestMessage.
/// You can access these values within HTTP message handlers, such as  <see cref="Services.HttpMessageHandlers.AuthDelegatingHandler"/>.
/// </summary>
public class RequestOptionNames
{
    public const string IControllerTypeName = nameof(IControllerTypeName);
    public const string ActionName = nameof(ActionName);
    public const string ActionParametersInfo = nameof(ActionParametersInfo);
    public const string RequestTypeName = nameof(RequestTypeName);
    public const string ResponseTypeName = nameof(ResponseTypeName);
}
