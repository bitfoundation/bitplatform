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
        private static Rgb defaultColor => new Rgb() { Red = 255, Green = 255, Blue = 255 };

        private Rgb rgb = defaultColor;

        public Rgb Rgb { get => rgb; }
        public Hsv Hsv { get => rgb.ToHsv(); }
        public Hex Hex { get => rgb.ToHex(); }

        public double Alpha { get; set; }

        public BitColor(string color = "", double alpha = 1)
        {
            try
            {
                if (color.StartsWith("#"))
                {
                    rgb = new Hex() { ColorCode = color }.ToRGB();
                    Alpha = alpha;
                }
                else if (color.ToLower().Contains("rgb"))
                {
                    Regex rx = new Regex(@"\(([^)]+)\)");
                    var mathedColor = rx.Match(color).Value;

                    mathedColor = mathedColor.Trim('(');
                    mathedColor = mathedColor.Trim(')');

                    var colorString = mathedColor.Split(",");
                    if (colorString.Length >= 3)
                    {
                        rgb.Red = int.Parse(colorString[0]);
                        rgb.Green = int.Parse(colorString[1]);
                        rgb.Blue = int.Parse(colorString[2]);
                        Alpha = alpha;


                        if (colorString.Length == 4)
                        {
                            Alpha = Convert.ToDouble(colorString[3]);
                        }
                    }
                }
                else
                {
                    rgb = defaultColor;
                    Alpha = alpha;
                }
            }

            catch (Exception exp)
            {
                rgb = defaultColor;
                Alpha = alpha;
            }
        }

        public string ToRgbaCss()
        {
            return $"rgba({rgb.Red},{rgb.Green},{rgb.Blue},{Alpha})";
        }

        public void SetColorByRgb(Rgb color)
        {
            rgb = color;
        }
    }
}
