#if UWP
using Bit.View.Controls;
using Bit.View.UWP;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(BitFrame), typeof(BitFrameRenderer))]

namespace Bit.View.UWP
{
    public class BitFrameRenderer : FrameRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                double borderWidth = ((BitFrame)e.NewElement).BorderWidth;
                if (borderWidth != (double)BitFrame.BorderWidthProperty.DefaultValue)
                {
                    Control.BorderThickness = new Windows.UI.Xaml.Thickness(borderWidth);
                }
            }
        }

        public static void Init()
        {
            
        }
    }
}
#endif
