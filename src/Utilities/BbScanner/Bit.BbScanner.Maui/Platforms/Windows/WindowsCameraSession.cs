using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Panel = Windows.Devices.Enumeration.Panel;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using WinRT;

namespace Bit.BbScanner;

/// <summary>
/// A camera opened through MediaCapture: a frame reader whose NV12 frames give the scanner their luminance plane, the
/// latest only, and a media player drawing the same source into the host's MediaPlayerElement.
/// </summary>
internal sealed class WindowsCameraSession : BbNativeCameraSession
{
    private readonly MediaCapture _capture;
    private readonly MediaFrameSource _source;
    private readonly MediaFrameReader _reader;
    private readonly MediaPlayerElement _view;
    private MediaPlayer? _player;

    private WindowsCameraSession(BbPreviewClaim claim, BbCameraInfo camera, MediaCapture capture, MediaFrameSource source,
        MediaFrameReader reader, MediaPlayerElement view)
        : base(claim, camera)
    {
        _capture = capture;
        _source = source;
        _reader = reader;
        _view = view;
        capture.Failed += OnFailed;
        reader.FrameArrived += OnFrameArrived;
    }

    /// <summary>Opens the camera the request asks for, falling back to whichever there is.</summary>
    public static async Task<IBbCameraSession> OpenAsync(BbPreviewClaim claim, BbCameraRequest request, CancellationToken cancellationToken)
    {
        var view = await MainThread.InvokeOnMainThreadAsync(() => claim.Preview.Handler?.PlatformView as MediaPlayerElement).ConfigureAwait(false)
            ?? throw new BbScannerException(BbScannerErrorKind.NotSupported, "The scanner host's preview is not on screen yet.");

        var selected = await SelectAsync(request).ConfigureAwait(false)
            ?? throw new BbScannerException(BbScannerErrorKind.NoCamera, "No camera matching the request was found.");

        var capture = new MediaCapture();
        try
        {
            await capture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                SourceGroup = selected.Group,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            capture.Dispose();
            throw new BbScannerException(BbScannerErrorKind.PermissionDenied, "Camera access is turned off for this app in Windows' privacy settings.", ex);
        }
        catch (Exception ex)
        {
            capture.Dispose();
            throw new BbScannerException(BbScannerErrorKind.CameraUnavailable, "The camera could not be opened; another application may be using it.", ex);
        }

        WindowsCameraSession? camera = null;
        MediaFrameReader? reader = null;
        try
        {
            var source = capture.FrameSources[selected.Info.Id];

            // The request is the frame size to aim for: the closest the camera has, uncompressed formats first.
            var target = (long)request.Width * request.Height;
            var format = source.SupportedFormats
                .Where(f => f.VideoFormat is not null && f.FrameRate.Denominator != 0 && f.FrameRate.Numerator / (double)f.FrameRate.Denominator >= 14)
                .OrderBy(f => string.Equals(f.Subtype, MediaEncodingSubtypes.Mjpg, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(f => Math.Abs(((long)f.VideoFormat.Width * f.VideoFormat.Height) - target))
                .FirstOrDefault();
            if (format is not null)
            {
                await source.SetFormatAsync(format);
            }

            reader = await capture.CreateFrameReaderAsync(source, MediaEncodingSubtypes.Nv12);
            reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            var front = selected.Info.DeviceInformation?.EnclosureLocation?.Panel == Panel.Front;
            var controller = capture.VideoDeviceController;
            var zoom = controller.ZoomControl;
            camera = new WindowsCameraSession(claim, new BbCameraInfo
            {
                DeviceId = selected.Group.Id,
                Label = selected.Group.DisplayName,
                Width = (int)(format?.VideoFormat.Width ?? (uint)request.Width),
                Height = (int)(format?.VideoFormat.Height ?? (uint)request.Height),
                FrameRate = format is null ? 0 : format.FrameRate.Numerator / (double)format.FrameRate.Denominator,
                FacingMode = front ? "user" : selected.Info.DeviceInformation?.EnclosureLocation?.Panel == Panel.Back ? "environment" : "",
                SupportsTorch = controller.TorchControl.Supported,
                SupportsZoom = zoom.Supported && zoom.Max > zoom.Min,
                ZoomMin = zoom.Supported ? zoom.Min : 0,
                ZoomMax = zoom.Supported ? zoom.Max : 0,
                ZoomStep = zoom.Supported ? zoom.Step : 0,
                Zoom = zoom.Supported ? zoom.Value : 0,
            }, capture, source, reader, view);

            if (await reader.StartAsync() != MediaFrameReaderStartStatus.Success)
            {
                throw new BbScannerException(BbScannerErrorKind.CameraUnavailable, "The camera could not be started.");
            }

            var opened = camera;
            await MainThread.InvokeOnMainThreadAsync(() => opened.ShowPreview(front)).ConfigureAwait(false);
            return opened;
        }
        catch (Exception ex)
        {
            // The session closes everything it was given; before there is one, what there is so far is closed here.
            if (camera is not null)
            {
                await camera.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                reader?.Dispose();
                capture.Dispose();
            }

            throw ex as BbScannerException ?? new BbScannerException(BbScannerErrorKind.CameraUnavailable, "The camera could not be started.", ex);
        }
    }

    public override Task<bool> SetTorchAsync(bool on)
    {
        var torch = _capture.VideoDeviceController.TorchControl;
        if (!torch.Supported || IsDisposed)
        {
            return Task.FromResult(false);
        }

        torch.Enabled = on;
        Describe(camera => camera.TorchOn = on);
        return Task.FromResult(true);
    }

    public override Task<bool> SetZoomAsync(double zoom)
    {
        var control = _capture.VideoDeviceController.ZoomControl;
        if (!control.Supported || IsDisposed)
        {
            return Task.FromResult(false);
        }

        var value = (float)Math.Clamp(zoom, control.Min, control.Max);
        control.Value = value;
        Describe(camera => camera.Zoom = value);
        return Task.FromResult(true);
    }

    protected override async ValueTask CloseAsync()
    {
        _reader.FrameArrived -= OnFrameArrived;
        _capture.Failed -= OnFailed;
        await _reader.StopAsync();
        _reader.Dispose();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // The view may already show the camera that replaced this one.
            if (_player is not null && _view.MediaPlayer == _player)
            {
                _view.SetMediaPlayer(null);
            }

            _player?.Dispose();
            _player = null;
        }).ConfigureAwait(false);
        _capture.Dispose();
    }

