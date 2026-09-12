using OtpNet;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.OAuth;

/// <summary>
/// A token got the way an external app gets one - through /oauth/authorize, the app's own sign in and its consent
/// page - rather than the run-wide global admin session DeployedApiClientProvider signs in directly. The same account,
/// the same second factor, yet the token has to be good for /dev-mcp only: its audience is that resource and its
/// features are the scope's, so the rest of the api must refuse it.
/// <para>
/// The client is <c>e2e</c>, pre-registered in both deployments' <c>OAuth:Clients</c> with the loopback redirect uri
/// <c>http://127.0.0.1/callback</c>. A loopback redirect uri matches on any port (RFC 8252 7.3), so the browser comes
/// back to a port of this test, where a route catches the code before anything leaves the machine. A Client ID Metadata
/// Document would be the alternative, but it has to be an https url the deployment can fetch, and this machine cannot
/// host one - which is why the suite used to borrow a real product's published document.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebOAuthDevMcpTokenTests : AppTestBase
{
    private const string clientId = "e2e";

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>AdminPanel's api is exercised by every other dev-mcp test; these are the two other ways an api ships.</summary>
    [TestMethod]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    [DataRow(App.Todo, DisplayName = "Todo (standalone API)")]
    public async Task AnOAuthToken_Should_OpenDevMcp_AndNothingElse(App app)
    {
        await SkipWithoutGlobalAdminCredentials();

        var api = new Uri(DeployedApps.ApiOf(app));
        var issuer = api.ToString().TrimEnd('/');
        var resource = $"{issuer}/dev-mcp";
        var redirectUri = $"http://127.0.0.1:{FreeLoopbackPort()}/callback";
        var (verifier, challenge) = GeneratePkcePair();
        var state = Guid.NewGuid().ToString("N");

        // Registered first: approving creates the grant's own session, whatever happens after it.
        var startedOn = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();
        RegisterForCleanup(() => RevokeGrants(resource, startedOn));

        // ---- The redirect back to the "app": caught in the browser, so no listener is needed ----
        var callback = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        await Context.RouteAsync($"{redirectUri}**", async route =>
        {
            callback.TrySetResult(route.Request.Url);
            await route.FulfillAsync(new() { Status = 200, ContentType = "text/plain", Body = "The authorization code reached the test." });
        });

        var authorize = QueryHelpers.AddQueryString(new Uri(api, "oauth/authorize").ToString(), new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = OAuthScopes.DevMcp,
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["resource"] = resource
        });

        // ---- authorize -> consent page -> sign in (password + second factor) -> consent page ----
        await Page.GotoAsync(authorize);
        await SignInGlobalAdminThroughTheForm(Page);
        await DeleteSessionAtCleanup(Page);

        // Read now: once authorized, the page is on the redirect's loopback origin, whose storage holds no token.
        var sessionAccessToken = await ReadAccessToken(Page);

        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Authorize, Exact = true }).ClickAsync();

        var callbackUrl = await callback.Task.WaitAsync(TimeSpan.FromMinutes(2), TestContext.CancellationToken);
        var query = QueryHelpers.ParseQuery(new Uri(callbackUrl).Query);

        Assert.AreEqual(state, query["state"].ToString(), "The redirect has to carry the state the request was started with.");
        Assert.AreEqual(issuer, query["iss"].ToString(), "RFC 9207: the response names the authorization server that answered.");

        // ---- The code for a token, as the client would ----
        using var httpClient = new HttpClient { BaseAddress = api, Timeout = TimeSpan.FromMinutes(2) };

        using var tokenResponse = await httpClient.PostAsync("oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = query["code"].ToString(),
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = verifier
        }), TestContext.CancellationToken);

        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, tokenResponse.StatusCode, $"The token exchange failed: {tokenBody}");

        var accessToken = JsonNode.Parse(tokenBody)!["access_token"]!.GetValue<string>();

        Assert.AreEqual(resource, AudienceOf(accessToken), "The audience is what keeps the token out of the rest of the api.");

        // ---- It opens /dev-mcp ----
        using (var mcpHttpClient = new HttpClient { BaseAddress = api, Timeout = TimeSpan.FromMinutes(2) })
        {
            mcpHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(api, "dev-mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            }, mcpHttpClient, NullLoggerFactory.Instance, ownsHttpClient: false);

            await using var mcp = await McpClient.CreateAsync(transport, cancellationToken: TestContext.CancellationToken);

            var tools = await mcp.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
            Assert.IsNotEmpty(tools, "/dev-mcp answered but lists no tool.");

            var report = await mcp.GetDiagnosticReport(TestContext.CancellationToken);
            Assert.IsFalse(string.IsNullOrWhiteSpace(report), "A dev-mcp tool call made with the OAuth token came back empty.");
        }

        // ---- And nothing else: the same account's own session token is let in, this one is not ----
        var getCurrentUser = new Uri(api, "api/v1/User/GetCurrentUser/");

        Assert.AreEqual(HttpStatusCode.OK, await StatusWith(httpClient, getCurrentUser, sessionAccessToken),
            "The control: the browser's own session token has to be let into GetCurrentUser, or its refusal below proves nothing.");

        var statusWithOAuthToken = await StatusWith(httpClient, getCurrentUser, accessToken);
        Assert.IsTrue(statusWithOAuthToken is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"GetCurrentUser answered {(int)statusWithOAuthToken} to a token whose audience is {resource}; it has to be refused.");
    }

    /// <summary>
    /// The consent page sends a signed-out visitor to /sign-in and brings them back. The global admin has two factor
    /// authentication through an authenticator app, whose code this computes from the shared key.
    /// </summary>
    private async Task SignInGlobalAdminThroughTheForm(IPage page)
    {
        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.AdminPanelApi);
        var configuration = apiClient.Services.GetRequiredService<IConfiguration>();

        if (configuration["GlobalAdminAuthenticatorKey"] is not { Length: > 0 } authenticatorKey)
        {
            Assert.Inconclusive("'GlobalAdminAuthenticatorKey' is not in this project's user secrets, so the second factor cannot be answered.");
            return;
        }

        await Expect(page).ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase), new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });
        await WaitUntilInteractive(page);

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillEnsuringStable(configuration["GlobalAdminEmail"]!);
        var passwordBox = page.GetByPlaceholder(AppStrings.PasswordPlaceholder);
        await passwordBox.FillEnsuringStable(configuration["GlobalAdminPassword"]!);
        await passwordBox.PressAsync("Enter");

        // TfaPanel submits on its own once all six digits are in.
        await BitOtpInputUtils.FillOtpInputs(page, new Totp(Base32Encoding.ToBytes(authenticatorKey)).ComputeTotp());

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));
    }

    private static async Task<HttpStatusCode> StatusWith(HttpClient httpClient, Uri url, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<string> ReadAccessToken(IPage page)
    {
        return await page.EvaluateAsync<string>("() => localStorage.getItem('access_token') ?? sessionStorage.getItem('access_token')");
    }

    /// <summary>The payload's <c>aud</c>, read without validating anything - the server is what validates it.</summary>
    private static string? AudienceOf(string jwt)
    {
        var payload = JsonNode.Parse(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(jwt.Split('.')[1])))!["aud"];

        return payload is JsonArray audiences ? audiences.Single()?.GetValue<string>() : payload?.GetValue<string>();
    }

    private static int FreeLoopbackPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static (string Verifier, string Challenge) GeneratePkcePair()
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }

    /// <summary>
    /// A grant is its own session (OAuthGrantConfiguration), and revoking it is deleting that session. Picked by this
    /// row's resource, so the Sales and Todo rows - running side by side under one client id - never take each other's.
    /// Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.
    /// </summary>
    private static async Task RevokeGrants(string resource, long startedOn)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var grantSessionIds = await dbContext.OAuthGrants.IgnoreQueryFilters()
            .Where(grant => grant.ClientId == clientId && grant.Resource == resource && grant.UserSession!.StartedOn >= startedOn)
            .Select(grant => grant.UserSessionId)
            .ToListAsync(CancellationToken.None);

        await dbContext.UserSessions.IgnoreQueryFilters()
            .Where(session => grantSessionIds.Contains(session.Id))
            .ExecuteDeleteAsync(CancellationToken.None);
    }
}
