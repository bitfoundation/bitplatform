using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public class BitSpinButtonChangeValue
    {
        public double Value { get; set; }
        public MouseEventArgs? MouseEventArgs { get; set; }
        public KeyboardEventArgs? KeyboardEventArgs { get; set; }
    }
}
