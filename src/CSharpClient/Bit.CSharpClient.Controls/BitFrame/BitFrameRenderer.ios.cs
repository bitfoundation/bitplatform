#if iOS

using Bit.CSharpClient.Controls;
using Bit.CSharpClient.Controls.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(BitFrame), typeof(BitFrameRenderer))]

namespace Bit.CSharpClient.Controls.iOS
{
    public class BitFrameRenderer : FrameRenderer
    {
        public static void Init()
        {
            
        }
    }
}

#endif