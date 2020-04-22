#if UWP
using Bit.UWP;
using Bit.View.Controls;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(BitFrame), typeof(BitFrameRenderer))]

namespace Bit.UWP
{
    public class BitFrameRenderer : FrameRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            if (e == null)
                throw new ArgumentNullException(nameof(e));

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
