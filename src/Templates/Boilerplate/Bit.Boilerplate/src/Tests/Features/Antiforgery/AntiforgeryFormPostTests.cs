//+:cnd:noEmit
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;

namespace Boilerplate.Tests.Features.Antiforgery;

/// <summary>
/// The CSRF contract for an <b>anonymous form post</b> - the shape a cross-site attack actually takes, since a
/// cross-site page can send a form with the visitor's cookies but can add neither an Authorization header nor a JSON
/// content type without a CORS preflight. <c>AutoCsrfProtectionFilter</c> lets the first two through and demands an
/// antiforgery token from everything else; these two tests pin both sides of that decision.
/// <para>
/// The endpoint is chosen, not incidental. The filter runs as an action filter, so it is reached only after
/// authorization and model binding: every endpoint that binds a DTO answers a form body with 415 before the filter
/// sees it, and every endpoint that does accept a form (the <c>IFormFile</c> uploads) is <c>[Authorize]</c>.
/// <c>SendTestPushNotification</c> is the anonymous POST that binds nothing but a route value, so a form body reaches
/// the filter intact - and an unknown device id makes it return false without sending anything.
/// </para>
/// </summary>
//#if (notification == true)
[TestClass, TestCategory("IntegrationTest")]
public partial class AntiforgeryFormPostTests
{
    /// <summary>AutoCsrfProtectionFilter's own refusal, written as plain text rather than problem details.</summary>
    private const string refusal = "Anti-forgery token validation failed";

    /// <summary>The name EditForm gives the hidden token input, and the field antiforgery reads it back from.</summary>
    private const string tokenFieldName = "__RequestVerificationToken";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnAnonymousFormPost_Should_BeRefused_WithoutAnAntiforgeryToken()
    {
        await using var server = await StartServer();
        using var httpClient = CreateClient(server);

        using var response = await PostForm(httpClient, token: null, antiforgeryCookie: null);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            $"A form post with no antiforgery token is the cross-site case and must be refused. Body: {body}");
        Assert.Contains(refusal, body, $"The refusal must come from the antiforgery check. Body: {body}");
    }

    [TestMethod]
    public async Task AnAnonymousFormPost_Should_ReachTheEndpoint_WithAnAntiforgeryToken()
    {
        await using var server = await StartServer();
        using var httpClient = CreateClient(server);

        var (cookie, token) = MintFormToken(server);

        using var response = await PostForm(httpClient, token, cookie);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.DoesNotContain(refusal, body,
            $"A form post carrying the token the page issued must get past the antiforgery check. Body: {body}");

        // The endpoint's own answer for a device id nothing is subscribed with. Reaching it is the proof.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Body: {body}");
        Assert.AreEqual("false", body.Trim(), $"Expected the endpoint's own 'no such device' answer. Body: {body}");
    }

    private async Task<HttpResponseMessage> PostForm(HttpClient httpClient, string? token, string? antiforgeryCookie)
    {
        List<KeyValuePair<string, string>> fields = [];

        if (token is not null)
            fields.Add(new(tokenFieldName, token));

        // A device id nothing is subscribed with, so the endpoint answers false and sends no notification.
        var deviceId = $"antiforgery-test-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/Diagnostic/SendTestPushNotification/{deviceId}")
        {
            Content = new FormUrlEncodedContent(fields)
        };

        if (antiforgeryCookie is not null)
            request.Headers.Add("Cookie", antiforgeryCookie);

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }

    /// <summary>
    /// The token pair a server-rendered form would embed, taken from the server's own <see cref="IAntiforgery"/> -
    /// the same call <c>RazorComponentEndpointInvoker</c> makes while rendering a page.
    /// <para>
    /// It cannot be read off a response instead, because this template renders no server-side form: there is no
    /// <c>@formname</c> and no <c>AntiforgeryToken</c> anywhere, so no page carries a request token in its HTML (the
    /// reasoning is written out on <c>SharedResponseCacheCompatibleAntiforgery</c>). That is the point of the filter's
    /// last branch - nothing legitimate in this app posts a form, so everything that does is refused.
    /// </para>
    /// </summary>
    private static (string Cookie, string Token) MintFormToken(AppTestServer server)
    {
        var antiforgery = server.WebApp.Services.GetRequiredService<IAntiforgery>();

        var httpContext = new DefaultHttpContext { RequestServices = server.WebApp.Services };
        var tokens = antiforgery.GetAndStoreTokens(httpContext);

        var cookie = httpContext.Response.Headers.SetCookie
            .Select(value => value!.Split(';')[0])
            .FirstOrDefault(value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal));

        Assert.IsNotNull(cookie, "GetAndStoreTokens wrote no antiforgery cookie, so the request token binds to nothing.");
        Assert.IsNotNull(tokens.RequestToken, "GetAndStoreTokens produced no request token.");

        return (cookie, tokens.RequestToken);
    }

    /// <summary>The cookies are this test's to send by hand, and a redirect is an answer of its own.</summary>
    private static HttpClient CreateClient(AppTestServer server)
    {
        return new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
        {
            BaseAddress = server.WebAppServerAddress
        };
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

}
//#endif
