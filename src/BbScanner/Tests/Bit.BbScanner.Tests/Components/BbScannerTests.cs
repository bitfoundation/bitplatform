using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.BbScanner.Tests.Components;

/// <summary>
/// The component outside a browser, the way it meets a server: prerendered markup that waits for WebAssembly, the
/// parameters that shape that markup, and the honest failure an interactive renderer that is not WebAssembly gets
/// instead of a Start button that quietly does nothing. The camera itself is the E2E suite's to prove.
/// </summary>
[TestClass]
public class BbScannerTests : BunitContext
{
    public BbScannerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
#if NET9_0_OR_GREATER
        // What a prerender looks like: the markup is produced on a renderer that is not interactive.
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
#endif
    }

    [TestMethod]
    public void Renders_a_labelled_region_with_the_viewfinder_and_the_built_in_chrome()
    {
        var scanner = RenderPrerendered();

        var root = scanner.Find(".bit-bbscanner");

        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("Barcode scanner", root.GetAttribute("aria-label"));
        Assert.IsNotNull(scanner.Find("video.bit-bbscanner__video"));
        Assert.HasCount(4, scanner.FindAll(".bit-bbscanner__corner"));
        Assert.IsNotNull(scanner.Find(".bit-bbscanner__status"));
        Assert.IsNotNull(scanner.Find(".bit-bbscanner__controls"));
        Assert.IsNotNull(scanner.Find("input[type=file].bit-bbscanner__file"));
        Assert.AreEqual("polite", scanner.Find(".bit-bbscanner__sr").GetAttribute("aria-live"));
    }

    [TestMethod]
    public void A_scanner_that_has_not_started_is_idle_and_offers_to_start()
    {
        var scanner = RenderPrerendered();

        if (OnNet8()) return;

        Assert.AreEqual(BbScannerStatus.Idle, scanner.Instance.Status);
        Assert.AreEqual("idle", scanner.Find(".bit-bbscanner").GetAttribute("data-status"));
        Assert.Contains("Start", scanner.Find(".bit-bbscanner__controls").TextContent);
        Assert.IsNull(scanner.Instance.Camera);
    }

    [TestMethod]
    public void The_application_class_and_style_are_merged_with_the_component_own()
    {
        var scanner = RenderPrerendered(parameters => parameters
            .Add(p => p.CssClass, "from-parameter")
            .AddUnmatched("class", "from-attribute")
            .AddUnmatched("style", "--bit-bbscanner-accent: red;")
            .AddUnmatched("data-test", "kept"));

        var root = scanner.Find(".bit-bbscanner");

        Assert.Contains("from-parameter", root.ClassName!);
        Assert.Contains("from-attribute", root.ClassName!);
        Assert.Contains("--bit-bbscanner-accent: red", root.GetAttribute("style")!);
        Assert.AreEqual("kept", root.GetAttribute("data-test"));
    }

    [TestMethod]
    public void The_mask_dims_only_around_a_region_smaller_than_the_frame()
    {
        var full = RenderPrerendered(parameters => parameters.Add(p => p.Region, BbScanRegion.Full));
        var square = RenderPrerendered(parameters => parameters.Add(p => p.Region, BbScanRegion.CenteredSquare(0.5)));

        Assert.IsEmpty(full.FindAll(".bit-bbscanner__mask"));
        Assert.Contains("polygon(evenodd", square.Find(".bit-bbscanner__mask").GetAttribute("style")!);
        Assert.Contains("left:25%", square.Find(".bit-bbscanner__reticle").GetAttribute("style")!);
    }

    [TestMethod]
    public void An_overlay_replaces_the_reticle_and_receives_the_scanner()
    {
        BbScanner? received = null;

        var scanner = RenderPrerendered(parameters => parameters
            .Add(p => p.Overlay, (RenderFragment<BbScanner>)(instance => builder =>
            {
                received = instance;
                builder.AddMarkupContent(0, "<div class=\"custom-overlay\"></div>");
            })));

        Assert.IsNotNull(scanner.Find(".custom-overlay"));
        Assert.IsEmpty(scanner.FindAll(".bit-bbscanner__reticle"));
        Assert.AreSame(scanner.Instance, received);
    }

    [TestMethod]
    public void The_built_in_chrome_can_be_switched_off_piece_by_piece()
    {
        var scanner = RenderPrerendered(parameters => parameters
            .Add(p => p.ShowControls, false)
            .Add(p => p.ShowStatusBar, false)
            .Add(p => p.ShowReticle, false)
            .Add(p => p.EnableFileScanning, false));

        Assert.IsEmpty(scanner.FindAll(".bit-bbscanner__controls"));
        Assert.IsEmpty(scanner.FindAll(".bit-bbscanner__status"));
        Assert.IsEmpty(scanner.FindAll(".bit-bbscanner__reticle"));
        Assert.IsEmpty(scanner.FindAll(".bit-bbscanner__file"));
    }

    [TestMethod]
    public void An_interactive_renderer_that_is_not_webassembly_reports_not_supported()
    {
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
#endif
        BbScannerException? error = null;
        var statuses = new List<BbScannerStatus>();

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.OnError, e => error = e)
            .Add(p => p.OnStatusChanged, s => statuses.Add(s)));

        Assert.AreEqual(BbScannerStatus.Faulted, scanner.Instance.Status);
        Assert.IsNotNull(error);
        Assert.AreEqual(BbScannerErrorKind.NotSupported, error.Kind);
        Assert.Contains("WebAssembly", error.Message);
        CollectionAssert.Contains(statuses, BbScannerStatus.Faulted);
        Assert.Contains("WebAssembly", scanner.Find(".bit-bbscanner__message--error").TextContent);
