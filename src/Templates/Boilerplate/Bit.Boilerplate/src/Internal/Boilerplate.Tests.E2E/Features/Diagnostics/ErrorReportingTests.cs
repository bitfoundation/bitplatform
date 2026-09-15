using System.Collections.Concurrent;
using System.IO.Compression;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Diagnostics;

/// <summary>The diagnostic modal's test error, as the error tracker's ingestion endpoint receives it.</summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class ErrorReportingTests : AppTestBase
{
    /// <summary>AppDiagnosticModal.ThrowTestException alternates between these two.</summary>
    private static readonly Regex testError = new(@"Something (bad|critical) happened\.");

    protected override IAppOpener AppOpener => new WebAppOpener();

    [TestMethod]
    [DataRow(App.AdminPanel, "applicationinsights.azure.com/v2/track", DisplayName = "AdminPanel (Application Insights)")]
    [DataRow(App.AdminPanelWasmStandalone, ".sentry.io/api/", DisplayName = "AdminPanel WASM standalone (Sentry)")]
    // Sales redirects to a culture url, where a script placed before <base> resolved under /en-US/.
    [DataRow(App.Sales, "applicationinsights.azure.com/v2/track", DisplayName = "Sales (Application Insights)")]
    public async Task TheDiagnosticTestError_Should_ReachTheErrorTracker(App app, string ingestionUrl)
    {
        var sent = new ConcurrentQueue<(int Status, string Body)>();
        var failedAssets = new ConcurrentQueue<string>();

        Context.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.Contains("_content", StringComparison.OrdinalIgnoreCase))
                failedAssets.Enqueue(response.Url);

            if (response.Url.Contains(ingestionUrl, StringComparison.OrdinalIgnoreCase) is false || response.Request.PostDataBuffer is not { Length: > 0 } body)
                return;

            sent.Enqueue((response.Status, Decode(body)));
        };

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        var throwTestError = page.Locator("button[title='Throw test error']");
        await OpenDiagnosticModal(page, throwTestError);
        await throwTestError.ClickAsync();

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        while (sent.Any(s => testError.IsMatch(s.Body)) is false && DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                // App Insights batches for up to 15 seconds otherwise.
                await page.EvaluateAsync("() => window.appInsights?.flush?.()");
            }
            catch (PlaywrightException)
            {
                // The unhandled error can take the page through a reload.
            }

            await page.WaitForTimeoutAsync(500);
        }

        var reports = sent.Where(s => testError.IsMatch(s.Body)).ToArray();
        Assert.IsNotEmpty(reports, $"{app} sent the test error in none of its {sent.Count} request(s) to {ingestionUrl}.");
        Assert.Contains(r => r.Status is 200, reports, $"{ingestionUrl} refused the test error: {string.Join(", ", reports.Select(r => r.Status))}.");

        Assert.IsEmpty(failedAssets, $"{app} failed to load: {string.Join(", ", failedAssets)}");
    }

    /// <summary>App.showDiagnostic reaches .NET only once the app has booted, so it is called until the modal opens.</summary>
    private static async Task OpenDiagnosticModal(IPage page, ILocator throwTestError)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        while (true)
        {
            await page.EvaluateAsync("() => window.App?.showDiagnostic?.()");

            try
            {
                await throwTestError.WaitForAsync(new() { Timeout = 5_000 });
                return;
            }
            catch (TimeoutException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
    }

    /// <summary>Sentry gzips its envelopes.</summary>
    private static string Decode(byte[] body)
    {
        Stream stream = new MemoryStream(body);

        if (body is [0x1f, 0x8b, ..])
            stream = new GZipStream(stream, CompressionMode.Decompress);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
