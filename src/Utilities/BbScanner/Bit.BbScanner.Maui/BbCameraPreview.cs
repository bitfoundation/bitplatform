using Microsoft.Maui.Handlers;
#if ANDROID
using PlatformPreview = AndroidX.Camera.View.PreviewView;
#elif IOS || MACCATALYST
using PlatformPreview = Bit.BbScanner.BbPreviewView;
#elif WINDOWS
using PlatformPreview = Microsoft.UI.Xaml.Controls.MediaPlayerElement;
#endif

namespace Bit.BbScanner;

/// <summary>The view a native camera draws its preview into, inside a <see cref="BbScannerHost"/>.</summary>
internal sealed class BbCameraPreview : View;

/// <summary>
/// The platform's own preview view: CameraX's PreviewView on Android, a layer AVFoundation draws into on iOS and Mac
/// Catalyst, a MediaPlayerElement on Windows. Each scales the camera to cover its bounds, as the page's video element
/// does, so the scanner's overlays line up with it.
/// </summary>
internal sealed partial class BbCameraPreviewHandler() : ViewHandler<BbCameraPreview, PlatformPreview>(ViewMapper);
