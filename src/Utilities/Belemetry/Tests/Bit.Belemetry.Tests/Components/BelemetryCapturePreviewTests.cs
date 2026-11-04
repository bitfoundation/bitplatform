using Bunit;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Belemetry.Tests.Components;

/// <summary>The preview a user confirms before a bundle leaves the app, driven through a real renderer.</summary>
[TestClass]
public class BelemetryCapturePreviewTests
{
    private static async Task<(BunitContext Context, BelemetryDiagnosticCapture Capture)> CreateAsync(Action<BunitContext>? setup = null)
    {
        var context = new BunitContext();
        context.Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
        context.Services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Consent.Default = BelemetryFeatures.All;
            o.Diagnostics.Enabled = true;
            o.AddExporter(new BelemetryInMemoryExporter());
        });
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        setup?.Invoke(context);

        var logger = context.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Orders");
        context.Services.GetRequiredService<BelemetryPipeline>().Start();
        logger.LogWarning("Inventory <b>slow</b>");
        try
        {
            throw new InvalidOperationException("<script>alert(1)</script>");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saving failed");
        }

        var capture = await context.Services.GetRequiredService<IBelemetryDiagnostics>().CaptureAsync(new() { IncludeBrowserEvidence = false });
        return (context, capture);
    }

    [TestMethod]
    public async Task ItShowsWhatWillBeSent_AsText()
    {
        var (context, capture) = await CreateAsync();
        await using var _ = context;

        var preview = context.Render<BelemetryCapturePreview>(p => p.Add(c => c.Capture, capture));

        // "in <the component the first application frame names>": here, this test class.
        StringAssert.StartsWith(preview.Find(".bit-belemetry-preview-title").TextContent, "InvalidOperationException in BelemetryCapturePreviewTests");
        Assert.DoesNotContain("<script>", preview.Markup, "Every value is rendered as text.");
        Assert.HasCount(2, preview.FindAll(".bit-belemetry-preview-events tbody tr"));
        Assert.IsTrue(preview.FindAll(".bit-belemetry-preview-section input").Any(i => i.HasAttribute("disabled")), "A section that was not requested cannot be ticked.");
    }

    [TestMethod]
    public async Task RemovingAndExcluding_NarrowTheCapture_AndConfirmingConfirmsIt()
    {
        var (context, capture) = await CreateAsync();
        await using var _ = context;
        BelemetryDiagnosticCapture? confirmed = null;
        var cancelled = false;

        var preview = context.Render<BelemetryCapturePreview>(p => p
            .Add(c => c.Capture, capture)
            .Add(c => c.OnConfirmed, c => confirmed = c)
            .Add(c => c.OnCancelled, () => cancelled = true));

        preview.FindAll(".bit-belemetry-preview-remove")[0].Click();
        Assert.HasCount(1, capture.Events);

        var release = preview.FindAll(".bit-belemetry-preview-section input")[0];
        release.Change(false);
        Assert.IsFalse(capture.IncludedSections.HasFlag(BelemetryBundleSections.Release));

        preview.Find("textarea").Input("I pressed Save; mail me at jane@example.com");
        preview.Find("textarea").Blur();
        Assert.AreEqual("I pressed Save; mail me at [email]", capture.Description);

        await preview.Find(".bit-belemetry-preview-confirm").ClickAsync(new());
        Assert.AreSame(capture, confirmed);
        Assert.IsTrue(capture.IsConfirmed);
        Assert.AreEqual("The report was saved.", preview.Find(".bit-belemetry-preview-status").TextContent, "The bUnit JS runtime accepts the download call.");

        preview.Find(".bit-belemetry-preview-cancel").Click();
        Assert.IsTrue(cancelled);
    }

    [TestMethod]
    public async Task ADownloadThatCouldNotStart_IsNotAConfirmationTheAppActsOn()
    {
        // The browser refused the download: the app must not dismiss the capture or tell the user it was sent.
        var (context, capture) = await CreateAsync(c => c.JSInterop.SetupModule(BelemetryJsBridge.ModulePath)
            .SetupVoid(i => i.Identifier == "downloadFile").SetException(new JSException("The download was blocked.")));
        await using var _ = context;
        BelemetryDiagnosticCapture? confirmed = null;

        var preview = context.Render<BelemetryCapturePreview>(p => p
            .Add(c => c.Capture, capture)
            .Add(c => c.OnConfirmed, c => confirmed = c));
        await preview.Find(".bit-belemetry-preview-confirm").ClickAsync(new());

        Assert.IsNull(confirmed);
        Assert.AreEqual("The report could not be saved here.", preview.Find(".bit-belemetry-preview-status").TextContent);
        Assert.IsFalse(preview.Find(".bit-belemetry-preview-confirm").HasAttribute("disabled"), "The user can try again.");
    }

    [TestMethod]
    public async Task AFailedCapture_SaysWhy()
    {
        await using var context = new BunitContext();
        context.Services.AddBitBelemetryServices(TestHost.ApplyTestDefaults);
        var capture = await context.Services.GetRequiredService<IBelemetryDiagnostics>().CaptureAsync();

        var preview = context.Render<BelemetryCapturePreview>(p => p.Add(c => c.Capture, capture));

        StringAssert.Contains(preview.Find("[role=alert]").TextContent, "not enabled");
        Assert.IsEmpty(preview.FindAll(".bit-belemetry-preview-confirm"));
    }
}
