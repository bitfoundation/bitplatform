using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public class Hex
    {
        public string ColorCode { get; set; }

        public Rgb ToRGB()
        {
            return new Rgb()
            {
                Red = int.Parse(ColorCode.Substring(1, 2), NumberStyles.AllowHexSpecifier),
                Green = int.Parse(ColorCode.Substring(3, 2), NumberStyles.AllowHexSpecifier),
                Blue = int.Parse(ColorCode.Substring(5, 2), NumberStyles.AllowHexSpecifier)
            };
        }
    }
}