    /// <summary>The cameras MediaCapture can open: every source group with a colour source.</summary>
    public static async Task<IReadOnlyList<BbCameraDevice>> ListAsync()
    {
        var groups = await MediaFrameSourceGroup.FindAllAsync();
        return [.. groups.Where(group => ColourSource(group) is not null).Select(group => new BbCameraDevice(group.Id, group.DisplayName))];
    }

    private static async Task<(MediaFrameSourceGroup Group, MediaFrameSourceInfo Info)?> SelectAsync(BbCameraRequest request)
    {
        var groups = await MediaFrameSourceGroup.FindAllAsync();
        var candidates = groups
            .Select(group => (Group: group, Info: ColourSource(group)))
            .Where(candidate => candidate.Info is not null)
            .Select(candidate => (candidate.Group, Info: candidate.Info!))
            .ToList();

        if (request.DeviceId is { Length: > 0 } id && candidates.FirstOrDefault(c => c.Group.Id == id) is { Info: not null } chosen)
        {
            return chosen;
        }

        var panel = request.Facing == BbCameraFacing.Front ? Panel.Front : Panel.Back;
        var facing = candidates.FirstOrDefault(c => c.Info.DeviceInformation?.EnclosureLocation?.Panel == panel);
        return facing.Info is not null ? facing : candidates.Count > 0 ? candidates[0] : null;
    }

    /// <summary>A group's colour source, the recording stream before the preview one.</summary>
    private static MediaFrameSourceInfo? ColourSource(MediaFrameSourceGroup group)
        => group.SourceInfos
            .Where(info => info.SourceKind == MediaFrameSourceKind.Color)
            .OrderBy(info => info.MediaStreamType == MediaStreamType.VideoRecord ? 0 : info.MediaStreamType == MediaStreamType.VideoPreview ? 1 : 2)
            .FirstOrDefault();

    private void ShowPreview(bool mirrored)
    {
        _player = new MediaPlayer { RealTimePlayback = true, AutoPlay = true, IsMuted = true };
        _player.Source = MediaSource.CreateFromMediaFrameSource(_source);
        _view.SetMediaPlayer(_player);

        // A front camera is shown as a mirror would show it, as a browser shows it.
        _view.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        _view.RenderTransform = mirrored ? new ScaleTransform { ScaleX = -1 } : null;
    }

    private void OnFailed(MediaCapture sender, MediaCaptureFailedEventArgs args) => State = BbCameraSessionState.Ended;

    private unsafe void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (IsDisposed)
        {
            return;
        }

        // A frame raced by the reader stopping is one frame fewer; the camera thread is not the place to fail.
        try
        {
            using var frame = sender.TryAcquireLatestFrame();
            if (frame?.VideoMediaFrame?.SoftwareBitmap is not { } bitmap)
            {
                return;
            }

            using (bitmap)
            {
                if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Nv12)
                {
                    return;
                }

                // NV12 is the luminance plane, then the chroma: the plane's own description says where the first ends.
                // The luminance is copied straight out of the bitmap's own memory while it is locked.
                using var locked = bitmap.LockBuffer(BitmapBufferAccessMode.Read);
                var plane = locked.GetPlaneDescription(0);
                using var reference = locked.CreateReference();
                reference.As<IMemoryBufferByteAccess>().GetBuffer(out var data, out var capacity);
                if (data != null && plane.StartIndex + ((long)plane.Stride * plane.Height) <= capacity)
                {
                    Deliver(data + plane.StartIndex, plane.Width, plane.Height, plane.Stride);
                }
            }
        }
        catch (Exception ex) when (ex is COMException or ObjectDisposedException or InvalidOperationException)
        {
        }
    }

    /// <summary>The raw memory behind a locked bitmap.</summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
