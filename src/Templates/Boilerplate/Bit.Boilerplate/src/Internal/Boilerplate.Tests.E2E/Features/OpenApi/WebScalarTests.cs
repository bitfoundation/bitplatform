using System.Text.RegularExpressions;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.OpenApi;

/// <summary>
/// Scalar (MapScalarApiReference) as a developer uses it: open /scalar/v1, pick an operation, press its "Test Request"
/// button and send the request from Scalar's API client - the request goes from the browser to the deployment, and the
/// client shows the answer.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebScalarTests : AppTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    public async Task AnAnonymousOperation_Should_AnswerThroughScalar(string api)
    {
        // Anonymous, read only, in every document, and known to answer.
        var response = await SendFromScalar(api, tag: "identity", operation: "/api/v1/Identity/GetSupportedExternalAuthSchemes");

        Assert.AreEqual(200, response.Status, $"{api} answered Scalar's request with {response.Status}.");
        Assert.Contains("Google", await response.TextAsync(), "The external sign-in schemes the demos configure.");
    }

    /// <summary>
    /// The global admin's access token given to the browser Scalar runs in, as the access_token cookie the API also reads
    /// (See HttpContextExtensions.GetAccessToken): GetCurrentUser, sent from Scalar, answers with the global admin.
    /// Not through Scalar's header editor - see this class's markdown notes for why that could not be driven.
    /// </summary>
    [TestMethod]
    public async Task GetCurrentUser_Should_AnswerTheGlobalAdmin_ThroughScalar()
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        var accessToken = await globalApiClient.Services.GetRequiredService<AuthManager>().GetFreshAccessToken(requestedBy: nameof(WebScalarTests));
        var email = globalApiClient.Services.GetRequiredService<IConfiguration>()["GlobalAdminEmail"]!;

        await Context.AddCookiesAsync([new() { Name = "access_token", Value = accessToken!, Url = DeployedApps.AdminPanelApi, Secure = true, HttpOnly = true }]);

        var response = await SendFromScalar(DeployedApps.AdminPanelApi, tag: "user", operation: "/api/v1/User/GetCurrentUser");

        Assert.AreEqual(200, response.Status, "GetCurrentUser, sent from Scalar with the global admin's access_token cookie.");
        Assert.Contains(email, await response.TextAsync(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Opens the operation, its API client, sends, and returns what the browser got; the client must show the same status.</summary>
    private async Task<IResponse> SendFromScalar(string api, string tag, string operation)
    {
        await Page.GotoAsync(new Uri(new Uri(api), $"scalar/v1#tag/{tag}/GET{operation}").ToString());

        var testRequest = Page.GetByRole(AriaRole.Button, new() { Name = $"Test Request (get {operation})" });
        await testRequest.ScrollIntoViewIfNeededAsync();
        await testRequest.ClickAsync();

        var client = Page.GetByRole(AriaRole.Dialog, new() { Name = "API Client" });
        await Expect(client).ToBeVisibleAsync();

        // The client's own shortcut: its address bar Send button is hidden below a container width.
        var response = await Page.RunAndWaitForResponseAsync(async () =>
        {
            await client.ClickAsync(new() { Position = new() { X = 20, Y = 20 } });
            await Page.Keyboard.PressAsync("Control+Enter");
        }, r => new Uri(r.Url).AbsolutePath.EndsWith(operation, StringComparison.OrdinalIgnoreCase));

        await Expect(client).ToContainTextAsync(new Regex($@"Status:\s*{response.Status}"));

        return response;
    }
}
