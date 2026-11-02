namespace Bit.BbScanner;

internal sealed partial class BbNativeCameraSource
{
    private static partial bool PlatformSupported() => AndroidCameraSession.HasCamera();

    private static partial bool PlatformInBackground() => AndroidCameraSession.InBackground();

    private static partial Task<IReadOnlyList<BbCameraDevice>> GetPlatformCamerasAsync() => AndroidCameraSession.ListAsync();

    private static partial Task<IBbCameraSession> OpenPlatformAsync(BbPreviewClaim preview, BbCameraRequest request, CancellationToken cancellationToken)
        => AndroidCameraSession.OpenAsync(preview, request, cancellationToken);
}
