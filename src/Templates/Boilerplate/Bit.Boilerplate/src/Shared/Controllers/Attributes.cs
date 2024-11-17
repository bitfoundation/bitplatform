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
/// This attribute enforces client-side validation of access tokens expiry to optimize application performance.
/// By checking token expiration locally, we can avoid unnecessary server requests that enhances overall response times and streamlines the refresh token process.
/// Note: This requires the client's date and time settings to be accurate, as incorrect settings may cause validation to fail.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
public partial class AuthorizedApiAttribute : Attribute
{

}
