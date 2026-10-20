using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Belemetry.Tests.E2E.Infrastructure;
using Bit.Belemetry.Tests.Harness.Web.Collector;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Belemetry.Tests.E2E;

/// <summary>
/// The browser script in one render mode of the web harness host, each test in a browser context of its own (fresh
/// storage, so no consent decision carried over). What the script records does not depend on Blazor's render mode -
/// <c>&lt;BelemetryScript /&gt;</c> starts it from the prerendered head in every one - so every mode runs these.
/// </summary>
public abstract partial class WebModeTests : HarnessTest
{
    private readonly WebSession _session = new();
    private CollectorClient? _collector;

    /// <summary>The BelemetryHarness:Mode the host is started with.</summary>
    protected abstract string Mode { get; }

    /// <summary>Whether the mode has a .NET runtime in the page (and so event handlers to wait for).</summary>
    protected abstract bool IsInteractive { get; }

    protected override string HostName => Mode;

    protected override string BaseUrl => _session.Host.BaseUrl;

    // The host is its own receiver: the browser exports same-origin.
    protected override CollectorClient Collector => _collector ??= new CollectorClient(_session.Host.BaseUrl);

    protected override Task<IPage> OpenPageAsync() => _session.OpenAsync(Mode);

    protected override Task ClosePageAsync() => _session.CloseAsync();

    /// <summary>Loads <paramref name="path"/> and waits for the script (and, when the mode has one, the .NET bridge).</summary>
    protected async Task OpenAsync(string path)
    {
        await GotoAsync(path);
        await WaitForScriptAsync();
        if (IsInteractive)
        {
            await WaitForInteractiveAsync();
            await WaitForBridgeAsync();
        }
    }

