namespace Bit.BbScanner;

internal sealed partial class BbScannerNativeCameraSource
{
    // Whether there is a camera is only known asynchronously on Windows; opening one without reports NoCamera.
    private static partial bool PlatformSupported() => true;

    private static partial bool PlatformInBackground() => false;

    private static partial Task<IReadOnlyList<BbScannerCameraDevice>> GetPlatformCamerasAsync() => BbScannerWindowsCameraSession.ListAsync();

    private static partial Task<IBbScannerCameraSession> OpenPlatformAsync(BbScannerPreviewClaim preview, BbScannerCameraRequest request, CancellationToken cancellationToken)
        => BbScannerWindowsCameraSession.OpenAsync(preview, request, cancellationToken);
}
