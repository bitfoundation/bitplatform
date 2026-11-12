using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
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

    private protected override CollectorClient Collector => _collector ?? throw new InvalidOperationException("The page is opened in TestInitialize.");

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
    public async Task Errors_reach_Sentry_from_the_app_process_with_their_frames()
    {
        var marker = NewMarker();
        await OpenAsync($"/?marker={marker}");
        await Page.Locator("#consent-all").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-granted", "4095");
        var session = await SessionIdAsync();

        // A C# exception, from the exception object: each app frame carries the method's row and IL offset, and the event
        // the debug image of the assembly file it ran from, for Sentry's symbolicator.
        await Page.Locator("#track-exception").ClickAsync();
        await Expect(Page.Locator("#consent-state")).ToHaveAttributeAsync("data-actions", "1");
        var dotnet = await Collector.WaitForAsync(t => t.SentryEvents.FirstOrDefault(), "the Sentry event of the C# TrackException", marker: marker);
        Assert.AreEqual("csharp", dotnet.Platform);
        Assert.AreEqual("bit.belemetry.dotnet", dotnet.SdkName);
        Assert.IsTrue(dotnet.Handled);
        Assert.Contains(f => f.EndsWith("HomePage.TrackException", StringComparison.Ordinal), dotnet.Frames, string.Join(" | ", dotnet.Frames));

        using var json = System.Text.Json.JsonDocument.Parse(dotnet.Json!);
        var images = json.RootElement.GetProperty("debug_meta").GetProperty("images").EnumerateArray().ToList();
        var harness = images.Single(i => i.GetProperty("code_file").GetString()!.EndsWith("Bit.Belemetry.Tests.Harness.dll", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("pe_dotnet", harness.GetProperty("type").GetString());
        Assert.MatchesRegex(new System.Text.RegularExpressions.Regex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}-[0-9a-f]+$"), harness.GetProperty("debug_id").GetString());
        var frame = json.RootElement.GetProperty("exception").GetProperty("values")[0].GetProperty("stacktrace").GetProperty("frames").EnumerateArray()
            .Single(f => f.GetProperty("function").GetString() == "TrackException");
        Assert.StartsWith("0x", frame.GetProperty("function_id").GetString());
        Assert.StartsWith("0x", frame.GetProperty("instruction_addr").GetString());
        Assert.AreEqual($"rel:{images.IndexOf(harness)}", frame.GetProperty("addr_mode").GetString());

        // What the symbolicator will look up, read back from the assembly file without Belemetry: the image is that build's
        // PDB id, the frame's row is the method itself, and its IL offset is inside the method's body.
        using (var pe = new PEReader(File.OpenRead(harness.GetProperty("code_file").GetString()!)))
        {
            var codeView = pe.ReadDebugDirectory().First(e => e.Type == DebugDirectoryEntryType.CodeView);
            var pdb = pe.ReadCodeViewDebugDirectoryData(codeView);
            Assert.AreEqual(codeView.IsPortableCodeView ? $"{pdb.Guid}-{codeView.Stamp:x8}" : $"{pdb.Guid}-{pdb.Age:x}", harness.GetProperty("debug_id").GetString());

            var metadata = pe.GetMetadataReader();
            var row = Convert.ToInt32(frame.GetProperty("function_id").GetString(), 16);
            Assert.IsGreaterThan(0, row);
            var method = metadata.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(row));
            Assert.AreEqual("TrackException", metadata.GetString(method.Name));
            var offset = Convert.ToInt32(frame.GetProperty("instruction_addr").GetString(), 16);
            Assert.IsLessThan(pe.GetMethodBody(method.RelativeVirtualAddress).Size, offset);
        }

        // A JS error, relayed to .NET: the C# exporter parses its stack as the script would.
        AllowConsoleError("Harness JS error");
        await Page.Locator("#js-error").ClickAsync();
        var js = await Collector.WaitForAsync(t => t.SentryEvents.FirstOrDefault(e => e.Exceptions.Any(x => x.Contains("Harness JS error"))), "the relayed JS error's Sentry event", session);
        Assert.AreEqual("javascript", js.Platform);
        Assert.AreEqual("bit.belemetry.javascript", js.SdkName, "The record is the page's, whoever exports it.");
        Assert.IsFalse(js.Handled);
        Assert.Contains(f => f.Contains("harness.js", StringComparison.Ordinal), js.Frames, string.Join(" | ", js.Frames));
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
