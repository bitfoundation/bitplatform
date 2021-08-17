using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI.Components.SpinButtons
{
    public class BitSpinButtonEventArgs
    {
        public double Value { get; set; }
        public MouseEventArgs? MouseEventArgs { get; set; }
        public KeyboardEventArgs? KeyboardEventArgs { get; set; }
    }
}
