#if IOS || MACCATALYST
using AVFoundation;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;

namespace Bit.BbScanner;

/// <summary>
/// A camera opened through AVFoundation: a capture session whose preview layer draws into the host's view, and whose
/// video data output - full range 4:2:0, the late frames discarded - gives the scanner the luminance plane, rotated
/// upright by the connection.
/// </summary>
/// <remarks>
/// iOS interrupts the session while the app is in the background and resumes it when the app comes back, by itself; the
/// session says so through its notifications.
/// </remarks>
internal sealed class AppleCameraSession : BbNativeCameraSession
{
    private readonly AVCaptureSession _session;
    private readonly AVCaptureDevice _device;
    private readonly AVCaptureVideoDataOutput _output;
    private readonly FrameDelegate _frames;
    private readonly BbPreviewView _view;
    private readonly List<NSObject> _observers = [];

    private AppleCameraSession(BbPreviewClaim claim, BbCameraInfo camera, AVCaptureSession session, AVCaptureDevice device,
        AVCaptureVideoDataOutput output, BbPreviewView view)
        : base(claim, camera)
    {
        _session = session;
        _device = device;
        _output = output;
        _view = view;
        _frames = new FrameDelegate(this);

        var center = NSNotificationCenter.DefaultCenter;
        _observers.Add(center.AddObserver(AVCaptureSession.WasInterruptedNotification, _ => State = BbCameraSessionState.Interrupted, session));
        _observers.Add(center.AddObserver(AVCaptureSession.InterruptionEndedNotification, _ => State = BbCameraSessionState.Running, session));
        _observers.Add(center.AddObserver(AVCaptureSession.RuntimeErrorNotification, _ => State = BbCameraSessionState.Ended, session));
#if IOS && !MACCATALYST
        // The interface turns after the device does, and resizes the host when it does; a turn half way round leaves the
        // size as it was, and says so only through the device, which says it turned only while something asks it to.
        claim.Host.SizeChanged += OnHostSizeChanged;
        MainThread.BeginInvokeOnMainThread(UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications);
        _observers.Add(center.AddObserver(UIDevice.OrientationDidChangeNotification, _ => MainThread.BeginInvokeOnMainThread(RotateWithInterface)));
#endif
    }

    /// <summary>Opens the camera the request asks for, falling back to whichever there is.</summary>
    public static async Task<IBbCameraSession> OpenAsync(BbPreviewClaim claim, BbCameraRequest request, CancellationToken cancellationToken)
    {
        var view = await MainThread.InvokeOnMainThreadAsync(() => claim.Preview.Handler?.PlatformView as BbPreviewView).ConfigureAwait(false)
            ?? throw new BbScannerException(BbScannerErrorKind.NotSupported, "The scanner host's preview is not on screen yet.");

        var device = Select(request) ?? throw new BbScannerException(BbScannerErrorKind.NoCamera, "No camera matching the request was found.");
        var input = AVCaptureDeviceInput.FromDevice(device, out var error);
        if (input is null)
        {
            throw new BbScannerException(BbScannerErrorKind.CameraUnavailable, $"The camera could not be opened: {error?.LocalizedDescription}");
        }

        var session = new AVCaptureSession();
        session.BeginConfiguration();

        var output = new AVCaptureVideoDataOutput
        {
            AlwaysDiscardsLateVideoFrames = true,
            WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange }.Dictionary,
        };

        if (!session.CanAddInput(input) || !session.CanAddOutput(output))
        {
            session.CommitConfiguration();
            session.Dispose();
            throw new BbScannerException(BbScannerErrorKind.CameraUnavailable, "The camera could not be opened.");
        }

        session.AddInput(input);
        session.AddOutput(output);

        // The request is the frame size to aim for; the presets are the sizes there are, for the camera just added.
        var preset = request.Width * request.Height > 1280 * 720 ? AVCaptureSession.Preset1920x1080
            : request.Width * request.Height > 640 * 480 ? AVCaptureSession.Preset1280x720
            : AVCaptureSession.Preset640x480;
        if (session.CanSetSessionPreset(preset))
        {
            session.SessionPreset = preset;
        }

