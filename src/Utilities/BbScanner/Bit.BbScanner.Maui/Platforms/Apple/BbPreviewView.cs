#if IOS || MACCATALYST
using AVFoundation;
using UIKit;

namespace Bit.BbScanner;

/// <summary>The view an AVFoundation capture session's preview layer draws into, covering it as the page's video would.</summary>
internal sealed class BbPreviewView : UIView
{
    public BbPreviewView()
    {
        BackgroundColor = UIColor.Black;
        ClipsToBounds = true;
    }

    /// <summary>The layer the open camera draws into, or <see langword="null"/>.</summary>
    public AVCaptureVideoPreviewLayer? PreviewLayer { get; private set; }

    /// <summary>Draws <paramref name="session"/>'s preview; a front camera's is mirrored, as a mirror would be.</summary>
    public void Attach(AVCaptureSession session)
    {
        Remove();
        PreviewLayer = new AVCaptureVideoPreviewLayer(session)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill,
            Frame = Bounds,
        };
        Layer.AddSublayer(PreviewLayer);
    }

    /// <summary>Stops drawing <paramref name="session"/>'s preview, unless another session's is drawn by now.</summary>
    public void Detach(AVCaptureSession session)
    {
        if (PreviewLayer?.Session == session)
        {
            Remove();
        }
    }

    private void Remove()
    {
        PreviewLayer?.RemoveFromSuperLayer();
        PreviewLayer?.Dispose();
        PreviewLayer = null;
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        if (PreviewLayer is { } layer)
        {
            layer.Frame = Bounds;
        }
    }
}
#endif
