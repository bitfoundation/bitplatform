namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class AccessTokenCookieUITests : AppPageTest
{
    private const string UpdateSessionUri = $"api/v1/User/{nameof(IUserController.UpdateSession)}";

    /// <summary>
    /// The web app on one host and its api on another - the loopback server as localhost for the page, as 127.0.0.1 for
    /// the api, the way a standalone api sits on a sibling host. The api's own answer would carry the access_token
    /// cookie to the wrong host, so UpdateSession has to be answered by the page's host, or pre-rendering never sees
    /// the user (See RequestHeadersDelegatingHandler).
    /// </summary>
    [TestMethod]
    public async Task PreRendering_Should_SeeTheUser_WhenTheApiIsOnAnotherHost()
    {
        // Its startup params point the app's ServerAddress at 127.0.0.1.
        await using var server = new AppTestServer(Context);
        await server.Build(configureTestConfigurations: configuration =>
        {
            configuration["WebAppRender:BlazorMode"] = nameof(BlazorWebAppMode.BlazorWebAssembly);
            configuration["WebAppRender:PrerenderEnabled"] = "true";
        }).Start(TestContext.CancellationToken);

        var pageBase = new UriBuilder(server.WebAppServerAddress) { Host = "localhost" }.Uri;
        var protectedPage = new Uri(pageBase, PageUrls.Settings).ToString();

        var anonymousVisit = await Context.APIRequest.GetAsync(protectedPage);
        Assert.Contains(PageUrls.NotAuthorized, anonymousVisit.Url, "Precondition: without the cookie the page is not pre-rendered for anyone.");

        List<string> updateSessionHosts = [];
        Page.Request += (_, request) =>
        {
            if (request.Url.Contains(UpdateSessionUri, StringComparison.OrdinalIgnoreCase))
            {
                updateSessionHosts.Add(new Uri(request.Url).Host);
            }
        };

        await SignIn(pageBase);

        Assert.IsNotEmpty(updateSessionHosts);
        Assert.IsTrue(updateSessionHosts.All(host => host is "localhost"), $"UpdateSession went to {string.Join(", ", updateSessionHosts)}.");

        var cookie = (await Context.CookiesAsync([pageBase.ToString()])).Single(cookie => cookie.Name == "access_token");
        Assert.AreEqual("localhost", cookie.Domain, "A host-only cookie, for the page's host.");
        Assert.IsTrue(cookie.HttpOnly);

        var signedInVisit = await Context.APIRequest.GetAsync(protectedPage);
        Assert.AreEqual(200, signedInVisit.Status);
        Assert.DoesNotContain(PageUrls.NotAuthorized, signedInVisit.Url, "Pre-rendering did not recognize the signed-in user.");
    }

    private async Task SignIn(Uri pageBase)
    {
        await Page.GotoAsync(new Uri(pageBase, PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        await SignInPanelUtils.FillCredentials(Page, TestData.DefaultTestEmail, TestData.DefaultTestPassword);

        var updateSession = Page.WaitForResponseAsync(response => response.Url.Contains(UpdateSessionUri, StringComparison.OrdinalIgnoreCase));

        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        Assert.IsTrue((await updateSession).Ok, "UpdateSession failed after sign in.");
    }
}
