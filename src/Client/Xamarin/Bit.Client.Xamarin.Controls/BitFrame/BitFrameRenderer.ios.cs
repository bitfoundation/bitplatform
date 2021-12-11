#if iOS

using Bit.iOS;
using Bit.View.Controls;
using ObjCRuntime;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(BitFrame), typeof(BitFrameRenderer))]

namespace Bit.iOS
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