    [TestMethod]
    public async Task Nothing_is_sent_before_consent_and_granting_sends_the_held_records()
    {
        await OpenAsync("/");
        var session = await SessionIdAsync();
        Assert.IsFalse((await ContextAsync()).Decided, "A fresh browser context already had a consent decision.");

        await Page.Locator("#named-button").ClickAsync();
        await FlushScriptAsync();
        // Several flush intervals (500 ms): anything not held would have been exported by now.
        await Task.Delay(TimeSpan.FromSeconds(2.5));

        var before = await Collector.ReceivedAsync(session);
        Assert.AreEqual(0, before.Logs.Count + before.Spans.Count + before.Envelopes.Count,
            $"Telemetry was sent before the user decided. {CollectorClient.Describe(before)}");

        await Page.Locator("#js-consent-all").ClickAsync();

        // The records held until the decision: the initial page view and the click from before it.
        await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "browser.page_view" && l.Attributes.GetValueOrDefault("browser.navigation.type") == "initial"),
            "the held initial page view", session);
        await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "browser.user_interaction" && l.Attributes.GetValueOrDefault("target.name") == "harness-named-button"),
            "the click held from before the decision", session);
    }

    [TestMethod]
    public async Task Revoking_consent_stops_sending_and_clears_the_stored_ids()
    {
        await OpenAsync("/?consent=all");
        var session = await SessionIdAsync();
        await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "browser.page_view"), "a page view after granting consent", session);

        // Granted Sessions and UserTracking keep their ids on the device.
        Assert.IsNotNull(await SessionStorageAsync("bit-belemetry-session"), "The session was not stored with Sessions granted.");
        Assert.IsNotNull(await LocalStorageAsync("bit-belemetry-pseudo-id"), "The pseudo id was not stored with UserTracking granted.");
        Assert.IsNotNull((await ContextAsync()).PseudoId);

        await Page.Locator("#js-consent-none").ClickAsync();

        Assert.IsNull(await SessionStorageAsync("bit-belemetry-session"), "Revoking Sessions left the stored session behind.");
        Assert.IsNull(await LocalStorageAsync("bit-belemetry-pseudo-id"), "Revoking UserTracking left the pseudo id behind.");
        Assert.IsNull(await SessionStorageAsync("bit-belemetry-user-id"), "Revoking UserTracking left the user id behind.");

        // The choice itself is strictly necessary and stays.
        using (var consent = JsonDocument.Parse(await LocalStorageAsync("bit-belemetry-consent") ?? "null"))
        {
            Assert.AreEqual(JsonValueKind.Object, consent.RootElement.ValueKind, "The consent decision was not stored.");
            Assert.AreEqual(1, consent.RootElement.GetProperty("v").GetInt32());
            Assert.AreEqual(0, consent.RootElement.GetProperty("features").GetInt32());
        }

        var context = await ContextAsync();
        Assert.AreEqual(0, context.Consent);
        Assert.IsTrue(context.Decided);
        Assert.IsNull(context.PseudoId);

        await Page.Locator("#named-button").ClickAsync();
        await Page.EvaluateAsync("() => window.BitBelemetry.trackEvent('harness.after-revoke')");
        await FlushScriptAsync();
        await Task.Delay(TimeSpan.FromSeconds(2.5));

        foreach (var id in new[] { session, context.SessionId! }.Distinct())
        {
            var after = await Collector.ReceivedAsync(id);
            Assert.IsFalse(after.Logs.Any(l => l.Attributes.GetValueOrDefault("target.name") == "harness-named-button"), "A click was sent after consent was revoked.");
            Assert.IsFalse(after.Logs.Any(l => l.EventName == "harness.after-revoke"), "A custom event was sent after consent was revoked.");
        }
    }

    [TestMethod]
    public async Task The_initial_load_and_every_navigation_are_page_views()
    {
        await OpenAsync("/?consent=all");
        var session = await SessionIdAsync();
        // Blazor intercepts links (client-side or enhanced navigation) once it has started.
        await Page.WaitForFunctionAsync("() => !!window.Blazor");
        await Page.EvaluateAsync("() => window.__harnessNoReload = true");

        await Page.Locator("#nav-second").ClickAsync();
        await Expect(Page.Locator("#page-second")).ToBeVisibleAsync();
        await Page.Locator("#nav-third").ClickAsync();
        await Expect(Page.Locator("#page-third")).ToBeVisibleAsync();

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__harnessNoReload === true"), "The navigation reloaded the page.");

        var initial = await Collector.WaitForAsync(t => PageView(t, "/"), "the initial page view", session);
        Assert.AreEqual("initial", initial.Attributes.GetValueOrDefault("browser.navigation.type"));
        StringAssert.StartsWith(initial.Attributes.GetValueOrDefault("url.full"), BaseUrl + "/");
        Assert.AreEqual("Belemetry harness (" + Mode + ")", initial.Attributes.GetValueOrDefault("page.title"));

        var second = await Collector.WaitForAsync(t => PageView(t, "/second"), "the page view of /second", session);
        Assert.AreEqual("push", second.Attributes.GetValueOrDefault("browser.navigation.type"));

        var third = await Collector.WaitForAsync(t => PageView(t, "/third"), "the page view of /third", session);
        Assert.AreEqual("push", third.Attributes.GetValueOrDefault("browser.navigation.type"));
        Assert.AreEqual(BaseUrl + "/second", third.Attributes.GetValueOrDefault("page.referrer"));

        // Each navigation counts once, however many history calls and enhanced loads it took.
        var all = await Collector.ReceivedAsync(session);
        Assert.AreEqual(1, all.Logs.Count(l => l.EventName == "browser.page_view" && l.Attributes.GetValueOrDefault("url.path") == "/second"),
            "The navigation to /second was counted more than once.");

        // Browser records carry the browser's resource, and the session on every record.
        Assert.AreEqual("webjs", initial.Resource.GetValueOrDefault("telemetry.sdk.language"));
        Assert.AreEqual("belemetry-harness", initial.Resource.GetValueOrDefault("service.name"));
        Assert.IsNotNull(initial.Attributes.GetValueOrDefault("enduser.pseudo.id"), "A record carried no pseudo id with UserTracking granted.");
    }

    [TestMethod]
    public async Task A_click_on_a_named_element_is_recorded_and_an_ignored_subtree_is_not()
    {
        await OpenAsync("/?consent=all");
        var session = await SessionIdAsync();

        await Page.Locator("#ignored-button").ClickAsync();
        await Page.Locator("#named-button").ClickAsync();

        var click = await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "browser.user_interaction" && l.Attributes.GetValueOrDefault("target.id") == "named-button"),
            "the click on #named-button", session);
        Assert.AreEqual("harness-named-button", click.Attributes.GetValueOrDefault("target.name"));
        Assert.AreEqual("click", click.Attributes.GetValueOrDefault("event.type"));
        Assert.AreEqual("button", click.Attributes.GetValueOrDefault("target.element"));
        Assert.AreEqual("//*[@id=\"named-button\"]", click.Attributes.GetValueOrDefault("target.xpath"));
        // The element's text is never captured unless CaptureInteractionText is on.
        Assert.IsFalse(click.Attributes.ContainsKey("target.text"), "The click captured the element's text.");

        // The ignored click came first, so it would be in the same or an earlier batch.
        var all = await Collector.ReceivedAsync(session);
        Assert.IsFalse(all.Logs.Any(l => l.Attributes.GetValueOrDefault("target.id") == "ignored-button"), "A click inside [data-bit-belemetry-ignore] was recorded.");
    }

    [TestMethod]
    public async Task An_uncaught_JS_error_becomes_an_exception_record()
    {
        AllowConsoleError("Harness JS error");
        await OpenAsync("/?consent=all");
        var session = await SessionIdAsync();

        await Page.Locator("#js-error").ClickAsync();

        var error = await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "exception" && l.Attributes.GetValueOrDefault("exception.message")?.Contains("Harness JS error") is true),
            "the JS error", session);
        Assert.AreEqual(17, error.SeverityNumber);
        Assert.AreEqual("Error", error.Attributes.GetValueOrDefault("exception.type"));
        StringAssert.Contains(error.Attributes.GetValueOrDefault("exception.stacktrace"), "harness.js");

        // And the Azure Monitor exporter turned it into an Exception envelope.
        await Collector.WaitForAsync(t => t.Envelopes.FirstOrDefault(e => e.Name == "Microsoft.ApplicationInsights.Exception" && e.BaseData?.Contains("Harness JS error") is true),
            "the Exception envelope", session);
    }

    [TestMethod]
    public async Task A_same_origin_fetch_is_a_client_span_whose_traceparent_reaches_the_server()
    {
        await OpenAsync("/?consent=all");
        var session = await SessionIdAsync();

        await Page.Locator("#js-fetch").ClickAsync();
        var result = Page.Locator("#fetch-result");
        await Expect(result).ToHaveAttributeAsync("data-done", "true");
        await Expect(result).ToHaveAttributeAsync("data-status", "200");

        // What the server saw (the echo endpoint returns the request headers).
        var traceparent = await result.GetAttributeAsync("data-traceparent");
        var match = TraceparentPattern().Match(traceparent ?? string.Empty);
        Assert.IsTrue(match.Success, $"The same-origin request carried no valid traceparent header: '{traceparent}'.");

        var span = await Collector.WaitForAsync(t => t.Spans.FirstOrDefault(s => s.Attributes.GetValueOrDefault("url.full")?.Contains("/test-api/echo") is true),
            "the fetch client span", session);
        Assert.AreEqual(3, span.Kind, "The fetch span is not a CLIENT span.");
        Assert.AreEqual("GET", span.Name);
        Assert.AreEqual("GET", span.Attributes.GetValueOrDefault("http.request.method"));
        Assert.AreEqual("200", span.Attributes.GetValueOrDefault("http.response.status_code"));
        Assert.AreEqual("localhost", span.Attributes.GetValueOrDefault("server.address"));

        // The header is the span's own context: the server's span would be its child.
        Assert.AreEqual(span.TraceId, match.Groups["trace"].Value);
        Assert.AreEqual(span.SpanId, match.Groups["span"].Value);

        // The exports themselves are never traced.
        var all = await Collector.ReceivedAsync(session);
        Assert.IsFalse(all.Spans.Any(s => s.Attributes.GetValueOrDefault("url.full")?.Contains("/test-collector") is true), "An export request was traced.");
    }

    [TestMethod]
    public async Task A_cross_origin_fetch_carries_traceparent_when_its_origin_is_listed_and_the_api_allows_the_header()
    {
        await OpenAsync("/?consent=all");
        var session = await SessionIdAsync();

        // The host under its other name is another origin to the browser: a real cross-origin API call, answered
        // with a CORS policy that allows the traceparent header, as any API behind another origin must.
        await Page.Locator("#js-fetch-cross-origin").ClickAsync();
        var result = Page.Locator("#fetch-cross-origin-result");
        await Expect(result).ToHaveAttributeAsync("data-done", "true");
        Assert.IsNull(await result.GetAttributeAsync("data-error"), $"The cross-origin request failed: {await result.GetAttributeAsync("data-error")}");
        await Expect(result).ToHaveAttributeAsync("data-status", "200");

        var traceparent = await result.GetAttributeAsync("data-traceparent");
        var match = TraceparentPattern().Match(traceparent ?? string.Empty);
        Assert.IsTrue(match.Success, $"The cross-origin request carried no valid traceparent header: '{traceparent}'.");

        var span = await Collector.WaitForAsync(t => t.Spans.FirstOrDefault(s => s.Attributes.GetValueOrDefault("url.full")?.Contains("from=harness-cross-origin") is true),
            "the cross-origin fetch client span", session);
        Assert.AreEqual(3, span.Kind, "The fetch span is not a CLIENT span.");
        Assert.AreEqual("200", span.Attributes.GetValueOrDefault("http.response.status_code"));
        Assert.AreEqual(span.TraceId, match.Groups["trace"].Value);
        Assert.AreEqual(span.SpanId, match.Groups["span"].Value);
    }

    [TestMethod]
    public async Task Web_vitals_are_reported_when_the_page_is_hidden()
    {
        await OpenAsync("/?consent=all");
        var session = await SessionIdAsync();

        // TTFB and FCP are final as soon as they happen, so they arrive without the page being hidden. Only paints from
        // before the page was first hidden count, so the test waits for the first paint (which can take a while with
        // every suite's browser context sharing one Chromium) before hiding the page.
        foreach (var name in new[] { "TTFB", "FCP" })
        {
            AssertValid(await Collector.WaitForAsync(t => Vital(t, name), $"the {name} web vital", session), name);
        }

        // LCP and CLS are final when the page is hidden (LCP also at the first input).
        await HidePageAsync();

        foreach (var name in new[] { "LCP", "CLS" })
        {
            AssertValid(await Collector.WaitForAsync(t => Vital(t, name), $"the {name} web vital", session), name);
        }

        // Azure Monitor gets them as metrics.
        await Collector.WaitForAsync(t => t.Envelopes.FirstOrDefault(e => e.Name == "Microsoft.ApplicationInsights.Metric" && e.BaseData?.Contains("\"LCP\"") is true),
            "the LCP Metric envelope", session);
    }

    [TestMethod]
    public async Task A_page_view_reaches_Azure_Monitor_as_a_PageView_envelope()
    {
        await OpenAsync("/?consent=all");
        var context = await ContextAsync();

        var envelope = await Collector.WaitForAsync(t => t.Envelopes.FirstOrDefault(e => e.Name == "Microsoft.ApplicationInsights.PageView"),
            "a PageView envelope", context.SessionId);

        Assert.AreEqual("PageViewData", envelope.BaseType);
        Assert.AreEqual("11111111-2222-3333-4444-555555555555", envelope.IKey);
        Assert.AreEqual(context.PseudoId, envelope.Tags.GetValueOrDefault("ai.user.id"));
        Assert.AreEqual("belemetry-harness", envelope.Tags.GetValueOrDefault("ai.cloud.role"));
        Assert.AreEqual("Browser", envelope.Tags.GetValueOrDefault("ai.device.type"));
        StringAssert.StartsWith(envelope.Tags.GetValueOrDefault("ai.internal.sdkVersion"), "bitblmjs:");
        StringAssert.Contains(envelope.BaseData, BaseUrl + "/");
    }

    private static CollectedLog? Vital(CollectedTelemetry received, string name) =>
        received.Logs.FirstOrDefault(l => l.EventName == "browser.web_vital" && l.Attributes.GetValueOrDefault("web_vital.name") == name);

    private static void AssertValid(CollectedLog vital, string name)
    {
        Assert.IsTrue(double.TryParse(vital.Attributes.GetValueOrDefault("web_vital.value"), System.Globalization.CultureInfo.InvariantCulture, out var value) && value >= 0,
            $"{name} has no valid value.");
        CollectionAssert.Contains(new[] { "good", "needs-improvement", "poor" }, vital.Attributes.GetValueOrDefault("web_vital.rating"));
    }

    private static CollectedLog? PageView(CollectedTelemetry received, string path) =>
        received.Logs.FirstOrDefault(l => l.EventName == "browser.page_view" && l.Attributes.GetValueOrDefault("url.path") == path);

    [GeneratedRegex("^00-(?<trace>[0-9a-f]{32})-(?<span>[0-9a-f]{16})-0[0-9a-f]$")]
    private static partial Regex TraceparentPattern();
}