#if NET9_0_OR_GREATER
        Assert.Contains("Server", error.Message, "The message names the renderer the scanner is stuck on.");
#endif
    }

    [TestMethod]
    public async Task Starting_outside_a_browser_does_nothing_rather_than_throwing()
    {
        var scanner = RenderPrerendered();
        var before = scanner.Instance.Status;

        await scanner.InvokeAsync(() => scanner.Instance.StartAsync());
        await scanner.InvokeAsync(() => scanner.Instance.OpenFilePickerAsync());

        Assert.AreEqual(before, scanner.Instance.Status);
        Assert.IsFalse(await scanner.InvokeAsync(() => scanner.Instance.SetTorchAsync(true)));
        Assert.IsFalse(await scanner.InvokeAsync(() => scanner.Instance.SetZoomAsync(2)));
    }

    [TestMethod]
    public void DecodeImage_reads_pixels_the_application_owns_with_the_scanner_formats()
    {
        using var image = SyntheticImage.FromMatrix(QrEncoder.Encode("BUNIT-DECODE-IMAGE"), scale: 4);
        var rgba = ImageTransforms.ToRgba(image, tint: false);

        var qr = RenderPrerendered(parameters => parameters.Add(p => p.Formats, BbBarcodeFormat.QrCode));
        var linear = RenderPrerendered(parameters => parameters.Add(p => p.Formats, BbBarcodeFormat.AllLinear));

        var result = qr.Instance.DecodeImage(rgba, image.Width, image.Height, BbPixelFormat.Rgba32);

        Assert.IsNotNull(result);
        Assert.AreEqual("BUNIT-DECODE-IMAGE", result.Text);
        Assert.AreEqual(BbBarcodeFormat.QrCode, result.Format);
        Assert.IsNull(linear.Instance.DecodeImage(rgba, image.Width, image.Height, BbPixelFormat.Rgba32), "A scanner looking for linear symbols only must not report a QR code.");
    }

    [TestMethod]
    public async Task Disposing_twice_is_harmless()
    {
        var scanner = RenderPrerendered();

        await scanner.Instance.DisposeAsync();
        await scanner.Instance.DisposeAsync();
    }

    private IRenderedComponent<BbScanner> RenderPrerendered(Action<ComponentParameterCollectionBuilder<BbScanner>>? parameters = null)
        => Render<BbScanner>(builder =>
        {
            // Nothing in these tests may reach a camera: the component would not anyway outside a browser, and
            // saying so keeps the tests about the markup.
            builder.Add(p => p.AutoStart, false);
            parameters?.Invoke(builder);
        });

    /// <summary>
    /// .NET 8 has no RendererInfo, and so no way to tell a prerender from an interactive render: bUnit runs
    /// OnAfterRender, which a real prerender never does, and the scanner reports NotSupported straight away.
    /// </summary>
    private static bool OnNet8()
    {
#if NET9_0_OR_GREATER
        return false;
#else
        return true;
#endif
    }
}
