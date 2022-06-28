using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI
{
    public class BitSpinButtonChangeValue
    {
        public double Value { get; set; }
        public MouseEventArgs? MouseEventArgs { get; set; }
        public KeyboardEventArgs? KeyboardEventArgs { get; set; }
    }
}