        session.CommitConfiguration();

        var described = Describe(device, request);
        var camera = new AppleCameraSession(claim, described, session, device, output, view);
        try
        {
            output.SetSampleBufferDelegate(camera._frames, new DispatchQueue("bit.bbscanner.frames"));

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                view.Attach(session);
                camera.Rotate();
            }).ConfigureAwait(false);

            // StartRunning blocks until the camera delivers, so it is kept off the UI thread.
            await Task.Run(session.StartRunning, cancellationToken).ConfigureAwait(false);
            return camera;
        }
        catch (Exception ex)
        {
            await camera.DisposeAsync().ConfigureAwait(false);
            if (ex is OperationCanceledException)
            {
                throw;
            }

            throw new BbScannerException(BbScannerErrorKind.CameraUnavailable, "The camera could not be started.", ex);
        }
    }

    public override Task<bool> SetTorchAsync(bool on)
    {
        if (!Camera.SupportsTorch || IsDisposed || !_device.LockForConfiguration(out _))
        {
            return Task.FromResult(false);
        }

        try
        {
            _device.TorchMode = on ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
            Describe(camera => camera.TorchOn = on);
            return Task.FromResult(true);
        }
        finally
        {
            _device.UnlockForConfiguration();
        }
    }

    public override Task<bool> SetZoomAsync(double zoom)
    {
#if IOS && !MACCATALYST
        if (!Camera.SupportsZoom || IsDisposed || !_device.LockForConfiguration(out _))
        {
            return Task.FromResult(false);
        }

        try
        {
            var factor = Math.Clamp(zoom, Camera.ZoomMin, Camera.ZoomMax);
            _device.VideoZoomFactor = (nfloat)factor;
            Describe(camera => camera.Zoom = factor);
            return Task.FromResult(true);
        }
        finally
        {
            _device.UnlockForConfiguration();
        }
#else
        return Task.FromResult(false);
#endif
    }

    protected override async ValueTask CloseAsync()
    {
        foreach (var observer in _observers)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
        }

        _observers.Clear();
#if IOS && !MACCATALYST
        Preview.Host.SizeChanged -= OnHostSizeChanged;
#endif
        _output.SetSampleBufferDelegate(null, null);
        await Task.Run(_session.StopRunning).ConfigureAwait(false);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
#if IOS && !MACCATALYST
            UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
#endif

            // The view may already show the camera that replaced this one.
            _view.Detach(_session);
        }).ConfigureAwait(false);
        _session.Dispose();
    }

    /// <summary>The cameras AVFoundation can open.</summary>
    public static IReadOnlyList<BbCameraDevice> List() => [.. Devices().Select(device => new BbCameraDevice(device.UniqueID, device.LocalizedName))];

    /// <summary>Whether the device has a camera at all.</summary>
    public static bool HasCamera() => Devices().Length > 0;

    /// <summary>Whether the app is in the background, where iOS has interrupted its camera.</summary>
    public static bool InBackground()
        => MainThread.IsMainThread && UIApplication.SharedApplication.ApplicationState == UIApplicationState.Background;

    private static AVCaptureDevice[] Devices()
    {
        AVCaptureDeviceType[] types = OperatingSystem.IsIOSVersionAtLeast(17) || OperatingSystem.IsMacCatalystVersionAtLeast(17)
            ? [AVCaptureDeviceType.BuiltInWideAngleCamera, AVCaptureDeviceType.External]
            : [AVCaptureDeviceType.BuiltInWideAngleCamera];
        return AVCaptureDeviceDiscoverySession.Create(types, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified)?.Devices ?? [];
    }

    private static AVCaptureDevice? Select(BbCameraRequest request)
    {
        if (request.DeviceId is { Length: > 0 } id && AVCaptureDevice.DeviceWithUniqueID(id) is { } chosen)
        {
            return chosen;
        }

        var wanted = request.Facing == BbCameraFacing.Front ? AVCaptureDevicePosition.Front : AVCaptureDevicePosition.Back;
        var devices = Devices();
        return devices.FirstOrDefault(device => device.Position == wanted) ?? devices.FirstOrDefault();
    }

    private static BbCameraInfo Describe(AVCaptureDevice device, BbCameraRequest request)
    {
        var camera = new BbCameraInfo
        {
            DeviceId = device.UniqueID,
            Label = device.LocalizedName,
            Width = request.Width,
            Height = request.Height,
            FacingMode = device.Position switch
            {
                AVCaptureDevicePosition.Front => "user",
                AVCaptureDevicePosition.Back => "environment",
                _ => "",
            },
            SupportsTorch = device.HasTorch && device.IsTorchModeSupported(AVCaptureTorchMode.On),
        };

#if IOS && !MACCATALYST
        var min = (double)device.MinAvailableVideoZoomFactor;
        var max = Math.Min((double)device.MaxAvailableVideoZoomFactor, 10);
        camera.SupportsZoom = max > min;
        camera.ZoomMin = min;
        camera.ZoomMax = max;
        camera.ZoomStep = 0.1;
        camera.Zoom = (double)device.VideoZoomFactor;
#endif
        return camera;
    }

    /// <summary>
    /// Rotates the frames - and the preview - upright for the way the interface is turned. A Mac's camera is upright as it
    /// comes.
    /// </summary>
