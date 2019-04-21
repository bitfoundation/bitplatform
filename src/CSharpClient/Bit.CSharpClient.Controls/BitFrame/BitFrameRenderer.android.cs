#if Android

using Android.Content;
using Android.Graphics.Drawables;
using Bit.View.Android;
using Bit.View.Controls;
using System;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(BitFrame), typeof(BitFrameRenderer))]

namespace Bit.View.Android
{
    public class BitFrameRenderer : Xamarin.Forms.Platform.Android.FastRenderers.FrameRenderer
    {
        public BitFrameRenderer(Context context)
            : base(context)
        {
        }

        public static void Init()
        {
            
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                double borderWidth = ((BitFrame)e.NewElement).BorderWidth;
                Color borderColor = e.NewElement.BorderColor;

                if (borderWidth != (double)BitFrame.BorderWidthProperty.DefaultValue && borderColor.IsDefault == false)
                {
                    GradientDrawable _backgroundDrawable = (GradientDrawable)typeof(Xamarin.Forms.Platform.Android.FastRenderers.FrameRenderer)
                        .GetField(nameof(_backgroundDrawable), BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(this);

                    int borderWidthInPixel = (int)Context.ToPixels(borderWidth);

                    _backgroundDrawable.SetStroke(borderWidthInPixel, borderColor.ToAndroid());
                }
            }
        }
    }
}

#endif