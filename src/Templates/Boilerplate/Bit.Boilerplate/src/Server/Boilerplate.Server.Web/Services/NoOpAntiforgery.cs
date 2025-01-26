using Microsoft.AspNetCore.Antiforgery;

namespace Boilerplate.Server.Web.Services;

/// <summary>
/// The anti-forgery cookie can interfere with ASP.NET Core's output caching mechanisms.
/// This occurs because the cookie is tied to a user's session, causing the cache to be
/// considered specific to the individual user, thereby preventing the caching of
/// shared content that can be reused across multiple users.
/// </summary>
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