/// <summary>A mode with a .NET runtime in the page: <c>IBelemetry</c> calls from C#.</summary>
public abstract class InteractiveWebModeTests : WebModeTests
{
    protected override bool IsInteractive => true;

    /// <summary>
    /// <c>telemetry.sdk.language</c> of a C# <c>TrackEvent</c>'s record: "webjs" where it is routed through the browser
    /// (Server), "dotnet" where the .NET pipeline in the page exports it (WebAssembly), null when either may apply.
    /// </summary>
    protected abstract string? TrackEventSdkLanguage { get; }

    [TestMethod]
    public async Task IBelemetry_TrackEvent_from_CSharp_is_exported()
    {
        var marker = NewMarker();
        await OpenAsync($"/?consent=all&marker={marker}");

        await Page.Locator("#track-event").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-actions", "1");

        var record = await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.EventName == "harness.event"), "the C# TrackEvent", marker: marker);
        Assert.AreEqual("track-event", record.Attributes.GetValueOrDefault("harness.action"));
        if (TrackEventSdkLanguage is { } language)
        {
            Assert.AreEqual(language, record.Resource.GetValueOrDefault("telemetry.sdk.language"));
        }

        // Either way it belongs to the browser session.
        Assert.AreEqual(await SessionIdAsync(), record.Attributes.GetValueOrDefault("session.id"));
    }
}

