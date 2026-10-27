using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BbScanner.Tests.Components;

/// <summary>
/// The scanner in a Blazor Hybrid app that registered a platform camera (<see cref="IBbCameraSource"/>, which
/// Bit.BbScanner.Maui provides): the frames come from the source, in the app's process, and never from the page, which
/// only shows the source's preview through itself and says where to draw it.
/// </summary>
[TestClass]
public class BbScannerNativeCameraTests : BunitContext
{
    private const int FrameWidth = 640;
    private const int FrameHeight = 480;

    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly FakeScannerPage _page = new();
    private readonly FakeCameraSource _camera = new();

    public BbScannerNativeCameraTests()
    {
        Services.AddSingleton<IJSRuntime>(_page);
        Services.AddSingleton<IBbCameraSource>(_camera);
#if NET9_0_OR_GREATER
        SetRendererInfo(new RendererInfo("WebView", isInteractive: true));
#endif
    }

    [TestMethod]
    public void The_platform_camera_is_opened_and_its_frames_decoded_without_the_page_camera()
    {
        _camera.Scene = SceneWithQrCode("NATIVE-FRAME");
        var scans = new List<BbBarcodeResult>();

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode)
            .Add(p => p.OnScan, r => scans.Add(r)));

        WaitUntil(() => Assert.HasCount(1, scans));
        Assert.AreEqual("NATIVE-FRAME", scans[0].Text);
        Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status);
        Assert.AreEqual(BbCameraFacing.Rear, _camera.Requests.Single().Facing);
        Assert.AreEqual(0, _page.Count("start"), "The page's camera is never asked for.");
        Assert.AreEqual(0, _page.Count("grabFrameData"), "No frame crosses the page.");
        Assert.AreEqual(1, _page.Count("attachNativePreview"), "The page shows the native preview through itself.");
    }

    [TestMethod]
    public void The_page_places_the_preview_and_the_overlays_follow_its_size()
    {
        var scanner = Render<BbScanner>();
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));

        _page.Place(16, 120, 360, 270);

        var placement = _camera.Current!.Placements.Last();
        Assert.AreEqual(new BbPreviewPlacement(16, 120, 360, 270, 12, true, "rgb(255, 255, 255)", 0, 0, 400, 800), placement);
    }

    [TestMethod]
    public async Task Switching_cameras_keeps_the_page_showing_the_preview_through_itself()
    {
        var scanner = Render<BbScanner>();
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        var back = _camera.Current!;

        await scanner.InvokeAsync(() => scanner.Instance.SwitchCameraAsync("front"));

        WaitUntil(() => Assert.AreEqual("user", scanner.Instance.Camera?.FacingMode));
        Assert.IsTrue(back.Disposed);
        Assert.IsFalse(back.Placements.Any(p => p.IsDetached), "The preview is replaced, not taken down in between.");
        Assert.AreEqual(0, _page.Count("detachNativePreview"));
        Assert.IsNotNull(_page.PreviewReceiver);
    }

    [TestMethod]
    public async Task A_camera_that_opens_after_the_scanner_stopped_is_closed_again()
    {
        var open = new TaskCompletionSource();
        _camera.OpenDelay = open.Task;
        var scanner = Render<BbScanner>();
        WaitUntil(() => Assert.HasCount(1, _camera.Requests));

        await scanner.InvokeAsync(() => scanner.Instance.StopAsync());
        open.SetResult();

        WaitUntil(() => Assert.HasCount(1, _camera.Sessions));
        WaitUntil(() => Assert.IsTrue(_camera.Sessions[0].Disposed));
        Assert.IsTrue(_camera.Sessions[0].Placements.Last().IsDetached, "The preview is taken down before the camera closes.");
        Assert.AreEqual(BbScannerStatus.Stopped, scanner.Instance.Status);
        Assert.AreEqual(0, _page.Count("attachNativePreview"));
    }

    [TestMethod]
    public void A_turned_device_resizes_the_scanner_to_the_frames_it_now_delivers()
    {
        var cameras = new List<BbCameraInfo>();
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnCameraChanged, c => cameras.Add(c)));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));

        _camera.Current!.Turn(480, 640);

        WaitUntil(() => Assert.AreEqual(480, scanner.Instance.Camera!.Width));
        Assert.AreEqual(640, scanner.Instance.Camera!.Height);
        Assert.AreEqual((480, 640), scanner.Instance.ProcessingSize);
        WaitUntil(() => Assert.AreEqual(640, cameras.Last().Height));
    }

    [TestMethod]
    public void A_refusal_by_the_platform_is_reported_as_it_was_classified()
    {
        _camera.OpenFailure = new BbScannerException(BbScannerErrorKind.PermissionDenied, "The user refused the camera.");
        BbScannerException? error = null;

        Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));

        WaitUntil(() => Assert.IsNotNull(error));
        Assert.AreEqual(BbScannerErrorKind.PermissionDenied, error!.Kind);
        Assert.AreEqual("The user refused the camera.", error.Message);
    }

    [TestMethod]
    public void A_camera_that_ends_is_reported_lost_and_closed()
    {
        BbScannerException? error = null;
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        var session = _camera.Current!;

        session.State = BbCameraSessionState.Ended;

        WaitUntil(() => Assert.IsNotNull(error));
        Assert.AreEqual(BbScannerErrorKind.CameraUnavailable, error!.Kind);
        Assert.IsTrue(session.Disposed);
        Assert.IsNull(_page.PreviewReceiver, "The page is put back as it was.");
    }

    [TestMethod]
    public void Released_while_the_app_is_in_the_background_the_camera_is_opened_again_when_it_returns()
    {
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.ReleaseCameraWhenHidden, true));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        var first = _camera.Current!;

        _camera.IsInBackground = true;
        WaitUntil(() => Assert.IsTrue(first.Disposed));
        Assert.AreEqual(BbScannerStatus.Starting, scanner.Instance.Status);

        _camera.IsInBackground = false;
        WaitUntil(() => Assert.HasCount(2, _camera.Sessions));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
    }

    [TestMethod]
    public async Task The_current_frame_is_captured_at_the_camera_s_own_size()
    {
        _camera.Width = 1280;
        _camera.Height = 960;
        _camera.Scene = SceneWithQrCode("FULL-SIZE", 1280, 960, scale: 2);
        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode)
            .Add(p => p.MaxProcessingSize, 160));
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));

        var results = await scanner.InvokeAsync(() => scanner.Instance.ScanCurrentFrameAsync());

        Assert.AreEqual("FULL-SIZE", results.Single().Text);
        Assert.AreEqual(1280, results.Single().FrameWidth);
    }

    [TestMethod]
    public async Task The_torch_and_the_permission_are_asked_of_the_platform()
    {
        _camera.Permission = BbCameraPermission.Prompt;
        var scanner = Render<BbScanner>();
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));

        Assert.IsTrue(await scanner.InvokeAsync(() => scanner.Instance.SetTorchAsync(true)));
        Assert.IsTrue(_camera.Current!.TorchOn);
        Assert.AreEqual(BbCameraPermission.Prompt, await scanner.InvokeAsync(() => scanner.Instance.GetCameraPermissionAsync()));
        CollectionAssert.AreEqual(new[] { "Back camera", "Front camera" }, scanner.Instance.Cameras.Select(c => c.Label).ToArray());
    }

    [TestMethod]
    public void Changing_the_formats_while_frames_are_decoded_on_another_thread_keeps_scanning()
    {
        _camera.Scene = SceneWithQrCode("STILL-SCANNING");
        var scans = new List<string>();
        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode | BbBarcodeFormat.Code128)
            .Add(p => p.DuplicateSuppressionWindow, TimeSpan.Zero)
            .Add(p => p.OnScan, r => { lock (scans) { scans.Add(r.Text); } }));
        WaitUntil(() => Assert.IsNotEmpty(scans));

        // Each change replaces the decoder, which may be decoding a frame at that moment.
        for (var i = 0; i < 20; i++)
        {
            var formats = i % 2 == 0 ? BbBarcodeFormat.QrCode : BbBarcodeFormat.QrCode | BbBarcodeFormat.Code128;
            scanner.Render(parameters => parameters.Add(p => p.Formats, formats));
            Thread.Sleep(15);
        }

        int before;
        lock (scans)
        {
            before = scans.Count;
        }

        WaitUntil(() => Assert.IsGreaterThan(before, scans.Count));
        Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status);
    }

    [TestMethod]
    public async Task Stopping_closes_the_camera_and_puts_the_page_back()
    {
        var scanner = Render<BbScanner>();
        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        var session = _camera.Current!;

        await scanner.InvokeAsync(() => scanner.Instance.StopAsync());

        Assert.IsTrue(session.Disposed);
        Assert.IsTrue(session.Placements.Last().IsDetached, "The source puts back what it changed to show the preview.");
        Assert.AreEqual(1, _page.Count("detachNativePreview"));
        Assert.AreEqual(BbScannerStatus.Stopped, scanner.Instance.Status);
    }

    [TestMethod]
    public void A_source_that_is_not_supported_here_leaves_the_page_s_camera()
    {
        _camera.IsSupported = false;
        var scanner = Render<BbScanner>();

        WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status));
        Assert.AreEqual(1, _page.Count("start"));
        Assert.IsEmpty(_camera.Requests);
#if NET9_0_OR_GREATER
        Assert.AreEqual(0, _page.Last("start")[6], "A page behind a WebView needs no watchdog.");
#endif
    }

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

    private static byte[] SceneWithQrCode(string text, int width = FrameWidth, int height = FrameHeight, int scale = 4)
    {
        using var frame = BbLuminanceBuffer.Rent(width, height);
        frame.Pixels.Fill(255);
        using (var symbol = SyntheticImage.FromMatrix(QrEncoder.Encode(text), scale: scale))
        {
            ImageTransforms.Paste(symbol, frame, (width - symbol.Width) / 2, (height - symbol.Height) / 2);
        }

        return frame.Pixels[..(width * height)].ToArray();
    }
}
