namespace Bit.BbScanner.Tests.Components;

/// <summary>
/// A platform camera without a platform: what Bit.BbScanner.Maui registers, as far as the scanner can tell - cameras to
/// list, a permission to report, and sessions whose frames are whatever the test puts in front of them.
/// </summary>
internal sealed class FakeCameraSource : IBbCameraSource
{
    public bool IsSupported { get; set; } = true;

    public bool IsInBackground { get; set; }

    public BbCameraPermission Permission { get; set; } = BbCameraPermission.Granted;

    /// <summary>When set, opening a camera fails with it.</summary>
    public Exception? OpenFailure { get; set; }

    /// <summary>When set, a camera opens only once it completes: the platform's prompt, or a slow camera.</summary>
    public Task? OpenDelay { get; set; }

    /// <summary>The scene in front of the camera: luminance at the camera's size.</summary>
    public byte[]? Scene { get; set; }

    public int Width { get; set; } = 640;

    public int Height { get; set; } = 480;

    /// <summary>Every request the scanner made, in order.</summary>
    public List<BbCameraRequest> Requests { get; } = [];

    /// <summary>Every session opened, in order.</summary>
    public List<FakeCameraSession> Sessions { get; } = [];

    public FakeCameraSession? Current => Sessions.LastOrDefault(s => !s.Disposed);

    public Task<BbCameraPermission> GetPermissionAsync() => Task.FromResult(Permission);

    public Task<IReadOnlyList<BbCameraDevice>> GetCamerasAsync()
        => Task.FromResult<IReadOnlyList<BbCameraDevice>>([new("back", "Back camera"), new("front", "Front camera")]);

    public async Task<IBbCameraSession> OpenAsync(BbCameraRequest request, CancellationToken cancellationToken = default)
    {
        lock (Requests)
        {
            Requests.Add(request);
        }

        if (OpenDelay is { } delay)
        {
            await delay.ConfigureAwait(false);
        }

        if (OpenFailure is { } failure)
        {
            throw failure;
        }

        var facing = request.DeviceId == "front" || (request.DeviceId is null && request.Facing == BbCameraFacing.Front) ? "user" : "environment";
        var session = new FakeCameraSession(this, facing);
        lock (Sessions)
        {
            Sessions.Add(session);
        }

        return session;
    }
}

internal sealed class FakeCameraSession(FakeCameraSource source, string facing) : IBbCameraSession
{
    private long _frames;

    public BbCameraInfo Camera { get; private set; } = Describe(source.Width, source.Height, facing);

    public BbCameraSessionState State { get; set; }

    public bool Disposed { get; private set; }

    public bool TorchOn { get; private set; }

    /// <summary>Every placement the scanner gave the preview, in order.</summary>
    public List<BbPreviewPlacement> Placements { get; } = [];

    // A camera delivering a frame every time it is looked at.
    public long FrameCount => source.Scene is null ? 0 : Interlocked.Read(ref _frames) + 1;

    public long ReadFrame(Span<byte> destination, int width, int height)
    {
        if (source.Scene is not { } scene)
        {
            return 0;
        }

        // Nearest neighbour is all a test frame needs.
        for (var y = 0; y < height; y++)
        {
            var sourceRow = y * Camera.Height / height * Camera.Width;
            for (var x = 0; x < width; x++)
            {
                destination[(y * width) + x] = scene[sourceRow + (x * Camera.Width / width)];
            }
        }

        return Interlocked.Increment(ref _frames);
    }

    public Task<bool> SetTorchAsync(bool on)
    {
        TorchOn = on;
        return Task.FromResult(true);
    }

    public Task<bool> SetZoomAsync(double zoom) => Task.FromResult(false);

    public void PlacePreview(BbPreviewPlacement placement)
    {
        lock (Placements)
        {
            Placements.Add(placement);
        }
    }

    /// <summary>The device turned: frames of another shape, under a new description.</summary>
    public void Turn(int width, int height) => Camera = Describe(width, height, facing);

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }

    private static BbCameraInfo Describe(int width, int height, string facing) => new()
    {
        DeviceId = facing == "user" ? "front" : "back",
        Label = facing == "user" ? "Front camera" : "Back camera",
        Width = width,
        Height = height,
        FrameRate = 30,
        FacingMode = facing,
        SupportsTorch = true,
    };
}
