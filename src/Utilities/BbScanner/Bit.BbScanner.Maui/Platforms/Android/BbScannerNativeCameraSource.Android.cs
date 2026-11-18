namespace Bit.BbScanner;

internal sealed partial class BbScannerNativeCameraSource
{
    private static partial bool PlatformSupported() => BbScannerAndroidCameraSession.HasCamera();

    private static partial bool PlatformInBackground() => BbScannerAndroidCameraSession.InBackground();

    private static partial Task<IReadOnlyList<BbScannerCameraDevice>> GetPlatformCamerasAsync() => BbScannerAndroidCameraSession.ListAsync();

    private static partial Task<IBbScannerCameraSession> OpenPlatformAsync(BbScannerPreviewClaim preview, BbScannerCameraRequest request, CancellationToken cancellationToken)
        => BbScannerAndroidCameraSession.OpenAsync(preview, request, cancellationToken);
}
