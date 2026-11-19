using Microsoft.Maui.Handlers;
#if ANDROID
using PlatformPreview = AndroidX.Camera.View.PreviewView;
#elif IOS || MACCATALYST
using PlatformPreview = Bit.BbScanner.BbScannerPreviewView;
#elif WINDOWS
using PlatformPreview = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
#endif

namespace Bit.BbScanner;

/// <summary>
/// The platform's own preview view: CameraX's PreviewView on Android, a layer AVFoundation draws into on iOS and Mac
/// Catalyst, a MediaPlayerElement on Windows. Each scales the camera to cover its bounds, as the page's video element
/// does, so the scanner's overlays line up with it.
/// </summary>
internal sealed partial class BbScannerCameraPreviewHandler() : ViewHandler<BbScannerCameraPreview, PlatformPreview>(ViewMapper);
