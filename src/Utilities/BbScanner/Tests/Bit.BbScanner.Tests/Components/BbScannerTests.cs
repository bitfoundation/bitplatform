using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.BbScanner.Tests.Components;

/// <summary>
/// The component the way a prerender meets it: markup that waits for the component to become interactive, and the
/// parameters that shape that markup. <see cref="BbScannerRemoteTests"/> covers it once interactive outside
/// WebAssembly; the camera itself is the E2E suite's to prove.
/// </summary>
[TestClass]
public class BbScannerTests : BunitContext
{
    public BbScannerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        // What a prerender looks like: the markup is produced on a renderer that is not interactive.
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
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
    public async Task Starting_while_prerendering_does_nothing_rather_than_throwing()
    {
        var scanner = RenderPrerendered();

        var before = scanner.Instance.Status;

        await scanner.InvokeAsync(() => scanner.Instance.StartAsync());
        await scanner.InvokeAsync(() => scanner.Instance.OpenFilePickerAsync());

        Assert.AreEqual(before, scanner.Instance.Status);
        Assert.IsFalse(await scanner.InvokeAsync(() => scanner.Instance.SetTorchAsync(true)));
        Assert.IsFalse(await scanner.InvokeAsync(() => scanner.Instance.SetZoomAsync(2)));
        Assert.AreEqual(BbCameraPermission.Unknown, await scanner.InvokeAsync(() => scanner.Instance.GetCameraPermissionAsync()));
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
    public void DecodeAllInImage_reads_every_symbol_with_the_scanner_formats()
    {
        using var frame = BbLuminanceBuffer.Rent(900, 400);
        frame.Pixels.Fill(255);
        using (var left = SyntheticImage.FromMatrix(QrEncoder.Encode("LEFT"), scale: 5))
        using (var right = SyntheticImage.FromMatrix(QrEncoder.Encode("RIGHT"), scale: 5))
        {
            ImageTransforms.Paste(left, frame, 40, 60);
            ImageTransforms.Paste(right, frame, 560, 60);
        }

        var rgba = ImageTransforms.ToRgba(frame, tint: false);
        var qr = RenderPrerendered(parameters => parameters.Add(p => p.Formats, BbBarcodeFormat.QrCode));

        var results = qr.Instance.DecodeAllInImage(rgba, frame.Width, frame.Height, BbPixelFormat.Rgba32);

        CollectionAssert.AreEquivalent(new[] { "LEFT", "RIGHT" }, results.Select(r => r.Text).ToArray());
        Assert.HasCount(1, qr.Instance.DecodeAllInImage(rgba, frame.Width, frame.Height, BbPixelFormat.Rgba32, maxSymbols: 1));
    }

    [TestMethod]
    public async Task Scanning_an_encoded_image_while_prerendering_says_it_cannot()
    {
        var scanner = RenderPrerendered();

        var error = await Assert.ThrowsExactlyAsync<BbScannerException>(() =>
            scanner.InvokeAsync(() => scanner.Instance.ScanImageAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png")));

        Assert.AreEqual(BbScannerErrorKind.NotSupported, error.Kind);
        Assert.Contains("DecodeAllInImage", error.Message, "The message points at what does work here.");
    }

    [TestMethod]
    public async Task An_image_stream_is_not_read_while_prerendering()
    {
        var scanner = RenderPrerendered();

        using var stream = new MemoryStream(new byte[4096]);

        var error = await Assert.ThrowsExactlyAsync<BbScannerException>(() =>
            scanner.InvokeAsync(() => scanner.Instance.ScanImageAsync(stream)));

        Assert.AreEqual(BbScannerErrorKind.NotSupported, error.Kind);
        Assert.AreEqual(0, stream.Position, "An upload over a circuit must not cross the network only to be refused.");
    }

    [TestMethod]
    public void Capturing_a_frame_without_a_camera_is_an_error_rather_than_an_empty_result()
    {
        var scanner = RenderPrerendered();

        Assert.ThrowsExactly<InvalidOperationException>(() => scanner.Instance.ScanCurrentFrameAsync());
    }

    [TestMethod]
    public async Task Scanning_with_a_disposed_scanner_says_it_is_disposed_rather_than_unsupported()
    {
        var scanner = RenderPrerendered();
        await scanner.Instance.DisposeAsync();
        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() =>
            scanner.InvokeAsync(() => scanner.Instance.ScanImageAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png")));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => scanner.InvokeAsync(() => scanner.Instance.ScanImageAsync(stream)));
        Assert.ThrowsExactly<ObjectDisposedException>(() => scanner.Instance.ScanCurrentFrameAsync());
    }

    [TestMethod]
    public void The_root_carries_an_id_for_the_drop_target_unless_the_application_gave_it_one()
    {
        var own = RenderPrerendered();
        var given = RenderPrerendered(parameters => parameters.AddUnmatched("id", "my-scanner"));

        StringAssert.StartsWith(own.Find(".bit-bbscanner").Id, "bit-bbscanner-");
        Assert.AreEqual("my-scanner", given.Find(".bit-bbscanner").Id);
    }

    [TestMethod]
    public void The_scan_behaviour_parameters_are_accepted()
    {
        var scanner = RenderPrerendered(parameters => parameters
            .Add(p => p.MaxSymbolsPerFrame, 4)
            .Add(p => p.AfterScan, BbAfterScan.Stop)
            .Add(p => p.AcceptResult, r => r.Text.StartsWith("TICKET-", StringComparison.Ordinal))
            .Add(p => p.DuplicateSuppressionWindow, Timeout.InfiniteTimeSpan));

        Assert.AreEqual(4, scanner.Instance.MaxSymbolsPerFrame);
        Assert.AreEqual(BbAfterScan.Stop, scanner.Instance.AfterScan);
        scanner.Instance.ResetDuplicateFilter();
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
}
