using System.Collections.Concurrent;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Gdpr;

/// <summary>
/// The Analytics consent of Settings > Privacy, as Application Insights' ingestion endpoint sees it. Refused, Belemetry
/// sends no page views and keeps nothing on the device that recognizes it between visits; granted, page views carry the
/// signed-in user; withdrawn, the ids it wrote while it stood are taken away again rather than merely left unused.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebTelemetryConsentTests : AppTestBase
{
    /// <summary>Belemetry's browser storage that carries an id: the pseudonymous device, the session, and the signed in user.</summary>
    private const string IdentifyingStorageScript =
        "() => [['bit-belemetry-pseudo-id', localStorage], ['bit-belemetry-session', sessionStorage], ['bit-belemetry-user-id', sessionStorage]]" +
        ".filter(([key, storage]) => storage.getItem(key) !== null).map(([key]) => key)";

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// AppPageTest answers the consent banner with a refusal before the app opens, so the journey starts refused. The
    /// walks between About and Terms are in-app navigations: a document load would re-run that answer.
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    [DataRow(App.Todo, DisplayName = "Todo")]
    public async Task Telemetry_Should_KeepIdentifyingStorageOffTheDevice_UntilAnalyticsConsentIsGranted(App app)
    {
        var track = new ConcurrentQueue<string>();

        Context.Request += (_, request) =>
        {
            if (request.Url.Contains("/v2.1/track", StringComparison.OrdinalIgnoreCase) is false || request.PostData is not { Length: > 0 } body)
                return;

            track.Enqueue(body);
        };

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        await SignIn(page, StoreUser.Email, StoreUser.Password);
        var userId = (await GetUserId(page)).ToString();

        var analytics = await OpenPrivacySettings(page);
        await Expect(analytics).Not.ToBeCheckedAsync();

        // ---- Refused ----
        var sentWhileRefused = await WalkAboutAndTerms(page, track);

        Assert.DoesNotContain(body => body.Contains("Microsoft.ApplicationInsights.PageView", StringComparison.Ordinal), sentWhileRefused,
            "With Analytics refused, page views reached the ingestion endpoint.");
        await AssertNoIdentifyingStorage(page, "With Analytics refused");

        // ---- Granted ----
        analytics = await OpenPrivacySettings(page);
        await analytics.ClickAsync();
        await Expect(analytics).ToBeCheckedAsync();

        var sentWhileGranted = await WalkAboutAndTerms(page, track);

        Assert.Contains(body => body.Contains("Microsoft.ApplicationInsights.PageView", StringComparison.Ordinal) && body.Contains($"\"ai.user.authUserId\":\"{userId}\"", StringComparison.OrdinalIgnoreCase), sentWhileGranted,
            $"With Analytics granted, no page view named the signed-in user {userId}.");
        Assert.Contains("bit-belemetry-pseudo-id", await page.EvaluateAsync<string[]>(IdentifyingStorageScript),
            "With Analytics granted, Belemetry is allowed its pseudonymous device id and should have stored it.");

        // ---- Withdrawn: the identifiers written while it stood are taken away, not just left unused ----
        analytics = await OpenPrivacySettings(page);
        await analytics.ClickAsync();
        await Expect(analytics).Not.ToBeCheckedAsync();

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        while ((await page.EvaluateAsync<string[]>(IdentifyingStorageScript)).Length > 0 && DateTimeOffset.UtcNow < deadline)
        {
            await page.WaitForTimeoutAsync(250);
        }

        await AssertNoIdentifyingStorage(page, "Once Analytics is withdrawn");
    }

    private async Task<ILocator> OpenPrivacySettings(IPage page)
    {
        await page.GoToInApp($"{PageUrls.Settings}/{PageUrls.SettingsSections.Privacy}");

        var analytics = page.GetByRole(AriaRole.Switch, new() { Name = AppStrings.ConsentAnalytics });
        await Expect(analytics).ToBeVisibleAsync();

        return analytics;
    }

    /// <summary>
    /// About, then Terms, then the browser script's own flush - it batches for a few seconds otherwise. Returns what
    /// reached the endpoint during the walk only.
    /// </summary>
    private static async Task<List<string>> WalkAboutAndTerms(IPage page, ConcurrentQueue<string> track)
    {
        track.Clear();

        await page.GoToInApp(PageUrls.About);
        await page.GoToInApp(PageUrls.Terms);

        await page.EvaluateAsync("() => window.BitBelemetry?.flush?.()");
        await page.WaitForTimeoutAsync(3_000);

        return [.. track];
    }

    private static async Task AssertNoIdentifyingStorage(IPage page, string when)
    {
        var identifying = await page.EvaluateAsync<string[]>(IdentifyingStorageScript);

        Assert.IsEmpty(identifying, $"{when}, the device still holds {string.Join(", ", identifying)}.");
    }
}
