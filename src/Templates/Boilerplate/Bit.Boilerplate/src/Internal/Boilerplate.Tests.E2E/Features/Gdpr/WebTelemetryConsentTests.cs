using System.Collections.Concurrent;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Gdpr;

/// <summary>
/// The Analytics consent of Settings > Privacy, as Application Insights' ingestion endpoint sees it. What consent
/// gates here is what is left on the device - the SDK's own id cookies - not whether a log line says whose it is:
/// telemetry carries the signed-in user's id throughout, deliberately, because that is how a support request is traced
/// back to the session behind it (Sentry is given the same). Granted, the SDK may keep its ai_user cookie; withdrawn,
/// the ids it wrote while it stood are taken away again rather than merely left unused.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebTelemetryConsentTests : AppTestBase
{
    /// <summary>The SDK's cookies that carry an id; see AppInsightsJsSdkService.</summary>
    private static readonly string[] identifyingCookieNames = ["ai_user", "ai_session", "ai_authUser"];

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// AppPageTest answers the consent banner with a refusal before the app opens, so the journey starts refused. The
    /// walks between About and Terms are in-app navigations: a document load would re-run that answer.
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    public async Task Telemetry_Should_KeepIdentifyingCookiesOffTheDevice_UntilAnalyticsConsentIsGranted(App app)
    {
        var track = new ConcurrentQueue<string>();

        Context.Request += (_, request) =>
        {
            if (request.Url.Contains("/v2/track", StringComparison.OrdinalIgnoreCase) is false || request.PostData is not { Length: > 0 } body)
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

        Assert.IsNotEmpty(sentWhileRefused, "Nothing reached the ingestion endpoint, so there is nothing to tell apart - the checks below would pass vacuously.");
        // What the payloads say is not the subject: the user id travels in them by design (See the class summary).
        // Refused, the device is what has to stay clean.
        await AssertNoIdentifyingCookie("With Analytics refused");

        // ---- Granted ----
        analytics = await OpenPrivacySettings(page);
        await analytics.ClickAsync();
        await Expect(analytics).ToBeCheckedAsync();

        var sentWhileGranted = await WalkAboutAndTerms(page, track);

        Assert.Contains(body => body.Contains(userId, StringComparison.OrdinalIgnoreCase), sentWhileGranted,
            $"With Analytics granted, no telemetry named the signed-in user {userId}.");
        Assert.Contains(cookie => cookie.Name is "ai_user", await Context.CookiesAsync(),
            "With Analytics granted, the SDK is allowed its ai_user cookie and should have set it.");

        // ---- Withdrawn: the identifiers written while it stood are taken away, not just left unused ----
        analytics = await OpenPrivacySettings(page);
        await analytics.ClickAsync();
        await Expect(analytics).Not.ToBeCheckedAsync();

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        while ((await Context.CookiesAsync()).Any(cookie => identifyingCookieNames.Contains(cookie.Name)) && DateTimeOffset.UtcNow < deadline)
        {
            await page.WaitForTimeoutAsync(250);
        }

        await AssertNoIdentifyingCookie("Once Analytics is withdrawn");
    }

    private async Task<ILocator> OpenPrivacySettings(IPage page)
    {
        await page.GoToInApp($"{PageUrls.Settings}/{PageUrls.SettingsSections.Privacy}");

        var analytics = page.GetByRole(AriaRole.Switch, new() { Name = AppStrings.ConsentAnalytics });
        await Expect(analytics).ToBeVisibleAsync();

        return analytics;
    }

    /// <summary>
    /// About, then Terms, then the SDK's own flush - it batches for up to 15 seconds otherwise. Returns what reached the
    /// endpoint during the walk only.
    /// </summary>
    private static async Task<List<string>> WalkAboutAndTerms(IPage page, ConcurrentQueue<string> track)
    {
        track.Clear();

        await page.GoToInApp(PageUrls.About);
        await page.GoToInApp(PageUrls.Terms);

        await page.EvaluateAsync("() => window.appInsights?.flush?.()");
        await page.WaitForTimeoutAsync(3_000);

        return [.. track];
    }

    private async Task AssertNoIdentifyingCookie(string when)
    {
        var identifying = (await Context.CookiesAsync()).Where(cookie => identifyingCookieNames.Contains(cookie.Name)).Select(cookie => cookie.Name).ToArray();

        Assert.IsEmpty(identifying, $"{when}, the device still holds {string.Join(", ", identifying)}.");
    }
}
