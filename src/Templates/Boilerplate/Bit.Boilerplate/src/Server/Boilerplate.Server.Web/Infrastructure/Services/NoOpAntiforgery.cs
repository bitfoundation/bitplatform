using Microsoft.AspNetCore.Antiforgery;

namespace Boilerplate.Server.Web.Infrastructure.Services;

/// <summary>
/// Disables the default anti-forgery system to allow CDN caching.
/// <para>
/// By default, the anti-forgery mechanism generates a validation cookie and sends a <c>Set-Cookie</c> header
/// for <strong>EVERY</strong> request, even for <strong>Anonymous (unauthenticated)</strong> users.
/// </para>
/// <para>
/// This presence of the <c>Set-Cookie</c> header forces CDNs to treat the response as private/dynamic,
/// effectively <strong>disabling caching completely</strong> for pre-rendered public pages and APIs.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// <strong>Security Baseline:</strong>
/// This project template enforces <c>HttpOnly</c> and <c>SameSite=Strict</c> for authentication cookies.
/// This strict configuration inherently protects against CSRF attacks.
/// </para>
/// <para>
/// <strong>Deployment Scenarios:</strong>
/// <list type="bullet">
///     <item>
///         <strong>Separate Standalone Deployment:</strong> If <c>Server.Web</c> and <c>Server.Api</c> are deployed separately, 
///         this configuration is safe as it only affects the Server.Web project, leaving the API's security mechanisms intact.
///     </item>
///     <item>
///         <strong>Integrated Deployment:</strong> If the Server.Web and Server.Api are hosted within the same ASP.NET Core application, you must:
///         <list type="number">
///             <item>Ensure authentication cookies remain <c>SameSite=Strict</c>.</item>
///             <item>Enforce <c>application/json</c> content-type on API endpoints (blocking HTML form submissions).</item>
///             <item>OR remove this class to restore default anti-forgery behavior, accepting that CDN caching becomes ineffective for dynamic APIs and pre-rendered pages.</item>
///         </list>
///     </item>
/// </list>
/// </para>
/// </remarks>
public class NoOpAntiforgery : IAntiforgery
{
    private const string AntiforgeryTokenFieldName = "__RequestVerificationToken";
    private const string AntiforgeryTokenHeaderName = "RequestVerificationToken";

    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => new(string.Empty, string.Empty, AntiforgeryTokenFieldName, AntiforgeryTokenHeaderName);

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => new(string.Empty, string.Empty, AntiforgeryTokenFieldName, AntiforgeryTokenHeaderName);

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
        return;
    }

    public Task ValidateRequestAsync(HttpContext httpContext) => Task.FromResult(true);
}
