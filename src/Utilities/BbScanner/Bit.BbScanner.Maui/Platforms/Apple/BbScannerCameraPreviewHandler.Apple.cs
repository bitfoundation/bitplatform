#if IOS || MACCATALYST
namespace Bit.BbScanner;

internal sealed partial class BbScannerCameraPreviewHandler
{
    protected override BbScannerPreviewView CreatePlatformView() => new();
}
#endif
