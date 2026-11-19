namespace Bit.BbScanner;

/// <summary>
/// A camera of the platform's own, which the scanner reads in place of the page's: in a Blazor Hybrid app, the device's
/// camera through its native API. <c>Bit.BbScanner.Maui</c> registers one for .NET MAUI.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a service, it is used wherever .NET does not run in the page - everywhere but WebAssembly, where the
/// page's camera is the fastest there is. Its frames go from the platform straight to the decoder, in the app's own
/// process and at the camera's own resolution, without crossing into the page; the page only hosts the scanner's
/// overlays and controls, as anywhere else, over a preview the source draws itself where the scanner's video element is.
/// </para>
/// <para>
/// Where the source is not <see cref="IsSupported"/>, the scanner opens the page's camera instead, as it does with no
/// source registered at all.
/// </para>
/// </remarks>
public interface IBbScannerCameraSource
{
    /// <summary>Whether a camera can be opened here at all: the platform has a camera API and the app is set up for it.</summary>
    bool IsSupported { get; }

    /// <summary>Whether the app is in the background, where the platform does not let it use the camera.</summary>
    bool IsInBackground { get; }

    /// <summary>What opening a camera would do about permission, without asking the user.</summary>
    Task<BbScannerCameraPermission> GetPermissionAsync();

    /// <summary>The cameras that can be opened.</summary>
    Task<IReadOnlyList<BbScannerCameraDevice>> GetCamerasAsync();

    /// <summary>
    /// Opens a camera, asking the user for permission first where the platform needs it, and shows its preview.
    /// </summary>
    /// <param name="request">Which camera, at about which size.</param>
    /// <param name="cancellationToken">Abandons the request; a camera that opens afterwards is closed again.</param>
    /// <returns>The open camera.</returns>
    /// <exception cref="BbScannerException">
    /// The camera could not be opened: <see cref="BbScannerErrorKind.PermissionDenied"/>,
    /// <see cref="BbScannerErrorKind.NoCamera"/>, <see cref="BbScannerErrorKind.CameraUnavailable"/> or
    /// <see cref="BbScannerErrorKind.NotSupported"/>.
    /// </exception>
    Task<IBbScannerCameraSession> OpenAsync(BbScannerCameraRequest request, CancellationToken cancellationToken = default);
}
