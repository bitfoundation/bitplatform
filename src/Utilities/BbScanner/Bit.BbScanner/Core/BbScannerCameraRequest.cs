namespace Bit.BbScanner;

/// <summary>Which camera the scanner asks an <see cref="IBbScannerCameraSource"/> for.</summary>
public sealed class BbScannerCameraRequest
{
    /// <summary>A specific camera, from <see cref="IBbScannerCameraSource.GetCamerasAsync"/>, or <see langword="null"/> to go by <see cref="Facing"/>.</summary>
    public string? DeviceId { get; init; }

    /// <summary>Which camera to prefer when <see cref="DeviceId"/> is not set.</summary>
    public BbScannerCameraFacing Facing { get; init; } = BbScannerCameraFacing.Rear;

    /// <summary>The frame width to aim for; the camera delivers what it has that is closest.</summary>
    public int Width { get; init; } = 1280;

    /// <summary>The frame height to aim for.</summary>
    public int Height { get; init; } = 720;
}
