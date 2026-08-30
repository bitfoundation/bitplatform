using System.Collections.Concurrent;

namespace Boilerplate.Tests.E2E.Features.Web;

[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebAppDownloadSizeTests : AppPageTest
{
    /// <summary>
    /// Growing past the expected size by more than this fails the test - the regression this test exists to catch.
    /// </summary>
    private const double growthTolerance = 0.10;

    /// <summary>
    /// Shrinking below the expected size by more than this also fails: either the measurement broke (a deployment is
    /// down, the service worker no longer precaches) or the app genuinely got smaller and the expected size in the
    /// DataRow should be re-baselined to the measured value the failure message prints.
    /// </summary>
    private const double shrinkTolerance = 0.30;

    /// <summary>
    /// The download is considered complete once the context has started and finished no request for this long.
    /// Long enough to bridge the gaps in a bswup install (page load -> service worker precache -> app start),
    /// short enough that recurring telemetry beacons cannot keep the wait alive forever on their own.
    /// </summary>
    private static readonly TimeSpan networkQuietPeriod = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan measurementDeadline = TimeSpan.FromMinutes(4);

    /// <summary>
    /// First visit with a cold cache: everything the app makes the browser download - the page's own requests plus the
    /// bswup service worker's precache - summed as compressed-over-the-wire bytes. The expected sizes are simply
    /// earlier measurements of this same method, so re-baseline them from the failure message whenever a deliberate
    /// change moves an app's size.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.AdminPanel, 5.3, DisplayName = nameof(DeployedApps.AdminPanel))]
    [DataRow(DeployedApps.AdminPanelWasmStandalone, 4.9, DisplayName = nameof(DeployedApps.AdminPanelWasmStandalone))]
    [DataRow(DeployedApps.Todo, 5.0, DisplayName = nameof(DeployedApps.Todo))]
    [DataRow(DeployedApps.TodoAot, 8.0, DisplayName = nameof(DeployedApps.TodoAot))]
    [DataRow(DeployedApps.TodoSmall, 3.5, DisplayName = nameof(DeployedApps.TodoSmall))]
    [DataRow(DeployedApps.TodoOffline, 7.3, DisplayName = nameof(DeployedApps.TodoOffline))]
    [DataRow(DeployedApps.Sales, 5.4, DisplayName = nameof(DeployedApps.Sales))]
    public async Task FirstVisitDownloadSize_Should_NotRegress(string url, double expectedMegabytes)
    {
        // Qualified because the inherited BrowserType property (an IBrowserType) shadows the type of the same name.
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
    /// Navigates a fresh, empty-cache context to <paramref name="url"/>, waits until the network has been quiet for
    /// <see cref="networkQuietPeriod"/>, and sums the encoded (compressed) body + header bytes of every request the
    /// context made - the service worker's included, thanks to the PW_EXPERIMENTAL_SERVICE_WORKER_NETWORK_EVENTS flag
    /// .runsettings sets.
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

        var deadline = DateTimeOffset.UtcNow + measurementDeadline;

        while (DateTimeOffset.UtcNow < deadline)
        {
            var lastActivity = new DateTimeOffset(Interlocked.Read(ref lastActivityTicks), TimeSpan.Zero);

            if (DateTimeOffset.UtcNow - lastActivity >= networkQuietPeriod)
                break;

            await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.CancellationToken);
        }

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
                // A request that never finished (cancelled, or still in flight past the deadline) has no sizes; the
                // bytes it did move are not part of what the app needs, so skipping it is the right call.
            }
        }

        return (totalBytes, requestCount);
    }
}
