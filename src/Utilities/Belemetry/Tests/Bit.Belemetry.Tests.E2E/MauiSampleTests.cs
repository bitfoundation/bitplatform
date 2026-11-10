using Bit.Belemetry.Tests.E2E.Infrastructure;
using Bit.Belemetry.Tests.Harness.Web.Collector;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Belemetry.Tests.E2E;

/// <summary>
/// The MAUI sample (Samples/Maui) as a Windows app: the Blazor Hybrid host Belemetry documents, run as people copy it,
/// with MAUI's own BlazorWebView rather than the WinForms one <see cref="HybridModeTests"/> drives. The transport is
/// Relay - the script hands its records to .NET over JS interop and the app process exports them with its own - and
/// consent gates everything: the app starts with every feature granted (see <see cref="MauiSampleApp"/>).
/// </summary>
/// <remarks>
/// Windows only, like WebView2 (see <see cref="MauiSampleApp"/>); Android's WebView, WKWebView on iOS and Mac Catalyst
/// have no DevTools endpoint Playwright can drive, so CI only builds the sample for them. The tests share the app's single
/// window and run in order; each starts by navigating the WebView, which reloads the page.
/// </remarks>
[TestClass]
public class MauiSampleTests : HarnessTest
{
    private MauiSampleApp? _app;

    protected override string HostName => "maui";

    protected override string BaseUrl => MauiSampleApp.AppOrigin;

    protected override CollectorClient Collector => App.Collector;

    private MauiSampleApp App => _app ?? throw new InvalidOperationException("The page is opened in TestInitialize.");

    protected override async Task<IPage> OpenPageAsync()
    {
        _app = await MauiSampleApp.SharedAsync();
        return _app.Page;
    }

    // The page belongs to the shared app window; the suite closes it at the end of the run.
    protected override Task ClosePageAsync() => Task.CompletedTask;

    [TestMethod]
    public async Task Browser_records_are_relayed_and_exported_by_the_app_process()
    {
        var session = await OpenWithConsentAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Named click target" }).ClickAsync();

        var click = await Collector.WaitForAsync(t => FromTheApp(t.Logs).FirstOrDefault(l => l.EventName == "browser.user_interaction"
                                                                                             && l.Attributes.GetValueOrDefault("target.name") == "sample-checkout"),
            "the relayed click", session);
        // Exported by .NET: the app process's resource (process.pid only exists there), merged with the page's.
        Assert.IsNotNull(click.Resource.GetValueOrDefault("process.pid"), "The click was not exported by the app process.");
        Assert.IsNotNull(click.Resource.GetValueOrDefault("browser.language"), "The relayed record lost the page's resource attributes.");

        await Page.Locator(".topbar").GetByRole(AriaRole.Link, new() { Name = "Second page" }).ClickAsync();
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Second page");
        var pageView = await Collector.WaitForAsync(t => FromTheApp(t.Logs).FirstOrDefault(l => l.EventName == "browser.page_view"
                                                                                                && l.Attributes.GetValueOrDefault("url.path") == "/second"),
            "the page view of the client-side navigation", session);
        Assert.AreEqual("push", pageView.Attributes.GetValueOrDefault("browser.navigation.type"));
    }

