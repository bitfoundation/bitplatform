using Microsoft.JSInterop;

namespace Bit.BbScanner;

/// <summary>Receives, from the page, where the scanner's video element is, for a native camera's preview.</summary>
internal sealed class BbScannerNativePreviewReceiver(Action<BbScannerPreviewPlacement> place)
{
    /// <summary>Called by the page whenever the element moves, resizes, shows or hides.</summary>
    [JSInvokable]
    public void Place(double x, double y, double width, double height, double cornerRadius, bool visible, string? backdrop,
        double clipX, double clipY, double clipWidth, double clipHeight)
        => place(new BbScannerPreviewPlacement(x, y, width, height, cornerRadius, visible, backdrop, clipX, clipY, clipWidth, clipHeight));
}
