using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public class BitColor
    {
        private RGB color;

        public BitColor(string rgb)
        {
            color = StringToRGBConverter(rgb);
        }

        public RGB Color { get => color; }

        public void SetColorByString(string rgb)
        {
            color = StringToRGBConverter(rgb);
        }

        public void SetColorByRGB(RGB rgb)
        {
            color = rgb;
        }

        private RGB StringToRGBConverter(string value)
        {
            RGB color = new RGB { Red = 0, Green = 0, Blue = 0 };
            try
            {
                Regex rx = new Regex(@"\(([^)]+)\)");
                var mathedColor = rx.Match(value).Value;

                mathedColor = mathedColor.Trim('(');
                mathedColor = mathedColor.Trim(')');

                var colorString = mathedColor.Split(",");
                if (colorString.Length >= 3)
                {
                    color.Red = int.Parse(colorString[0]);
                    color.Green = int.Parse(colorString[1]);
                    color.Blue = int.Parse(colorString[2]);

                    if (colorString.Length == 4)
                    {
                        color.Alpha = Convert.ToDouble(colorString[3]);
                    }
                }
            }
            catch (Exception exp) { }

            return color;
        }
    }
}
