#if IOS || MACCATALYST
namespace Bit.BbScanner;

internal sealed partial class BbScannerNativeCameraSource
{
    private static partial bool PlatformSupported() => BbScannerAppleCameraSession.HasCamera();

    private static partial bool PlatformInBackground() => BbScannerAppleCameraSession.InBackground();

    private static partial Task<IReadOnlyList<BbScannerCameraDevice>> GetPlatformCamerasAsync() => Task.FromResult(BbScannerAppleCameraSession.List());

    private static partial Task<IBbScannerCameraSession> OpenPlatformAsync(BbScannerPreviewClaim preview, BbScannerCameraRequest request, CancellationToken cancellationToken)
        => BbScannerAppleCameraSession.OpenAsync(preview, request, cancellationToken);
}
#endif