    [TestMethod]
    public async Task A_fetch_and_a_JS_error_in_the_WebView_are_exported()
    {
        // Thrown on purpose, from a timer, so it is uncaught.
        AllowConsoleError("A sample JS error.");
        var session = await OpenWithConsentAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "fetch sample-data.json" }).ClickAsync();
        await Expect(Page.Locator(".status")).ToHaveTextAsync("fetch sample-data.json: 200.");
        // The script's privacy scrubbing replaces the IP address of BlazorWebView's origin (0.0.0.1) in the URL.
        var span = await Collector.WaitForAsync(t => FromTheApp(t.Spans).FirstOrDefault(s => s.Attributes.GetValueOrDefault("url.full")?.EndsWith("/sample-data.json", StringComparison.Ordinal) is true),
            "the fetch client span", session);
        Assert.AreEqual("200", span.Attributes.GetValueOrDefault("http.response.status_code"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Throw a JS error" }).ClickAsync();
        var error = await Collector.WaitForAsync(t => FromTheApp(t.Logs).FirstOrDefault(l => l.EventName == "exception"
                                                                                             && l.Attributes.GetValueOrDefault("exception.message")?.Contains("A sample JS error.") is true),
            "the JS error", session);
        Assert.AreEqual("Error", error.Attributes.GetValueOrDefault("exception.type"));
    }

    [TestMethod]
    public async Task DotNet_records_of_the_app_process_share_the_page_session()
    {
        var session = await OpenWithConsentAsync();

        await ClickAndWaitForStatusAsync("TrackEvent", "TrackEvent sent.");
        await ClickAndWaitForStatusAsync("TrackException", "TrackException sent.");
        await ClickAndWaitForStatusAsync("ILogger log", "ILogger log written.");
        await ClickAndWaitForStatusAsync("Activity span", "Activity span recorded.");
        await ClickAndWaitForStatusAsync("Meter measurement", "Meter measurement recorded (exported with the next metrics interval).");
        // Exports the measurement now rather than at the sample's 60 second metrics interval.
        await ClickAndWaitForStatusAsync("FlushAsync", "Flushed.");

        var trackedEvent = await Collector.WaitForAsync(t => FromTheApp(t.Logs).FirstOrDefault(l => l.EventName == "sample.button_clicked"), "the C# TrackEvent", session);
        Assert.AreEqual("track-event", trackedEvent.Attributes.GetValueOrDefault("sample.button"));

        var exception = await Collector.WaitForAsync(t => FromTheApp(t.Logs).FirstOrDefault(l => l.Attributes.GetValueOrDefault("sample.button") == "track-exception"),
            "the C# TrackException", session);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, exception.Attributes.GetValueOrDefault("exception.type"));

        var log = await Collector.WaitForAsync(t => FromTheApp(t.Logs).FirstOrDefault(l => l.Body?.StartsWith("The sample log button was clicked", StringComparison.Ordinal) is true),
            "the ILogger record", session);
        Assert.AreEqual("dotnet", log.Resource.GetValueOrDefault("telemetry.sdk.language"));

        var activity = await Collector.WaitForAsync(t => FromTheApp(t.Spans).FirstOrDefault(s => s.Name == "sample.work"), "the Activity span", session);
        Assert.AreEqual("activity-span", activity.Attributes.GetValueOrDefault("sample.button"));

        // A metric point carries no session: this run of the app is told apart by its random service.instance.id.
        // Waited for longer: should the flush fail past its retries, the stored request's retry or the next metrics
        // export, each up to a minute later, delivers the measurement.
        var instance = log.Resource.GetValueOrDefault("service.instance.id");
        Assert.IsNotNull(instance);
        var metric = await Collector.WaitForAsync(t => FromTheApp(t.Metrics).FirstOrDefault(m => m.Name == "samples.button.clicks"
                                                                                                 && m.Resource.GetValueOrDefault("service.instance.id") == instance),
            "the Meter measurement", timeout: TimeSpan.FromMinutes(2));
        Assert.AreEqual("sum", metric.Type);
    }

    /// <summary>
    /// Reloads the home page and waits for the script to hold the decision granting every feature, which the app
    /// restored from the consent file the suite wrote (see <see cref="MauiSampleApp"/>). Returns the page's session id.
    /// </summary>
    private async Task<string> OpenWithConsentAsync()
    {
        await GotoAsync("/");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Belemetry samples", new() { Timeout = 120_000 });
        // <BelemetryInitializer /> starts the script once the app is interactive, then hands it the decision of .NET.
        await WaitForScriptAsync();

        await Page.WaitForFunctionAsync("() => window.BitBelemetry.getContext().decided && window.BitBelemetry.getConsent() === 4095");
        await Expect(Page.Locator(".consent-banner")).ToBeHiddenAsync();
        return await SessionIdAsync();
    }

    private async Task ClickAndWaitForStatusAsync(string button, string status)
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = button, Exact = true }).ClickAsync();
        await Expect(Page.Locator(".status")).ToHaveTextAsync(status);
    }

    // The receiver is the static SSR host's, which its own suite and the hybrid one export to as well.
    private static IEnumerable<CollectedLog> FromTheApp(IEnumerable<CollectedLog> logs) => logs.Where(l => IsTheApp(l.Resource));

    private static IEnumerable<CollectedSpan> FromTheApp(IEnumerable<CollectedSpan> spans) => spans.Where(s => IsTheApp(s.Resource));

    private static IEnumerable<CollectedMetric> FromTheApp(IEnumerable<CollectedMetric> metrics) => metrics.Where(m => IsTheApp(m.Resource));

    private static bool IsTheApp(Dictionary<string, string?> resource) => resource.GetValueOrDefault("service.name") == MauiSampleApp.ServiceName;
}
