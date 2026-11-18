namespace Bit.BbScanner;

/// <summary>
/// The device's own camera, for the scanners of a .NET MAUI Blazor Hybrid app: CameraX on Android, AVFoundation on iOS
/// and Mac Catalyst, MediaCapture on Windows. <see cref="BbScannerMauiAppBuilderExtensions.UseBbScanner"/> registers it.
/// </summary>
internal sealed partial class BbScannerNativeCameraSource : IBbScannerCameraSource
{
#if ANDROID
    private const string AskedKey = "bit.bbscanner.camera-asked";
#endif

    // Without a host there is nowhere to draw the preview: the scanner opens the WebView's camera instead.
    public bool IsSupported => PlatformSupported() && BbScannerHost.Active is not null;

    public bool IsInBackground => PlatformInBackground();

    public async Task<BbScannerCameraPermission> GetPermissionAsync()
    {
        var status = await MainThread.InvokeOnMainThreadAsync(Permissions.CheckStatusAsync<Permissions.Camera>).ConfigureAwait(false);
#if ANDROID
        // Android reports a permission it has never asked for as denied, and one the user refused once as denied too,
        // though it asks again for either.
        if (status == PermissionStatus.Denied
            && (!Preferences.Default.Get(AskedKey, false) || await MainThread.InvokeOnMainThreadAsync(Permissions.ShouldShowRationale<Permissions.Camera>).ConfigureAwait(false)))
        {
            return BbScannerCameraPermission.Prompt;
        }
#endif
        return status switch
        {
            PermissionStatus.Granted or PermissionStatus.Limited => BbScannerCameraPermission.Granted,
            PermissionStatus.Denied or PermissionStatus.Restricted => BbScannerCameraPermission.Denied,
            PermissionStatus.Unknown => BbScannerCameraPermission.Prompt,
            _ => BbScannerCameraPermission.Unknown,
        };
    }

    public Task<IReadOnlyList<BbScannerCameraDevice>> GetCamerasAsync() => GetPlatformCamerasAsync();

    public async Task<IBbScannerCameraSession> OpenAsync(BbScannerCameraRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var host = BbScannerHost.Active ?? throw new BbScannerException(
            BbScannerErrorKind.NotSupported,
            "The device's camera draws its preview behind the BlazorWebView: wrap the BlazorWebView in a BbScannerHost.");

        // The prompt is the platform's own, asked for on its UI thread.
        var status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.Camera>).ConfigureAwait(false);
#if ANDROID
        Preferences.Default.Set(AskedKey, true);
#endif
        if (status is not (PermissionStatus.Granted or PermissionStatus.Limited))
        {
            throw new BbScannerException(BbScannerErrorKind.PermissionDenied, "Camera permission was denied.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var preview = await MainThread.InvokeOnMainThreadAsync(host.Claim).ConfigureAwait(false);
        IBbScannerCameraSession session;
        try
        {
            session = await OpenPlatformAsync(preview, request, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            preview.Release();
            throw;
        }

        try
        {
            if (session is BbScannerNativeCameraSession native)
            {
                await native.WaitForFirstFrameAsync(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return session;
    }

    private static partial bool PlatformSupported();

    private static partial bool PlatformInBackground();

    private static partial Task<IReadOnlyList<BbScannerCameraDevice>> GetPlatformCamerasAsync();

    private static partial Task<IBbScannerCameraSession> OpenPlatformAsync(BbScannerPreviewClaim preview, BbScannerCameraRequest request, CancellationToken cancellationToken);
}