#if IOS && !MACCATALYST
    private void OnHostSizeChanged(object? sender, EventArgs e) => Rotate();

    /// <summary>Rotates now, and again once the interface has had time to turn after the device.</summary>
    private void RotateWithInterface()
    {
        Rotate();
        _ = Task.Delay(500).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(Rotate), TaskScheduler.Default);
    }
#endif

    private void Rotate()
    {
#if IOS && !MACCATALYST
        if (IsDisposed)
        {
            return;
        }

        var scene = UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().FirstOrDefault();
        var orientation = scene is null ? UIInterfaceOrientation.Portrait
            : OperatingSystem.IsIOSVersionAtLeast(26) ? scene.EffectiveGeometry.InterfaceOrientation
            : scene.InterfaceOrientation;
        var angle = orientation switch
        {
            UIInterfaceOrientation.LandscapeRight => 0,
            UIInterfaceOrientation.LandscapeLeft => 180,
            UIInterfaceOrientation.PortraitUpsideDown => 270,
            _ => 90,
        };

        foreach (var connection in new[] { _output.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!), _view.PreviewLayer?.Connection })
        {
            if (connection is null)
            {
                continue;
            }

            if (OperatingSystem.IsIOSVersionAtLeast(17))
            {
                if (connection.IsVideoRotationAngleSupported(angle))
                {
                    connection.VideoRotationAngle = angle;
                }
            }
            else if (connection.SupportsVideoOrientation)
            {
                connection.VideoOrientation = orientation switch
                {
                    UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
                    UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
                    UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
                    _ => AVCaptureVideoOrientation.Portrait,
                };
            }
        }
#endif
    }

    private unsafe void OnFrame(CVPixelBuffer pixels)
    {
        if (pixels.Lock(CVPixelBufferLock.ReadOnly) != CVReturn.Success)
        {
            return;
        }

        try
        {
            var plane = (byte*)pixels.GetBaseAddress(0);
            if (plane != null)
            {
                Deliver(plane, (int)pixels.GetWidthOfPlane(0), (int)pixels.GetHeightOfPlane(0), (int)pixels.GetBytesPerRowOfPlane(0));
            }
        }
        finally
        {
            pixels.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    private sealed class FrameDelegate(AppleCameraSession session) : AVCaptureVideoDataOutputSampleBufferDelegate
    {
        public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            try
            {
                if (sampleBuffer.GetImageBuffer() is CVPixelBuffer pixels)
                {
                    using (pixels)
                    {
                        session.OnFrame(pixels);
                    }
                }
            }
            finally
            {
                // Back to the camera at once: AVFoundation stops delivering while its few buffers are held.
                sampleBuffer.Dispose();
            }
        }
    }
}
#endif
