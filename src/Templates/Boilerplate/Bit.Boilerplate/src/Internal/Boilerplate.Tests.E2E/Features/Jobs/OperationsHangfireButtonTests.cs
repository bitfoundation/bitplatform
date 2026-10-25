using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Jobs;

/// <summary>
/// The operations page's "Hangfire dashboard", as the global admin clicks it: the dashboard opens on the app's
/// own host, signed in by the access_token cookie UpdateSession wrote there - through Server.Web's forwarder on a
/// standalone api.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class OperationsHangfireButtonTests : AppTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (standalone api)")]
    [DataRow(App.Todo, DisplayName = "Todo (standalone api)")]
    [DataRow(App.Sales, DisplayName = "Sales (integrated api)")]
    public async Task TheDashboard_Should_OpenOnTheAppsOwnHost_SignedIn(App app)
    {
        await SkipWithoutGlobalAdminCredentials();

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);
        await SignInGlobalAdmin(page);

        // The button shows once the signed-in user's features are known.
        await GoToWhenInteractive(page, PageUrls.Operations);
        var openHangfire = page.GetByRole(AriaRole.Button, new() { Name = "Hangfire dashboard" });
        await openHangfire.WaitForAsync(new() { Timeout = 120_000 });

        var dashboard = await page.RunAndWaitForPopupAsync(() => openHangfire.ClickAsync());
        await dashboard.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var appHost = new Uri(DeployedApps.AddressOf(app)).Host;
        var dashboardUrl = new Uri(dashboard.Url);

        Assert.AreEqual(appHost, dashboardUrl.Host, "The dashboard has to open where the cookie is.");
        Assert.StartsWith("/hangfire", dashboardUrl.AbsolutePath);
        await Expect(dashboard).ToHaveTitleAsync(new Regex("Hangfire"));

        var cookie = (await page.Context.CookiesAsync([DeployedApps.AddressOf(app)])).Single(cookie => cookie.Name == "access_token");
        Assert.AreEqual(appHost, cookie.Domain, "A host-only cookie, for the app's host.");
        Assert.IsTrue(cookie.HttpOnly);
    }

    /// <summary>Password, then the authenticator code TfaPanel asks the global admin for.</summary>
    private async Task SignInGlobalAdmin(IPage page)
    {
        var configuration = (await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken)).Services.GetRequiredService<IConfiguration>();

        if (configuration["GlobalAdminAuthenticatorKey"] is not { Length: > 0 } authenticatorKey)
        {
            Assert.Inconclusive("'GlobalAdminAuthenticatorKey' is not in this project's user secrets, so the second factor cannot be answered.");
            return;
        }

        await GoToWhenInteractive(page, PageUrls.SignIn);

        await SubmitCredentials(page, configuration["GlobalAdminEmail"]!, configuration["GlobalAdminPassword"]!);

        // TfaPanel submits on its own once all six digits are in.
        await BitOtpInputUtils.FillOtpInputs(page, GlobalAdmin.TwoFactorCode(authenticatorKey));

        await Expect(page).Not.ToHaveURLAsync(new Regex("sign-in", RegexOptions.IgnoreCase));

        await DeleteSessionAtCleanup(page);
    }
}
