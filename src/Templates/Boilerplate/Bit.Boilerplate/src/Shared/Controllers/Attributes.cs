namespace Boilerplate.Shared.Controllers;

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
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
public partial class NoRetryPolicyAttribute : Attribute
{

}

/// <summary>
/// This `optional` attribute enables `client-side` validation of access token expiry before making any requests to the server. 
/// By checking the token's validity locally, unnecessary server requests that would result in a 401 response are avoided, 
/// and the refresh token can be invoked immediately if the access token is expired. 
/// Since access tokens are intentionally set to expire frequently for security and other benefits, this scenario is expected to occur often. 
/// Performing this validation on the client improves overall application performance by reducing redundant network calls. 
/// Note: This attribute requires the client’s date and time settings to be accurate, as incorrect settings may cause validation issues.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
public partial class AuthorizedApiAttribute : Attribute
{

}
