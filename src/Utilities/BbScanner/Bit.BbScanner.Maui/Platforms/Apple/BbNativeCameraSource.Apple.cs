#if IOS || MACCATALYST
namespace Bit.BbScanner;

internal sealed partial class BbNativeCameraSource
{
    private static partial bool PlatformSupported() => AppleCameraSession.HasCamera();

    private static partial bool PlatformInBackground() => AppleCameraSession.InBackground();

    private static partial Task<IReadOnlyList<BbCameraDevice>> GetPlatformCamerasAsync() => Task.FromResult(AppleCameraSession.List());

    private static partial Task<IBbCameraSession> OpenPlatformAsync(BbPreviewClaim preview, BbCameraRequest request, CancellationToken cancellationToken)
        => AppleCameraSession.OpenAsync(preview, request, cancellationToken);
}
#endif
