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
        SetRendererInfo(new RendererInfo("WebView", isInteractive: true));
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task The_platform_camera_is_opened_and_its_frames_decoded_without_the_page_camera()
    {
        _camera.Scene = SceneWithQrCode("NATIVE-FRAME");
        var scans = new List<BbBarcodeResult>();

        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode)
            .Add(p => p.OnScan, r => scans.Add(r)));

        await WaitUntil(() => Assert.ContainsSingle(scans), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual("NATIVE-FRAME", scans[0].Text);
        Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status);
        Assert.AreEqual(BbCameraFacing.Rear, _camera.Requests.Single().Facing);
        Assert.AreEqual(0, _page.Count("start"), "The page's camera is never asked for.");
        Assert.IsEmpty(_page.Grabs, "No frame crosses the page.");
        Assert.AreEqual(1, _page.Count("attachNativePreview"), "The page shows the native preview through itself.");
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task The_page_places_the_preview_and_the_overlays_follow_its_size()
    {
        var scanner = Render<BbScanner>();
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);

        _page.Place(16, 120, 360, 270);

        var placement = _camera.Current!.Placements.Last();
        Assert.AreEqual(new BbPreviewPlacement(16, 120, 360, 270, 12, true, "rgb(255, 255, 255)", 0, 0, 400, 800), placement);
    }

    [TestMethod]
    public async Task Switching_cameras_keeps_the_page_showing_the_preview_through_itself()
    {
        var scanner = Render<BbScanner>();
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        var back = _camera.Current!;

        await scanner.InvokeAsync(() => scanner.Instance.SwitchCameraAsync("front"));

        await WaitUntil(() =>
        {
            var actualValue = scanner.Instance.Camera?.FacingMode;
            Assert.AreEqual("user", actualValue);
        }, cancellationToken: TestContext.CancellationToken);
        Assert.IsTrue(back.Disposed);
        Assert.DoesNotContain(p => p.IsDetached, back.Placements, "The preview is replaced, not taken down in between.");
        Assert.AreEqual(0, _page.Count("detachNativePreview"));
        Assert.IsNotNull(_page.PreviewReceiver);
    }

    [TestMethod]
    public async Task A_camera_that_opens_after_the_scanner_stopped_is_closed_again()
    {
        var open = new TaskCompletionSource();
        _camera.OpenDelay = open.Task;
        var scanner = Render<BbScanner>();
        await WaitUntil(() => Assert.ContainsSingle(_camera.Requests), cancellationToken: TestContext.CancellationToken);

        await scanner.InvokeAsync(() => scanner.Instance.StopAsync());
        open.SetResult();

        await WaitUntil(() => Assert.ContainsSingle(_camera.Sessions), cancellationToken: TestContext.CancellationToken);
        await WaitUntil(() => Assert.IsTrue(_camera.Sessions[0].Disposed), cancellationToken: TestContext.CancellationToken);
        Assert.IsTrue(_camera.Sessions[0].Placements.Last().IsDetached, "The preview is taken down before the camera closes.");
        Assert.AreEqual(BbScannerStatus.Stopped, scanner.Instance.Status);
        Assert.AreEqual(0, _page.Count("attachNativePreview"));
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task A_turned_device_resizes_the_scanner_to_the_frames_it_now_delivers()
    {
        var cameras = new List<BbCameraInfo>();
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnCameraChanged, c => cameras.Add(c)));
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);

        _camera.Current!.Turn(480, 640);

        await WaitUntil(() => Assert.AreEqual(480, scanner.Instance.Camera!.Width), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(640, scanner.Instance.Camera!.Height);
        Assert.AreEqual((480, 640), scanner.Instance.ProcessingSize);
        await WaitUntil(() => Assert.AreEqual(640, cameras.Last().Height), cancellationToken: TestContext.CancellationToken);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task A_refusal_by_the_platform_is_reported_as_it_was_classified()
    {
        _camera.OpenFailure = new BbScannerException(BbScannerErrorKind.PermissionDenied, "The user refused the camera.");
        BbScannerException? error = null;

        Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));

        await WaitUntil(() => Assert.IsNotNull(error), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(BbScannerErrorKind.PermissionDenied, error!.Kind);
        Assert.AreEqual("The user refused the camera.", error.Message);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task A_camera_that_ends_is_reported_lost_and_closed()
    {
        BbScannerException? error = null;
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        var session = _camera.Current!;

        session.State = BbCameraSessionState.Ended;

        await WaitUntil(() => Assert.IsNotNull(error), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(BbScannerErrorKind.CameraUnavailable, error!.Kind);
        Assert.IsTrue(session.Disposed);
        Assert.IsNull(_page.PreviewReceiver, "The page is put back as it was.");
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Released_while_the_app_is_in_the_background_the_camera_is_opened_again_when_it_returns()
    {
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.ReleaseCameraWhenHidden, true));
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        var first = _camera.Current!;

        _camera.IsInBackground = true;
        await WaitUntil(() => Assert.IsTrue(first.Disposed), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(BbScannerStatus.Starting, scanner.Instance.Status);

        _camera.IsInBackground = false;
        await WaitUntil(() => Assert.HasCount(2, _camera.Sessions), cancellationToken: TestContext.CancellationToken);
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task A_camera_the_platform_interrupts_while_the_app_is_in_front_is_waited_for_rather_than_released()
    {
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.ReleaseCameraWhenHidden, true));
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        var session = _camera.Current!;

        // iPad multitasking, or system pressure: the app is still in front. Released, the camera would be opened again at
        // once, into the same interruption.
        session.State = BbCameraSessionState.Interrupted;
        await global::System.Threading.Tasks.Task.Delay(500, TestContext.CancellationToken);

        Assert.IsFalse(session.Disposed);
        Assert.ContainsSingle(_camera.Sessions);
        Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task A_camera_that_ends_while_the_app_is_in_the_background_is_opened_again_when_it_returns()
    {
        BbScannerException? error = null;
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.OnError, e => error = e));
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        var first = _camera.Current!;

        _camera.IsInBackground = true;
        first.State = BbCameraSessionState.Ended;
        await WaitUntil(() => Assert.IsTrue(first.Disposed), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(BbScannerStatus.Starting, scanner.Instance.Status);

        _camera.IsInBackground = false;
        await WaitUntil(() => Assert.HasCount(2, _camera.Sessions), cancellationToken: TestContext.CancellationToken);
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        Assert.IsNull(error, "An interruption is recovered from, not reported.");
    }

    [TestMethod]
    public async Task A_platform_that_throws_rather_than_say_reports_the_permission_unknown()
    {
        _camera.PermissionFailure = new InvalidOperationException("You need to declare using the permission: `android.permission.CAMERA`.");
        var scanner = Render<BbScanner>(parameters => parameters.Add(p => p.AutoStart, false));

        Assert.AreEqual(BbCameraPermission.Unknown, await scanner.InvokeAsync(() => scanner.Instance.GetCameraPermissionAsync()));
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
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);

        var results = await scanner.InvokeAsync(() => scanner.Instance.ScanCurrentFrameAsync());

        Assert.AreEqual("FULL-SIZE", results.Single().Text);
        Assert.AreEqual(1280, results.Single().FrameWidth);
    }

    [TestMethod]
    public async Task The_torch_and_the_permission_are_asked_of_the_platform()
    {
        _camera.Permission = BbCameraPermission.Prompt;
        var scanner = Render<BbScanner>();
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);

        Assert.IsTrue(await scanner.InvokeAsync(() => scanner.Instance.SetTorchAsync(true)));
        Assert.IsTrue(_camera.Current!.TorchOn);
        Assert.AreEqual(BbCameraPermission.Prompt, await scanner.InvokeAsync(() => scanner.Instance.GetCameraPermissionAsync()));
        Assert.AreSequenceEqual(new[] { "Back camera", "Front camera" }, scanner.Instance.Cameras.Select(c => c.Label).ToArray());
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Changing_the_formats_while_frames_are_decoded_on_another_thread_keeps_scanning()
    {
        _camera.Scene = SceneWithQrCode("STILL-SCANNING");
        var scans = new List<string>();
        var scanner = Render<BbScanner>(parameters => parameters
            .Add(p => p.Formats, BbBarcodeFormat.QrCode | BbBarcodeFormat.Code128)
            .Add(p => p.DuplicateSuppressionWindow, TimeSpan.Zero)
            .Add(p => p.OnScan, r => { lock (scans) { scans.Add(r.Text); } }));
        await WaitUntil(() => Assert.IsNotEmpty(scans), cancellationToken: TestContext.CancellationToken);

        // Each change replaces the decoder, which may be decoding a frame at that moment.
        for (var i = 0; i < 20; i++)
        {
            var formats = i % 2 == 0 ? BbBarcodeFormat.QrCode : BbBarcodeFormat.QrCode | BbBarcodeFormat.Code128;
            scanner.Render(parameters => parameters.Add(p => p.Formats, formats));
            await global::System.Threading.Tasks.Task.Delay(15, TestContext.CancellationToken);
        }

        int before;
        lock (scans)
        {
            before = scans.Count;
        }

        await WaitUntil(() => Assert.IsGreaterThan(before, scans.Count), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status);
    }

    [TestMethod]
    public async Task Stopping_closes_the_camera_and_puts_the_page_back()
    {
        var scanner = Render<BbScanner>();
        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        var session = _camera.Current!;

        await scanner.InvokeAsync(() => scanner.Instance.StopAsync());

        Assert.IsTrue(session.Disposed);
        Assert.IsTrue(session.Placements.Last().IsDetached, "The source puts back what it changed to show the preview.");
        Assert.AreEqual(1, _page.Count("detachNativePreview"));
        Assert.AreEqual(BbScannerStatus.Stopped, scanner.Instance.Status);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task A_source_that_is_not_supported_here_leaves_the_page_s_camera()
    {
        _camera.IsSupported = false;
        var scanner = Render<BbScanner>();

        await WaitUntil(() => Assert.AreEqual(BbScannerStatus.Scanning, scanner.Instance.Status), cancellationToken: TestContext.CancellationToken);
        Assert.AreEqual(1, _page.Count("start"));
        Assert.IsEmpty(_camera.Requests);
        Assert.AreEqual(0, _page.Last("start")[6], "A page behind a WebView needs no watchdog.");
    }

    private static async global::System.Threading.Tasks.Task WaitUntil(Action assertion, global::System.Threading.CancellationToken cancellationToken = default)
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
                await global::System.Threading.Tasks.Task.Delay(20, cancellationToken);
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
    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = null!;
}
