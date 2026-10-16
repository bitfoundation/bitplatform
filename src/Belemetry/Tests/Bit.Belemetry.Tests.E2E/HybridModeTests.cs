using Bit.Belemetry.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Belemetry.Tests.E2E;

/// <summary>
/// Blazor Hybrid: the harness inside a WinForms BlazorWebView (the same WebView core MAUI uses), driven over CDP. What
/// differs from the web: the transport defaults to Relay - the script hands its records to .NET over JS interop and the
/// app process exports them with its own - and consent is app-wide, shared by the script and the .NET pipeline.
/// </summary>
/// <remarks>
/// Windows only. The tests share the app's single window and run in order; each starts by navigating the WebView, which
/// reloads the page and gives the test a fresh DI scope. The app exports to the receiver of the static SSR web host.
/// </remarks>
[TestClass]
public class HybridModeTests : HarnessTest
{
    private CollectorClient? _collector;

    protected override string HostName => "hybrid";

    protected override string BaseUrl => HybridHarnessHost.AppOrigin;

    protected override CollectorClient Collector => _collector ?? throw new InvalidOperationException("The page is opened in TestInitialize.");

    protected override async Task<IPage> OpenPageAsync()
    {
        if (OperatingSystem.IsWindows() is false)
            Assert.Inconclusive("BlazorWebView renders through WebView2, which needs Windows.");

        var host = await HybridSession.GetAsync();
        _collector = new CollectorClient((await HarnessHosts.GetAsync(HybridSession.CollectorHostMode)).BaseUrl);
        return host.Page;
    }

    // The page belongs to the shared app window; the session closes it at the end of the run.
    protected override Task ClosePageAsync() => Task.CompletedTask;

    private async Task OpenAsync(string path)
    {
        await GotoAsync(path);
        await WaitForInteractiveAsync();
        await WaitForBridgeAsync();
        await WaitForScriptAsync();
    }

    [TestMethod]
    public async Task Browser_records_are_relayed_and_exported_by_the_app_process()
    {
        await OpenAsync("/");
        await Page.Locator("#consent-all").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-granted", "4095");
        var session = await SessionIdAsync();

        await Page.Locator("#named-button").ClickAsync();

        var click = await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "browser.user_interaction" && l.Attributes.GetValueOrDefault("target.id") == "named-button"),
            "the relayed click", session);
        Assert.AreEqual("harness-named-button", click.Attributes.GetValueOrDefault("target.name"));

        // Exported by .NET: the app process's resource (process.pid only exists there), merged with the page's.
        Assert.IsNotNull(click.Resource.GetValueOrDefault("process.pid"), "The click was not exported by the app process.");
        Assert.IsNotNull(click.Resource.GetValueOrDefault("browser.language"), "The relayed record lost the page's resource attributes.");

        // And the same path feeds Azure Monitor. A navigation, not the initial load: the app-wide consent decision
        // persists across runs, so the load may have happened while consent was still revoked by an earlier test.
        await Page.Locator("#nav-second").ClickAsync();
        await Expect(Page.Locator("#page-second")).ToBeVisibleAsync();
        await Collector.WaitForAsync(t => t.Envelopes.FirstOrDefault(e => e.Name == "Microsoft.ApplicationInsights.PageView"), "a relayed PageView envelope", session);
    }

    [TestMethod]
    public async Task DotNet_records_and_IBelemetry_calls_share_the_page_session()
    {
        var marker = NewMarker();
        await OpenAsync($"/?marker={marker}");
        await Page.Locator("#consent-all").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-granted", "4095");
        var session = await SessionIdAsync();

        await Page.Locator("#track-event").ClickAsync();
        await Page.Locator("#log-info").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-actions", "3");

        var record = await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "harness.event"), "the C# TrackEvent", marker: marker);
        Assert.AreEqual(session, record.Attributes.GetValueOrDefault("session.id"));

        var log = await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.Body == $"Harness log {marker}"), "the ILogger record", marker: marker);
        Assert.AreEqual("dotnet", log.Resource.GetValueOrDefault("telemetry.sdk.language"));
        Assert.AreEqual(session, log.Attributes.GetValueOrDefault("session.id"));
    }

    [TestMethod]
    public async Task Revoking_consent_in_DotNet_reaches_the_script()
    {
        await OpenAsync("/");
        await Page.Locator("#consent-all").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-granted", "4095");

        await Page.Locator("#consent-none").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-granted", "0");

        // IBelemetryConsent is app-wide in Hybrid: the decision made in C# is the script's decision too.
        await Page.WaitForFunctionAsync("() => window.BitBelemetry.getContext().decided && window.BitBelemetry.getConsent() === 0");
        Assert.IsNull(await LocalStorageAsync("bit-belemetry-pseudo-id"), "Revoking UserTracking left the pseudo id behind.");
    }
}