[TestClass]
public class StaticSsrModeTests : WebModeTests
{
    protected override string Mode => "ssr";

    protected override bool IsInteractive => false;
}

[TestClass]
public class ServerModeTests : InteractiveWebModeTests
{
    protected override string Mode => "server";

    protected override string? TrackEventSdkLanguage => "webjs";
}

[TestClass]
public class WebAssemblyModeTests : InteractiveWebModeTests
{
    protected override string Mode => "wasm";

    protected override string? TrackEventSdkLanguage => "dotnet";

    [TestMethod]
    public async Task DotNet_ILogger_and_Activity_records_are_exported_from_the_page()
    {
        var marker = NewMarker();
        await OpenAsync($"/?consent=all&marker={marker}");
        var session = await SessionIdAsync();

        await Page.Locator("#log-info").ClickAsync();
        await Page.Locator("#start-activity").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-actions", "2");

        var log = await Collector.WaitForAsync(t => t.Logs.FirstOrDefault(l => l.Body == $"Harness log {marker}"), "the ILogger record", marker: marker);
        Assert.AreEqual("Bit.Belemetry.Tests.Harness.Pages.HomePage", log.Scope, "ILogger<T> of an app type under Bit.Belemetry.* is collected.");
        Assert.AreEqual(9, log.SeverityNumber);
        Assert.AreEqual("dotnet", log.Resource.GetValueOrDefault("telemetry.sdk.language"));
        Assert.AreEqual("browser", log.Resource.GetValueOrDefault("os.type"));
        // The .NET pipeline in the page shares the script's session.
        Assert.AreEqual(session, log.Attributes.GetValueOrDefault("session.id"));

        var span = await Collector.WaitForAsync(t => t.Spans.FirstOrDefault(s => s.Name == "harness.activity"), "the Activity span", marker: marker);
        Assert.AreEqual("Bit.Belemetry.Tests.Harness", span.Scope);
        Assert.AreEqual(session, span.Attributes.GetValueOrDefault("session.id"));
    }

