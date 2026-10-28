using Microsoft.Extensions.DependencyInjection;

namespace Bit.BbScanner;

/// <summary>Sets a .NET MAUI app up for scanning with the device's own camera.</summary>
public static class BbScannerMauiAppBuilderExtensions
{
    /// <summary>
    /// Registers the device's camera as the scanners' <see cref="IBbCameraSource"/>, and the preview that draws it in a
    /// <see cref="BbScannerHost"/>. Wrap the app's BlazorWebView in a BbScannerHost, and declare the camera permission on
    /// each platform: <c>android.permission.CAMERA</c> on Android, <c>NSCameraUsageDescription</c> on iOS and Mac
    /// Catalyst (and the <c>com.apple.security.device.camera</c> entitlement in a sandboxed Mac app), and the
    /// <c>webcam</c> capability on Windows.
    /// </summary>
    /// <param name="builder">The app's builder.</param>
    /// <returns>The same builder.</returns>
    public static MauiAppBuilder UseBbScanner(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<BbCameraPreview, BbCameraPreviewHandler>());
        builder.Services.AddSingleton<IBbCameraSource, BbNativeCameraSource>();
        return builder;
    }
}
