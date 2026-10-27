using AndroidX.Camera.View;

namespace Bit.BbScanner;

internal sealed partial class BbCameraPreviewHandler
{
    // A TextureView rather than a SurfaceView: it is composited with the views above it, the transparent WebView among
    // them, where a SurfaceView would be drawn in a window of its own.
    protected override PreviewView CreatePlatformView()
    {
        var view = new PreviewView(Context);
        view.SetImplementationMode(PreviewView.ImplementationMode.Compatible!);
        view.SetScaleType(PreviewView.ScaleType.FillCenter!);
        view.SetBackgroundColor(Android.Graphics.Color.Black);
        return view;
    }
}
