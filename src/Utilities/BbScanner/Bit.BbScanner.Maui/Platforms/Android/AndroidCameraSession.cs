using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using AndroidX.Camera.Camera2.InterOp;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Google.Common.Util.Concurrent;
using Java.Util.Concurrent;
using AndroidSize = Android.Util.Size;
using CameraXSelector = AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector;

namespace Bit.BbScanner;

/// <summary>
/// A camera opened through CameraX: a Preview use case drawing into the host's PreviewView, and an ImageAnalysis use
/// case whose frames - YUV, rotated upright by CameraX, the latest only - give the scanner their luminance plane.
/// </summary>
/// <remarks>
/// The use cases are bound to the activity's lifecycle, so CameraX closes the camera while the app is in the background
/// and opens it again when it comes back, by itself.
/// </remarks>
internal sealed class AndroidCameraSession : BbNativeCameraSession
{
    private readonly ProcessCameraProvider _provider;
    private readonly ILifecycleOwner _owner;
    private readonly Preview _preview;
    private readonly ImageAnalysis _analysis;
    private readonly IExecutorService _executor;
    private readonly ICamera _camera;
    private readonly StateObserver _observer;
    private readonly DisplayListener _display;

    private AndroidCameraSession(BbPreviewClaim claim, BbCameraInfo info, ProcessCameraProvider provider, ILifecycleOwner owner,
        Preview preview, ImageAnalysis analysis, IExecutorService executor, ICamera camera)
        : base(claim, info)
    {
        _provider = provider;
        _owner = owner;
        _preview = preview;
        _analysis = analysis;
        _executor = executor;
        _camera = camera;
        _observer = new StateObserver(this);

        _display = new DisplayListener(this);

        analysis.SetAnalyzer(executor, new Analyzer(this));
        camera.CameraInfo!.CameraState!.Observe(owner, _observer);

        // An activity that handles rotation itself, as MAUI's does, is not recreated when the device turns: the
        // display says so instead, and the use cases are told the new rotation.
        DisplayManager?.RegisterDisplayListener(_display, null);
    }

    private static Android.Hardware.Display.DisplayManager? DisplayManager
        => Platform.AppContext.GetSystemService(Android.Content.Context.DisplayService) as Android.Hardware.Display.DisplayManager;

