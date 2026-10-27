#if IOS || MACCATALYST
namespace Bit.BbScanner;

internal sealed partial class BbCameraPreviewHandler
{
    protected override BbPreviewView CreatePlatformView() => new();
}
#endif
