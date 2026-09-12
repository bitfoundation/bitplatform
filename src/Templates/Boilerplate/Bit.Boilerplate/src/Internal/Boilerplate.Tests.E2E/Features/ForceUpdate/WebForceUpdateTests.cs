using System.Text.RegularExpressions;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.ForceUpdate;

/// <summary>
/// What a stale client meets: every API call it makes declares a version the deployment no longer supports - here by
/// rewriting the X-App-Version of the browser's own API calls, as a PWA still running an old cached build would send.
/// The refusal becomes a persistent FORCE_UPDATE message (See ExceptionDelegatingHandler), which ForceUpdateSnackBar shows.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebForceUpdateTests : AppTestBase
{
    private const string ancientVersion = "1.0.0";

    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(App.Sales, DisplayName = "Sales (prerendered, integrated API)")]
    [DataRow(App.Todo, DisplayName = "Todo (prerendered, standalone API)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (prerendered, bit Brouter)")]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (not prerendered)")]
    public async Task AnOutdatedClient_Should_BeShownTheForceUpdatePanel(App app)
    {
        await SkipWithoutGlobalAdminCredentials();

        var email = $"e2e-{Guid.NewGuid():N}"[..14] + "@bitplatform.dev";
        // Should the rewrite ever miss, the magic link below would reach the endpoint and could create this user.
        RegisterForCleanup(() => DeleteUser(email));

        var rewritten = 0;
        await Page.RouteAsync(ApiCall(), async route =>
        {
            Interlocked.Increment(ref rewritten);
            var headers = new Dictionary<string, string>(route.Request.Headers) { ["x-app-version"] = ancientVersion };
            await route.ContinueAsync(new() { Headers = headers });
        });

        await Page.GotoAsync(new Uri(new Uri(DeployedApps.AddressOf(app)), PageUrls.SignIn).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        var page = Page;

        // An API call a user makes: asking for a magic link. The middleware refuses it before the endpoint runs.
        var emailBox = page.GetByPlaceholder(AppStrings.EmailPlaceholder);
        var sendButton = page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SendMagicLinkButtonText });
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        while (true)
        {
            // Typed into the prerendered form before it turns interactive, the value never reaches the component, and
            // the button stays disabled - so it is typed again until the button takes it.
            await emailBox.FillAsync(string.Empty);
            await emailBox.FillAsync(email);

            try
            {
                await Expect(sendButton).ToBeEnabledAsync(new() { Timeout = 5_000 });
                break;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }

        await sendButton.ClickAsync();

        // Scoped to the panel: the exception's own message is the same title, and may reach AppSnackBar too.
        var panel = page.Locator(".force-update-snack-bar");
        await Expect(panel.GetByText(AppStrings.ForceUpdateTitle)).ToBeVisibleAsync();
        await Expect(panel.GetByText(AppStrings.ForceUpdateBody)).ToBeVisibleAsync();
        await Expect(panel.GetByRole(AriaRole.Button, new() { Name = AppStrings.Update })).ToBeVisibleAsync();

        Assert.IsGreaterThan(0, rewritten, "No API call went through the rewrite, so the panel was not this test's doing.");
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task DeleteUser(string email)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var db = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        var normalizedEmail = email.ToUpperInvariant();
        var userIds = await db.Users.IgnoreQueryFilters().Where(u => u.NormalizedEmail == normalizedEmail).Select(u => u.Id).ToListAsync(CancellationToken.None);

        await db.UserSessions.IgnoreQueryFilters().Where(session => userIds.Contains(session.UserId)).ExecuteDeleteAsync(CancellationToken.None);
        await db.Users.IgnoreQueryFilters().Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync(CancellationToken.None);
    }

    /// <summary>The app's own API calls, whichever host serves them (todo-api, adminpanel-api, or Sales itself).</summary>
    [GeneratedRegex(@"/api/v\d+/")]
    private static partial Regex ApiCall();
}