    [TestMethod]
    public async Task DotNet_Meter_measurements_are_exported_from_the_page()
    {
        // The WebAssembly SDK turns System.Diagnostics.Metrics off by default and the package's build props turn it
        // back on (the harness client imports them, as a PackageReference would). Without them every Meter in the
        // page is inert, and this measurement never leaves it - while logs and traces still flow.
        var marker = NewMarker();
        await OpenAsync($"/?consent=all&marker={marker}");

        await Page.Locator("#record-metric").ClickAsync();
        await Page.Locator("#flush").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-actions", "2");

        var metric = await Collector.WaitForAsync(t => t.Metrics.FirstOrDefault(m => m.Name == "harness.clicks"), "the Meter measurement", marker: marker);
        Assert.AreEqual("Bit.Belemetry.Tests.Harness", metric.Scope);
        Assert.AreEqual("sum", metric.Type);
        Assert.AreEqual("dotnet", metric.Resource.GetValueOrDefault("telemetry.sdk.language"));
        Assert.AreEqual("browser", metric.Resource.GetValueOrDefault("os.type"));

        // And the Azure Monitor exporter turned it into a Metric envelope.
        await Collector.WaitForAsync(t => t.Envelopes.FirstOrDefault(e => e.Name == "Microsoft.ApplicationInsights.Metric" && e.BaseData?.Contains("harness.clicks") is true),
            "the Metric envelope", marker: marker);
    }
}

[TestClass]
public class AutoModeTests : InteractiveWebModeTests
{
    protected override string Mode => "auto";

    // The first visit runs on a circuit while WebAssembly downloads; a later one may already run in the browser.
    protected override string? TrackEventSdkLanguage => null;
}
