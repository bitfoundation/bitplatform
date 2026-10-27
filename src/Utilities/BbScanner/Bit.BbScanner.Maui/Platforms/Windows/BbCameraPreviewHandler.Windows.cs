using Microsoft.UI.Xaml.Controls;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Stretch = Microsoft.UI.Xaml.Media.Stretch;

namespace Bit.BbScanner;

internal sealed partial class BbCameraPreviewHandler
{
    protected override MediaPlayerElement CreatePlatformView() => new()
    {
        Stretch = Stretch.UniformToFill,
        AreTransportControlsEnabled = false,
        Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
    };
}
