using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BbScanner.Tests.Components;

/// <summary>
/// The scanner under Blazor Server and Blazor Hybrid, where .NET runs in another process than the page: it reaches the
/// module through IJSRuntime, the page sends each frame as the luminance of the searched region, and the decoder runs
/// here. <see cref="FakeScannerPage"/> stands in for the page; the camera itself is the E2E suite's to prove.
/// </summary>
[TestClass]
public class BbScannerRemoteTests : BunitContext
{
    private const int FrameWidth = 640;
    private const int FrameHeight = 480;

    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly FakeScannerPage _page = new();

    public BbScannerRemoteTests()
    {
        Services.AddSingleton<IJSRuntime>(_page);
    }

    [TestMethod]
    [DataRow("Server")]
    [DataRow("WebView")]
    public void An_interactive_renderer_that_is_not_webassembly_opens_the_camera_and_reads_its_frames(string renderer)
    {
        Interactive(renderer);
        _page.Frame = FrameWithQrCode("REMOTE-FRAME", centreX: FrameWidth / 2, centreY: FrameHeight / 2);
        var scans = new List<BbBarcodeResult>();
        var statuses = new List<BbScannerStatus>();

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode)
            .Add(p => p.OnScan, r => scans.Add(r))
            .Add(p => p.OnStatusChanged, s => statuses.Add(s)));

        WaitUntil(() => Assert.HasCount(1, scans));

        Assert.AreEqual("REMOTE-FRAME", scans[0].Text);
        Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status);
        CollectionAssert.Contains(statuses, BbScannerStatus.Scanning);
        Assert.AreEqual("./_content/Bit.BbScanner/bit-bbscanner.js", _page.ImportedPath);
        Assert.AreEqual((FrameWidth, FrameHeight), _page.Configured);
#if NET9_0_OR_GREATER
        // A page across a network keeps a watchdog on the camera it opened; one behind a WebView goes away with .NET.
        Assert.AreEqual(renderer == "WebView" ? 0 : 20_000, _page.Last("start")[6]);
#else
        Assert.AreEqual(20_000, _page.Last("start")[6], "A page across a process keeps a watchdog on the camera it opened.");
