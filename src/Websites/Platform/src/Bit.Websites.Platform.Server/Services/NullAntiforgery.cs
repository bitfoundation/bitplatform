using Microsoft.AspNetCore.Antiforgery;

namespace Bit.Websites.Platform.Server.Services;

public class NullAntiforgery : IAntiforgery
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
