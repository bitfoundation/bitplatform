namespace Bit.BbScanner;

/// <summary>
/// What every platform's camera session shares: the latest frame, kept as luminance and scaled on request, the camera's
/// description, and the preview it draws in a <see cref="BbScannerHost"/>.
/// </summary>
/// <remarks>
/// <para>
/// The platform hands frames over on a thread of its own through <see cref="Deliver"/>, which copies the luminance plane
/// out of the platform's buffer - so the buffer goes back to the camera at once - into one array the session keeps.
/// A frame that arrives before the last was read replaces it: the scanner always reads the newest, and a slow decode
/// never holds up the camera.
/// </para>
/// <para>
/// The description is replaced, never changed: a frame of another shape - the device turned - publishes a new
/// <see cref="BbCameraInfo"/>, which is how the scanner tells that it has to be sized again.
/// </para>
/// </remarks>
internal abstract class BbNativeCameraSession : IBbCameraSession
{
    private readonly Lock _gate = new();
    private readonly Lock _description = new();
    private byte[] _frame = [];
    private int _frameWidth;
    private int _frameHeight;
    private long _frameCount;
    private int[] _columns = [];
    private int _disposed;
    private readonly TaskCompletionSource _firstFrame = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Published from the frame thread, read from the scanner's.
    private volatile BbCameraInfo _camera;

    protected BbNativeCameraSession(BbPreviewClaim preview, BbCameraInfo camera)
    {
        Preview = preview;
        _camera = camera;
    }

    /// <summary>Where the preview is drawn.</summary>
    protected BbPreviewClaim Preview { get; }

    public BbCameraInfo Camera => _camera;

    public BbCameraSessionState State { get; protected set; }

    public long FrameCount => Interlocked.Read(ref _frameCount);

    /// <summary>Whether the session has been disposed, for the platform's callbacks to stop delivering.</summary>
    protected bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>
    /// Takes a frame from the platform: <paramref name="height"/> rows of <paramref name="width"/> luminance bytes,
    /// <paramref name="stride"/> bytes apart, starting at <paramref name="plane"/>, already upright.
    /// </summary>
    protected unsafe void Deliver(byte* plane, int width, int height, int stride)
    {
        if (IsDisposed || width <= 0 || height <= 0)
        {
            return;
        }

        lock (_gate)
        {
            // Disposal clears the frame under this lock: a frame that arrives while it does is not kept after it.
            if (IsDisposed)
            {
                return;
            }

            var length = width * height;
            if (_frame.Length != length)
            {
                Array.Clear(_frame);
                _frame = new byte[length];
            }

            fixed (byte* target = _frame)
            {
                if (stride == width)
                {
                    Buffer.MemoryCopy(plane, target, length, length);
                }
                else
                {
                    for (var row = 0; row < height; row++)
                    {
                        Buffer.MemoryCopy(plane + ((long)row * stride), target + ((long)row * width), width, width);
                    }
                }
            }

            _frameWidth = width;
            _frameHeight = height;
        }

        if (width != Camera.Width || height != Camera.Height)
        {
            Describe(camera => { camera.Width = width; camera.Height = height; });
        }

        // A frame says an interrupted camera is back; one that ended for good stays ended.
        if (State == BbCameraSessionState.Interrupted)
        {
            State = BbCameraSessionState.Running;
        }

        Interlocked.Increment(ref _frameCount);
        _firstFrame.TrySetResult();
    }

    /// <summary>
    /// Waits for the first frame, which says the size the camera actually delivers - upright, in the orientation the
    /// device is held in - so that the scanner is sized for it from the start. A camera slower than that is described as
    /// asked for until its first frame arrives.
    /// </summary>
    internal async Task WaitForFirstFrameAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            await _firstFrame.Task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Described as asked for; the first frame corrects it.
        }
    }

    public long ReadFrame(Span<byte> destination, int width, int height)
    {
        lock (_gate)
        {
            if (_frameWidth == 0 || width <= 0 || height <= 0 || destination.Length < width * height)
            {
                return 0;
            }

            Scale(_frame, _frameWidth, _frameHeight, destination, width, height);
            return Interlocked.Read(ref _frameCount);
        }
    }

    public abstract Task<bool> SetTorchAsync(bool on);

    public abstract Task<bool> SetZoomAsync(double zoom);

    public void PlacePreview(BbPreviewPlacement placement)
    {
        if (!IsDisposed)
        {
            Preview.Place(placement);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            Preview.Release();

            // Nothing keeps the last thing the camera saw once it is closed.
            lock (_gate)
            {
                Array.Clear(_frame);
                _frame = [];
                _frameWidth = 0;
                _frameHeight = 0;
            }
        }
    }

    /// <summary>Closes the platform's camera.</summary>
    protected abstract ValueTask CloseAsync();

    /// <summary>Publishes a new description of the camera: a copy of the current one, changed.</summary>
    protected void Describe(Action<BbCameraInfo> change)
    {
        // A frame of a new size and a torch turned on at once each change a copy of the one before: neither is lost.
        lock (_description)
        {
            var current = Camera;
            var next = new BbCameraInfo
            {
                DeviceId = current.DeviceId,
                Label = current.Label,
                Width = current.Width,
                Height = current.Height,
                FrameRate = current.FrameRate,
                FacingMode = current.FacingMode,
                SupportsTorch = current.SupportsTorch,
                TorchOn = current.TorchOn,
                SupportsZoom = current.SupportsZoom,
                ZoomMin = current.ZoomMin,
                ZoomMax = current.ZoomMax,
                ZoomStep = current.ZoomStep,
                Zoom = current.Zoom,
            };
            change(next);
            _camera = next;
        }
    }

    /// <summary>
    /// Scales luminance by averaging the source pixels each destination pixel covers - a box filter, which keeps the
    /// thin bars and modules a scaled down frame would otherwise alias away - or by picking the nearest when it grows.
    /// </summary>
    private void Scale(byte[] source, int sourceWidth, int sourceHeight, Span<byte> destination, int width, int height)
    {
        if (sourceWidth == width && sourceHeight == height)
        {
            source.AsSpan(0, width * height).CopyTo(destination);
            return;
        }

        // Where each destination column starts in the source, and one past the last: the next column's start.
        if (_columns.Length != width + 1)
        {
            _columns = new int[width + 1];
        }

        for (var x = 0; x <= width; x++)
        {
            _columns[x] = (int)((long)x * sourceWidth / width);
        }

        for (var y = 0; y < height; y++)
        {
            var top = (int)((long)y * sourceHeight / height);
            var bottom = Math.Max(top + 1, (int)((long)(y + 1) * sourceHeight / height));
            var row = destination.Slice(y * width, width);
            for (var x = 0; x < width; x++)
            {
                var left = _columns[x];
                var right = Math.Max(left + 1, _columns[x + 1]);
                var sum = 0;
                for (var sy = top; sy < bottom; sy++)
                {
                    var offset = sy * sourceWidth;
                    for (var sx = left; sx < right; sx++)
                    {
                        sum += source[offset + sx];
                    }
                }

                row[x] = (byte)(sum / ((bottom - top) * (right - left)));
            }
        }
    }
}
