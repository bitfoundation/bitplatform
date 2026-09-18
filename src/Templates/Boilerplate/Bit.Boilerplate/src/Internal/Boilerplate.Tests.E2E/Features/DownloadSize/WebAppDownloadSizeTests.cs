using System.Collections.Concurrent;

namespace Boilerplate.Tests.E2E.Features.DownloadSize;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebAppDownloadSizeTests : AppPageTest
{
    /// <summary>Growing more than this over the expected size fails - the regression this test exists to catch.</summary>
    private const double growthTolerance = 0.10;

    /// <summary>
    /// Shrinking more than this fails too: either the measurement broke, or the app got smaller and the DataRow's
    /// expected size should be re-baselined to the value the failure message prints.
    /// </summary>
    private const double shrinkTolerance = 0.30;

    /// <summary>
    /// No request started or finished for this long counts as downloaded - long enough to bridge a bswup install's
    /// gaps (page load -> precache -> app start), short enough that telemetry beacons cannot keep the wait alive.
    /// </summary>
    private static readonly TimeSpan networkQuietPeriod = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan measurementDeadline = TimeSpan.FromMinutes(4);

    /// <summary>
    /// AppConsentBanner opens from OnAfterFirstRenderAsync - the app's own "I am up" signal. It is the only bottom
    /// panel in the app, so its position identifies it without depending on its localized text.
    /// </summary>
    private const string consentBannerSelector = ".bit-pnl-cnt.bit-pnl-bottom.bit-pnl-opn";

    /// <summary>
    /// Unlike every other test this one wants the banner, and leaving it unanswered downloads no more than refusing it.
    /// </summary>
    protected override bool AnswersConsentBanner => false;

    /// <summary>
    /// First visit with a cold cache: the page's own requests plus the bswup service worker's precache, summed as
    /// compressed-over-the-wire bytes. The expected sizes are earlier measurements of this same method - re-baseline
    /// them from the failure message whenever a deliberate change moves an app's size.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.AdminPanel, 5.5, DisplayName = nameof(DeployedApps.AdminPanel))]
    [DataRow(DeployedApps.AdminPanelWasmStandalone, 5.4, DisplayName = nameof(DeployedApps.AdminPanelWasmStandalone))]

    [DataRow(DeployedApps.Todo, 5.1, DisplayName = nameof(DeployedApps.Todo))]
    [DataRow(DeployedApps.TodoAot, 8.6, DisplayName = nameof(DeployedApps.TodoAot))]
    [DataRow(DeployedApps.TodoSmall, 3.7, DisplayName = nameof(DeployedApps.TodoSmall))]
    [DataRow(DeployedApps.TodoOffline, 7.7, DisplayName = nameof(DeployedApps.TodoOffline))]

    [DataRow(DeployedApps.Sales, 5.5, DisplayName = nameof(DeployedApps.Sales))]
    public async Task FirstVisitDownloadSize_Should_NotRegress(string url, double expectedMegabytes)
    {
        // Qualified because the inherited BrowserType property shadows the type of the same name.
        if (BrowserName is not Microsoft.Playwright.BrowserType.Chromium)
            Assert.Inconclusive("Only chromium honors PW_EXPERIMENTAL_SERVICE_WORKER_NETWORK_EVENTS, so on other engines the service worker's downloads go uncounted and the totals here would be meaningless. The payload is engine-independent anyway; a BROWSER=firefox/webkit run covers compatibility through the other web tests.");

        var (totalBytes, requestCount) = await MeasureFirstVisitDownloadSize(url);

        var actualMegabytes = totalBytes / 1024d / 1024d;

        TestContext.WriteLine($"{url} downloaded {actualMegabytes:F2}MB over {requestCount} requests (expected ~{expectedMegabytes:F2}MB).");

        Assert.IsLessThanOrEqualTo(expectedMegabytes * (1 + growthTolerance), actualMegabytes,
            $"{url} now downloads {actualMegabytes:F2}MB, more than {growthTolerance:P0} over the expected {expectedMegabytes:F2}MB - a size regression.");

        Assert.IsGreaterThanOrEqualTo(expectedMegabytes * (1 - shrinkTolerance), actualMegabytes,
            $"{url} downloaded only {actualMegabytes:F2}MB against the expected {expectedMegabytes:F2}MB - either the measurement broke or the app shrank and this DataRow should be re-baselined.");
    }

    /// <summary>
    /// Sums the encoded body + header bytes of every request a fresh, empty-cache context makes for
    /// <paramref name="url"/> - the service worker's included, thanks to .runsettings'
    /// PW_EXPERIMENTAL_SERVICE_WORKER_NETWORK_EVENTS. Waits for both a booted app and a quiet network, since either
    /// alone stops early: quiet is reached in the gap before the precache starts, and a booted app may still precache.
    /// </summary>
    private async Task<(long totalBytes, int requestCount)> MeasureFirstVisitDownloadSize(string url)
    {
        var requests = new ConcurrentQueue<IRequest>();
        var lastActivityTicks = DateTimeOffset.UtcNow.UtcTicks;

        void Touch() => Interlocked.Exchange(ref lastActivityTicks, DateTimeOffset.UtcNow.UtcTicks);

        Context.Request += (_, request) => { requests.Enqueue(request); Touch(); };
        Context.RequestFinished += (_, _) => Touch();
        Context.RequestFailed += (_, _) => Touch();

        await Page.GotoAsync(url);

        var consentBanner = Page.Locator(consentBannerSelector).First;

        var deadline = DateTimeOffset.UtcNow + measurementDeadline;
        var measured = false;

        while (DateTimeOffset.UtcNow < deadline)
        {
            var lastActivity = new DateTimeOffset(Interlocked.Read(ref lastActivityTicks), TimeSpan.Zero);

            if (DateTimeOffset.UtcNow - lastActivity >= networkQuietPeriod && await consentBanner.IsVisibleAsync())
            {
                measured = true;
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.CancellationToken);
        }

        // Running out of deadline is an incomplete measurement, not a size regression, so it fails as itself rather
        // than letting a fraction of a first visit reach the assertions: silence alone is also what the gap before the
        // precache looks like, and a booted app may still be precaching, which is why the loop waits for both.
        if (measured is false)
            Assert.Fail($"{url} reached no quiet network with a booted app within {measurementDeadline.TotalMinutes:F0} minutes (consent banner visible: {await consentBanner.IsVisibleAsync()}), so its download size was never fully measured.");

        long totalBytes = 0;
        var requestCount = 0;

        foreach (var request in requests)
        {
            try
            {
                var sizes = await request.SizesAsync();
                totalBytes += Math.Max(0, sizes.ResponseBodySize) + Math.Max(0, sizes.ResponseHeadersSize);
                requestCount++;
            }
            catch (PlaywrightException)
            {
                // A request that never finished has no sizes, and its bytes are not part of what the app needs.
            }
        }

        return (totalBytes, requestCount);
    }
}