#endif
    }

    [TestMethod]
    public void Only_the_searched_region_crosses_and_results_come_back_in_the_frame_coordinates()
    {
        Interactive("Server");
        _page.Frame = FrameWithQrCode("REGION", centreX: FrameWidth / 2, centreY: FrameHeight / 2);
        BbBarcodeResult? scanned = null;

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode)
            .Add(p => p.Region, BbScanRegion.CenteredSquare(0.7))
            .Add(p => p.OnScan, r => scanned = r));

        WaitUntil(() => Assert.IsNotNull(scanned));

        // A square of 0.7 of the shorter side, centred: 336 pixels of a 640 by 480 frame.
        var grab = _page.Last("grabFrameData");
        CollectionAssert.AreEqual(new object[] { 152, 72, 336, 336 }, grab[1..]);

        // 113 KB of luminance, read in slices that each fit in one message of a circuit's default 32 KB limit.
        Assert.IsLessThanOrEqualTo(30 * 1024, _page.LargestSlice);
        Assert.IsGreaterThanOrEqualTo(4, _page.Count("readData"));

        Assert.AreEqual(FrameWidth, scanned!.FrameWidth);
        Assert.AreEqual(FrameHeight, scanned.FrameHeight);
        var box = scanned.BoundingBox;
        Assert.AreEqual(FrameWidth / 2.0, box.X + (box.Width / 2), 8, "The symbol is where it is in the frame, not in the region.");
        Assert.AreEqual(FrameHeight / 2.0, box.Y + (box.Height / 2), 8);
    }

    [TestMethod]
    public void A_symbol_outside_the_searched_region_is_not_read()
    {
        Interactive("Server");
        _page.Frame = FrameWithQrCode("OUTSIDE", centreX: 70, centreY: 70);
        var scans = 0;

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode)
            .Add(p => p.Region, BbScanRegion.CenteredSquare(0.5))
            .Add(p => p.OnScan, _ => scans++));

        WaitUntil(() => Assert.IsGreaterThanOrEqualTo(3, _page.Count("grabFrameData")));
        Assert.AreEqual(0, scans);
    }

    [TestMethod]
    public void A_frame_call_that_times_out_is_asked_again_rather_than_faulting_the_scanner()
    {
        Interactive("Server");
        _page.Frame = FrameWithQrCode("AFTER-A-BLIP", centreX: FrameWidth / 2, centreY: FrameHeight / 2);

        // What a circuit reconnecting does to the calls in flight: they are never answered, and time out.
        _page.FailNext("poll", new TaskCanceledException());
        _page.FailNext("grabFrameData", new TaskCanceledException());
        _page.FailNext("readData", new OperationCanceledException());
        BbScannerException? error = null;
        BbBarcodeResult? scanned = null;

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode)
            .Add(p => p.OnError, e => error = e)
            .Add(p => p.OnScan, r => scanned = r));

        WaitUntil(() => Assert.IsNotNull(scanned));
        Assert.AreEqual("AFTER-A-BLIP", scanned!.Text);
        Assert.IsNull(error);
        Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status);
    }

    [TestMethod]
    public void A_camera_whose_opening_was_answered_into_a_dropped_connection_is_taken_up()
    {
        Interactive("Server");

        // The page opened the camera, but its answer never arrived: the page is asked what it has open.
        _page.FailNext("start", new TaskCanceledException());
        BbScannerException? error = null;

        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));

        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        Assert.IsNull(error);
        Assert.AreEqual(640, scanner.Instance.Camera!.Width);
    }

    [TestMethod]
    public async Task An_image_the_application_holds_crosses_in_parts_and_comes_back_in_slices()
    {
        Interactive("Server");
        using (var symbol = SyntheticImage.FromMatrix(QrEncoder.Encode("HANDED-OVER"), scale: 6))
        {
            _page.DecodedImage = (symbol.Pixels[..(symbol.Width * symbol.Height)].ToArray(), symbol.Width, symbol.Height);
        }

        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.AutoStart, false));

        // The encoded bytes are whatever the application holds; the page decodes them.
        var encoded = new byte[600 * 1024];
        var results = await scanner.InvokeAsync(() => scanner.Instance.ScanImageAsync(encoded, "image/png"));

        Assert.AreEqual("HANDED-OVER", results.Single().Text);
        CollectionAssert.AreEqual(new[] { 256 * 1024, 256 * 1024, 88 * 1024 }, _page.StagedParts.ToArray(), "Sent in parts, none of them large.");
        Assert.IsLessThanOrEqualTo(30 * 1024, _page.LargestSlice);
        Assert.AreEqual(1, _page.Count("releasePrepared"), "The page is told it can wipe what was read.");
    }

    [TestMethod]
    public void A_page_without_the_camera_api_reports_not_supported_and_says_why()
    {
        Interactive("Server");
        _page.Capabilities = 2;
        BbScannerException? error = null;

        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));

        Assert.AreEqual(BbScannerStatus.Faulted, scanner.Instance.Status);
        Assert.AreEqual(BbScannerErrorKind.NotSupported, error!.Kind);
        Assert.Contains("does not expose a camera API", error.Message);
        Assert.AreEqual(0, _page.Count("start"));
    }

    [TestMethod]
    public void A_page_that_is_not_a_secure_context_says_so()
    {
        Interactive("Server");
        _page.Capabilities = 0;
        BbScannerException? error = null;

        Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));

        Assert.AreEqual(BbScannerErrorKind.NotSupported, error!.Kind);
        Assert.Contains("secure context", error.Message);
    }

    [TestMethod]
    public void A_refused_permission_prompt_is_classified_from_the_message_the_page_sends()
    {
        Interactive("Server");
        _page.StartFailure = new JSException("NotAllowedError: Permission denied\nat open (bit-bbscanner.js:1:1)");
        BbScannerException? error = null;

        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));

        WaitUntil(() => Assert.IsNotNull(error));
        Assert.AreEqual(BbScannerErrorKind.PermissionDenied, error!.Kind);
        Assert.IsGreaterThanOrEqualTo(1, _page.Count("stop"), "A camera the page may have attached before failing is released.");
    }

    [TestMethod]
    public void A_camera_that_ends_is_reported_lost_and_released()
    {
        Interactive("Server");
        BbScannerException? error = null;
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));

        _page.Ended = true;

        // The status turns Faulted before OnError is raised, so the error is what is waited for.
        WaitUntil(() => Assert.IsNotNull(error));
        Assert.AreEqual(BbScannerStatus.Faulted, scanner.Instance.Status);
        Assert.AreEqual(BbScannerErrorKind.CameraUnavailable, error!.Kind);
        Assert.IsGreaterThanOrEqualTo(1, _page.Count("stop"));
    }

    [TestMethod]
    public void A_camera_the_page_closed_while_the_circuit_was_away_is_opened_again()
    {
        Interactive("Server");
        BbScannerException? error = null;
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        Assert.AreEqual(1, _page.Count("start"));

        // What the watchdog leaves behind: the camera ended, and the poll says it was an interruption.
        _page.Ended = true;
        _page.Interrupted = true;

        WaitUntil(() => Assert.AreEqual(2, _page.Count("start")));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        Assert.IsNull(error, "An interruption is recovered from, not reported.");
    }

    [TestMethod]
    public async Task Stopping_and_disposing_release_the_camera_and_the_session_in_the_page()
    {
        Interactive("Server");
        var scanner = Render<BbScanner>();
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));

        await scanner.InvokeAsync(() => scanner.Instance.StopAsync());
        Assert.AreEqual(BbScannerStatus.Stopped, scanner.Instance.Status);
        Assert.IsGreaterThanOrEqualTo(1, _page.Count("stop"));

        await scanner.Instance.DisposeAsync();
        Assert.AreEqual(1, _page.Count("dispose"));
    }

    [TestMethod]
    public async Task The_torch_and_the_permission_are_asked_of_the_page()
    {
        Interactive("Server");
        var scanner = Render<BbScanner>();
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));

        Assert.IsTrue(await scanner.InvokeAsync(() => scanner.Instance.SetTorchAsync(true)));
        Assert.IsTrue(scanner.Instance.IsTorchOn);
        Assert.AreEqual(BbCameraPermission.Granted, await scanner.InvokeAsync(() => scanner.Instance.GetCameraPermissionAsync()));
    }

    [TestMethod]
    public void The_scan_image_button_opens_the_picker_in_the_page_rather_than_through_dotnet()
    {
        Interactive("Server");
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.AutoStart, false));

        var input = scanner.Find("input[type=file].bit-bbscanner__file");
        var button = scanner.FindAll("button.bit-bbscanner__button").Single(b => b.HasAttribute("data-bit-bbscanner-pick"));

        Assert.AreEqual(input.Id, button.GetAttribute("data-bit-bbscanner-pick"));
        Assert.AreEqual(input.Id, _page.Last("attachRoot")[2]);
    }

    /// <summary>
    /// Waits for an assertion to pass. The frame loop runs on the renderer's dispatcher while the test thread waits, and
    /// most of what it does renders nothing, so this polls rather than waiting for a render as bUnit's WaitFor does.
    /// </summary>
    private static void WaitUntil(Action assertion)
    {
        var deadline = DateTime.UtcNow + Wait;
        while (true)
        {
            try
            {
                assertion();
                return;
            }
            catch (AssertFailedException) when (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(20);
            }
        }
    }

    private void Interactive(string renderer)
    {
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo(renderer, isInteractive: true));
#else
        // .NET 8 has no RendererInfo: every render bUnit makes is an interactive one.
        _ = renderer;
#endif
    }

    /// <summary>A light frame of the configured size with a QR code centred on the given point.</summary>
    private static byte[] FrameWithQrCode(string text, int centreX, int centreY)
    {
        using var frame = BbLuminanceBuffer.Rent(FrameWidth, FrameHeight);
        frame.Pixels.Fill(255);
        using (var symbol = SyntheticImage.FromMatrix(QrEncoder.Encode(text), scale: 4))
        {
            ImageTransforms.Paste(symbol, frame, centreX - (symbol.Width / 2), centreY - (symbol.Height / 2));
        }

        return frame.Pixels[..(FrameWidth * FrameHeight)].ToArray();
    }
}
