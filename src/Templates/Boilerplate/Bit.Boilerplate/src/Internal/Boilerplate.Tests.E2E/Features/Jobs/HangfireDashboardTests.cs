namespace Boilerplate.Tests.E2E.Features.Jobs;

/// <summary>
/// The Hangfire dashboard on every deployment that hosts one: /hangfire on each standalone API, and on Sales' web app
/// (its API is integrated). HangfireDashboardAuthorizationFilter lets in whoever holds System.Jobs_Manage - which only
/// the global admin role implies - and the user comes from the bearer handler, which also reads the access_token cookie:
/// that cookie is how the app's diagnostic modal opens the dashboard in a browser tab.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class HangfireDashboardTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    public async Task TheDashboard_Should_RenderForTheGlobalAdmin_ByTheAccessTokenCookie(string host)
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        // ---- Anonymous: refused - a 401, or on Sales the web app's redirect to its not-authorized page ----
        using (var response = await Send(HttpMethod.Get, host, "hangfire", cookies: null))
        {
            var refused = response.StatusCode is HttpStatusCode.Unauthorized
                || (response.StatusCode is HttpStatusCode.Redirect && response.Headers.Location?.ToString().Contains(PageUrls.NotAuthorized, StringComparison.OrdinalIgnoreCase) is true);

            Assert.IsTrue(refused, $"{host}hangfire answered an anonymous request with {(int)response.StatusCode} {response.Headers.Location}.");
        }

        // ---- The global admin, by the access_token cookie alone ----
        // A deployment only takes the tokens its own API issued, so the run's session (signed in on AdminPanelApi) serves
        // there, and the global admin signs in on the other two for the length of this test.
        await using var hostApiClient = host is DeployedApps.AdminPanelApi
            ? null
            : await DeployedApiClientProvider.SignInGlobalAdminOn(host, TestContext.CancellationToken);

        var authManager = (hostApiClient ?? await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken))
            .Services.GetRequiredService<AuthManager>();

        try
        {
            await ExpectDashboard(host, await authManager.GetFreshAccessToken(requestedBy: nameof(HangfireDashboardTests)));
        }
        finally
        {
            // Its own session on that deployment, not the run's shared one.
            if (hostApiClient is not null)
                await authManager.SignOut(CancellationToken.None);
        }
    }

    private async Task ExpectDashboard(string host, string? accessToken)
    {
        Assert.IsFalse(string.IsNullOrEmpty(accessToken), $"The global admin has no access token for {host}.");

        var accessTokenCookie = $"access_token={accessToken}";

        using (var response = await Send(HttpMethod.Get, host, "hangfire", accessTokenCookie))
        {
            var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{host}hangfire refused the global admin's access_token cookie.");
            Assert.IsNotNull(response.Content.Headers.ContentType, $"{host}hangfire answered with no Content-Type.");
            Assert.AreEqual("text/html", response.Content.Headers.ContentType.MediaType);
            Assert.Contains("Hangfire Dashboard", html, $"{host}hangfire answered 200 but not with the dashboard.");
        }

        // ---- The dashboard's own POSTs take its antiforgery token too: the cookie alone is what a cross-site form sends ----
        using (var response = await Send(HttpMethod.Post, host, "hangfire/stats", accessTokenCookie))
        {
            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode, $"{host}hangfire/stats served a POST carrying only the access_token cookie.");
        }
    }

    /// <summary>Cookies are this test's to write by hand, and a redirect is an answer of its own.</summary>
    private async Task<HttpResponseMessage> Send(HttpMethod method, string host, string path, string? cookies)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(2) };
        using var request = new HttpRequestMessage(method, new Uri(new Uri(host), path));

        if (cookies is not null)
            request.Headers.Add("Cookie", cookies);

        if (method == HttpMethod.Post)
            request.Content = new FormUrlEncodedContent([]);

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }
}
