using Xamarin.Forms;

namespace Bit.View.Controls
{
    public class BitFrame : Frame
    {
        public static BindableProperty BorderWidthProperty = BindableProperty.CreateAttached(nameof(BorderWidth), typeof(double), typeof(BitFrame), defaultValue: -1d);

        public double BorderWidth
        {
            get { return (double)GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }
    }
}
