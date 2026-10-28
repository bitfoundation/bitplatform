namespace Bit.BbScanner;

internal sealed partial class BbNativeCameraSource
{
    // Whether there is a camera is only known asynchronously on Windows; opening one without reports NoCamera.
    private static partial bool PlatformSupported() => true;

    private static partial bool PlatformInBackground() => false;

    private static partial Task<IReadOnlyList<BbCameraDevice>> GetPlatformCamerasAsync() => WindowsCameraSession.ListAsync();

    private static partial Task<IBbCameraSession> OpenPlatformAsync(BbPreviewClaim preview, BbCameraRequest request, CancellationToken cancellationToken)
        => WindowsCameraSession.OpenAsync(preview, request, cancellationToken);
}
