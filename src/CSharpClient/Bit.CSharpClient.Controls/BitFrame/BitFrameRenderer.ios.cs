#if iOS

using Bit.View.Controls;
using Bit.View.iOS;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(BitFrame), typeof(BitFrameRenderer))]

namespace Bit.View.iOS
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
                    Layer.BorderWidth = (nfloat)borderWidth;
                }
            }
        }

        public new static void Init()
        {

        }
    }
}

#endif