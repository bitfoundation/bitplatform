namespace Boilerplate.Tests.E2E.Features.ForceUpdate;

/// <summary>
/// ForceUpdateMiddleware as deployed: an API refuses a client whose X-App-Version is below
/// <c>SupportedAppVersions</c>' minimum for its X-App-Platform, before any endpoint runs. The demos configure a minimum
/// for every platform but Linux, so no deployment setting has to change - an ancient version is enough.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class ForceUpdateTests
{
    private const string ancientVersion = "1.0.0";

    /// <summary>Above any minimum the demos will configure: the control that the refusal is about the version alone.</summary>
    private const string futureVersion = "9999.0.0";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, nameof(AppPlatformType.Web), DisplayName = "AdminPanelApi, Web")]
    [DataRow(DeployedApps.TodoApi, nameof(AppPlatformType.Web), DisplayName = "TodoApi, Web")]
    [DataRow(DeployedApps.Sales, nameof(AppPlatformType.Web), DisplayName = "Sales (integrated API), Web")]
    [DataRow(DeployedApps.AdminPanelApi, nameof(AppPlatformType.Android), DisplayName = "AdminPanelApi, Android")]
    [DataRow(DeployedApps.AdminPanelApi, nameof(AppPlatformType.Ios), DisplayName = "AdminPanelApi, iOS")]
    [DataRow(DeployedApps.AdminPanelApi, nameof(AppPlatformType.MacOS), DisplayName = "AdminPanelApi, macOS")]
    [DataRow(DeployedApps.AdminPanelApi, nameof(AppPlatformType.Windows), DisplayName = "AdminPanelApi, Windows")]
    public async Task AnOutdatedClient_Should_BeRefusedByTheApi(string api, string platform)
    {
        // Anonymous, and its real answer is a 404: nothing but the middleware can make it a 400.
        var path = $"api/v1/Attachment/GetAttachment/{Guid.NewGuid()}/UserProfileImageSmall";

        using (var response = await Send(api, path, ancientVersion, platform))
        {
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"{api} answered a {platform} client at {ancientVersion} with: {body}");
            Assert.Contains(nameof(ClientNotSupportedException), body);
            // The key the clients translate into the force update panel (See ExceptionDelegatingHandler).
            Assert.Contains(nameof(AppStrings.ForceUpdateTitle), body);
        }

        using (var response = await Send(api, path, futureVersion, platform))
        {
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, $"The same request from an up-to-date {platform} client should reach the endpoint's own answer.");
        }
    }

    /// <summary>
    /// The same refusal must not reach a browser navigating to a page: the json would render as text in the tab, and
    /// the app that knows how to show a force update panel would never load. Sales is the one deployment whose web
    /// host runs the middleware, because its API is integrated (See HttpRequestExtensions.IsPageRequest).
    /// </summary>
    [TestMethod]
    public async Task AnOutdatedClient_Should_StillBeServedThePage()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(DeployedApps.Sales), "en-US/"));

        request.Headers.TryAddWithoutValidation("X-App-Version", ancientVersion);
        request.Headers.TryAddWithoutValidation("X-App-Platform", nameof(AppPlatformType.Web));
        // What a browser sends when it navigates.
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Sales answered a page request from an outdated client with: {body}");
        Assert.IsNotNull(response.Content.Headers.ContentType, "Sales answered a page request from an outdated client with no Content-Type.");
        Assert.AreEqual("text/html", response.Content.Headers.ContentType.MediaType, $"Sales answered a page request from an outdated client with: {body}");
        Assert.DoesNotContain(nameof(ClientNotSupportedException), body, "The force update refusal reached the document instead of the app's API calls.");
    }

    /// <summary>A bare client: the app's own handler would add its real version next to the one under test.</summary>
    private async Task<HttpResponseMessage> Send(string api, string path, string appVersion, string appPlatform)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(api), path));

        request.Headers.TryAddWithoutValidation("X-App-Version", appVersion);
        request.Headers.TryAddWithoutValidation("X-App-Platform", appPlatform);

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }
}