    /// <summary>Opens the camera the request asks for, falling back to whichever there is.</summary>
    public static async Task<IBbCameraSession> OpenAsync(BbPreviewClaim claim, BbCameraRequest request, CancellationToken cancellationToken)
    {
        var provider = await GetProviderAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (Platform.CurrentActivity is not ILifecycleOwner owner)
            {
                throw new BbScannerException(BbScannerErrorKind.NotSupported, "The app's activity has no lifecycle to bind the camera to.");
            }

            if (claim.Preview.Handler?.PlatformView is not PreviewView view)
            {
                throw new BbScannerException(BbScannerErrorKind.NotSupported, "The scanner host's preview is not on screen yet.");
            }

            var selector = Select(provider, request) ?? throw new BbScannerException(BbScannerErrorKind.NoCamera, "No camera matching the request was found.");

            // The request is the frame size to aim for: the closest the camera has, larger first.
            var resolution = new CameraXSelector.Builder()
                .SetResolutionStrategy(new ResolutionStrategy(new AndroidSize(request.Width, request.Height), ResolutionStrategy.FallbackRuleClosestHigherThenLower))!
                .Build()!;
            var rotation = CurrentRotation();

            var preview = new Preview.Builder().SetResolutionSelector(resolution)!.SetTargetRotation(rotation)!.Build()!;
            var analysis = new ImageAnalysis.Builder()
                .SetResolutionSelector(resolution)!
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)!
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatYuv420888)!
                .SetOutputImageRotationEnabled(true)!
                .SetTargetRotation(rotation)!
                .Build()!;

            preview.SurfaceProvider = view.SurfaceProvider;

            ICamera camera;
            try
            {
                camera = provider.BindToLifecycle(owner, selector, [preview, analysis])!;
            }
            catch (Java.Lang.Exception ex)
            {
                preview.SurfaceProvider = null;
                throw new BbScannerException(BbScannerErrorKind.CameraUnavailable, "The camera could not be opened.", ex);
            }

            IExecutorService? executor = null;
            try
            {
                var info = camera.CameraInfo!;
                var zoom = info.ZoomState?.Value?.JavaCast<IZoomState>();
                var front = info.LensFacing == CameraSelector.LensFacingFront;
                var described = new BbCameraInfo
                {
                    DeviceId = IdOf(info),
                    Label = LabelOf(info),
                    Width = request.Width,
                    Height = request.Height,
                    FacingMode = front ? "user" : info.LensFacing == CameraSelector.LensFacingBack ? "environment" : "",
                    SupportsTorch = info.HasFlashUnit,
                    SupportsZoom = zoom is not null && zoom.MaxZoomRatio > zoom.MinZoomRatio,
                    ZoomMin = zoom?.MinZoomRatio ?? 0,
                    ZoomMax = zoom?.MaxZoomRatio ?? 0,
                    ZoomStep = 0.1,
                    Zoom = zoom?.ZoomRatio ?? 0,
                };

                executor = Executors.NewSingleThreadExecutor()!;
                return (IBbCameraSession)new AndroidCameraSession(claim, described, provider, owner, preview, analysis, executor, camera);
            }
            catch (Exception ex)
            {
                // Bound but never handed over: nothing else would ever close it.
                analysis.ClearAnalyzer();
                preview.SurfaceProvider = null;
                provider.Unbind([preview, analysis]);
                executor?.Shutdown();
                throw ex as BbScannerException ?? new BbScannerException(BbScannerErrorKind.CameraUnavailable, "The camera could not be opened.", ex);
            }
        }).ConfigureAwait(false);
    }

    public override async Task<bool> SetTorchAsync(bool on)
    {
        if (!Camera.SupportsTorch || IsDisposed)
        {
            return false;
        }

        try
        {
            await AwaitAsync(_camera.CameraControl!.EnableTorch(on)!).ConfigureAwait(false);
            Describe(camera => camera.TorchOn = on);
            return true;
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or InvalidOperationException)
        {
            return false;
        }
    }

    public override async Task<bool> SetZoomAsync(double zoom)
    {
        if (!Camera.SupportsZoom || IsDisposed)
        {
            return false;
        }

        try
        {
            var ratio = (float)Math.Clamp(zoom, Camera.ZoomMin, Camera.ZoomMax);
            await AwaitAsync(_camera.CameraControl!.SetZoomRatio(ratio)!).ConfigureAwait(false);
            Describe(camera => camera.Zoom = ratio);
            return true;
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or InvalidOperationException)
        {
            return false;
        }
    }

    protected override async ValueTask CloseAsync()
    {
        DisplayManager?.UnregisterDisplayListener(_display);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _camera.CameraInfo?.CameraState?.RemoveObserver(_observer);
            _analysis.ClearAnalyzer();
            _preview.SurfaceProvider = null;
            _provider.Unbind([_preview, _analysis]);
        }).ConfigureAwait(false);
        _executor.Shutdown();
    }

    /// <summary>The cameras CameraX can open.</summary>
    public static async Task<IReadOnlyList<BbCameraDevice>> ListAsync()
    {
        var provider = await GetProviderAsync().ConfigureAwait(false);
        return await MainThread.InvokeOnMainThreadAsync(() =>
            (IReadOnlyList<BbCameraDevice>)[.. provider.AvailableCameraInfos.Select(info => new BbCameraDevice(IdOf(info), LabelOf(info)))]).ConfigureAwait(false);
    }

    /// <summary>Whether the device has a camera at all.</summary>
    public static bool HasCamera() => Platform.AppContext.PackageManager?.HasSystemFeature(PackageManager.FeatureCameraAny) == true;

    /// <summary>Whether the app's activity is out of sight, where CameraX has closed its camera.</summary>
    public static bool InBackground()
        => Platform.CurrentActivity is not ILifecycleOwner owner || !owner.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started!);

    private static CameraSelector? Select(ProcessCameraProvider provider, BbCameraRequest request)
    {
        if (request.DeviceId is { Length: > 0 } id)
        {
            var match = provider.AvailableCameraInfos.FirstOrDefault(info => IdOf(info) == id);
            if (match is not null)
            {
                return match.CameraSelector;
            }
        }

        // The facing asked for, then whichever camera there is: a tablet with only a front camera still scans.
        CameraSelector[] candidates = request.Facing == BbCameraFacing.Front
            ? [CameraSelector.DefaultFrontCamera!, CameraSelector.DefaultBackCamera!]
            : [CameraSelector.DefaultBackCamera!, CameraSelector.DefaultFrontCamera!];
        foreach (var candidate in candidates)
        {
            if (provider.HasCamera(candidate))
            {
                return candidate;
            }
        }

        return provider.AvailableCameraInfos.FirstOrDefault()?.CameraSelector;
    }

    private static string IdOf(ICameraInfo info) => Camera2CameraInfo.From(info)?.CameraId ?? "";

    private static string LabelOf(ICameraInfo info) => info.LensFacing switch
    {
        var facing when facing == CameraSelector.LensFacingBack => "Back camera",
        var facing when facing == CameraSelector.LensFacingFront => "Front camera",
        _ => "External camera",
    };

    private static int CurrentRotation()
    {
        var activity = Platform.CurrentActivity;
        var rotation = OperatingSystem.IsAndroidVersionAtLeast(30)
            ? activity?.Display?.Rotation
            : activity?.WindowManager?.DefaultDisplay?.Rotation;
        return (int)(rotation ?? SurfaceOrientation.Rotation0);
    }

    /// <summary>The device turned: frames are rotated upright for the new orientation, and the preview follows it.</summary>
    private void OnDisplayChanged()
    {
        if (!IsDisposed)
        {
            var rotation = CurrentRotation();
            _analysis.TargetRotation = rotation;
            _preview.TargetRotation = rotation;
        }
    }

    private void OnState(CameraState state)
    {
        // A camera that closed on its own for good. One taken by another app, or closed while the app is in the
        // background, comes back by itself.
        var code = state.Error?.Code;
        if (code == CameraState.ErrorCameraFatalError || code == CameraState.ErrorCameraDisabled || code == CameraState.ErrorCameraRemoved)
        {
            State = BbCameraSessionState.Ended;
        }
    }

    private unsafe void OnFrame(IImageProxy image)
    {
        IImageProxyPlaneProxy[]? planes = null;
        Java.Nio.ByteBuffer? buffer = null;
        try
        {
            planes = image.GetPlanes();
            if (planes is not { Length: > 0 } || (buffer = planes[0].Buffer) is null)
            {
                return;
            }

            var address = (byte*)buffer.GetDirectBufferAddress();
            if (address != null)
            {
                Deliver(address, image.Width, image.Height, planes[0].RowStride);
            }
        }
        finally
        {
            // Back to the camera at once: with KEEP_ONLY_LATEST it has no other buffer to fill. The managed peers go
            // too, rather than wait for the collector at thirty frames a second.
            buffer?.Dispose();
            foreach (var plane in planes ?? [])
            {
                plane.Dispose();
            }

            image.Close();
            image.Dispose();
        }
    }

    internal static Task<ProcessCameraProvider> GetProviderAsync()
    {
        var context = Platform.AppContext;
        var future = ProcessCameraProvider.GetInstance(context);
        return AwaitAsync(future).ContinueWith(
            task => task.Result!.JavaCast<ProcessCameraProvider>(),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private static Task<Java.Lang.Object?> AwaitAsync(IListenableFuture future)
    {
        var completion = new TaskCompletionSource<Java.Lang.Object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        future.AddListener(new Java.Lang.Runnable(() =>
        {
            try
            {
                completion.TrySetResult(future.Get());
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }), ContextCompat.GetMainExecutor(Platform.AppContext));
        return completion.Task;
    }

    private sealed class Analyzer(AndroidCameraSession session) : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public AndroidSize? DefaultTargetResolution => null;

        public int TargetCoordinateSystem => ImageAnalysis.CoordinateSystemOriginal;

        public void Analyze(IImageProxy? image)
        {
            if (image is not null)
            {
                session.OnFrame(image);
            }
        }

        public void UpdateTransform(Android.Graphics.Matrix? matrix)
        {
        }
    }

    private sealed class DisplayListener(AndroidCameraSession session) : Java.Lang.Object, Android.Hardware.Display.DisplayManager.IDisplayListener
    {
        public void OnDisplayAdded(int displayId)
        {
        }

        public void OnDisplayChanged(int displayId) => session.OnDisplayChanged();

        public void OnDisplayRemoved(int displayId)
        {
        }
    }

    private sealed class StateObserver(AndroidCameraSession session) : Java.Lang.Object, IObserver
    {
        public void OnChanged(Java.Lang.Object? value)
        {
            if (value?.JavaCast<CameraState>() is { } state)
            {
                session.OnState(state);
            }
        }
    }
}
