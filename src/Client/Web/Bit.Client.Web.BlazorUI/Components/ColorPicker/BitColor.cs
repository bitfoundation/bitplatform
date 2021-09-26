using System;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Globalization;

namespace Bit.Client.Web.BlazorUI
{
    internal class BitColor
    {
        private static Rgb defaultColor => new Rgb() { Red = 255, Green = 255, Blue = 255 };

        private Rgb rgb = defaultColor;

        public Rgb Rgb { get => rgb; }
        public Hsv Hsv { get => ToHsv(); }
        public Hex Hex { get => ToHex(); }

        public double Alpha { get; set; } = 1;

        public BitColor(string color = "", double alpha = 1)
        {
            Parse(color, alpha);
        }

        public BitColor(double hue, double saturation, double value, double alpha)
        {
            var c = value * saturation;
            var x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            var m = value - c;

            (double r, double g, double b) color =
                  hue >= 0 && hue < 60 ? (c, x, 0)
                : hue >= 60 && hue < 120 ? (x, c, 0)
                : hue >= 120 && hue < 180 ? (0, c, x)
                : hue >= 180 && hue < 240 ? (0, x, c)
                : hue >= 240 && hue < 300 ? (x, 0, c)
                : (c, 0, x);


            Math.Floor((color.r + m) * 255);
            Math.Floor((color.g + m) * 255);
            Math.Floor((color.b + m) * 255);

            rgb = new Rgb()
            {
                Red = Math.Floor((color.r + m) * 255),
                Green = Math.Floor((color.g + m) * 255),
                Blue = Math.Floor((color.b + m) * 255)
            };

            Alpha = alpha;
        }

        private Hsv ToHsv()
        {
            double hue;
            double saturation;

            var red = Rgb.Red / 255;
            var green = Rgb.Green / 255;
            var blue = Rgb.Blue / 255;

            double cMax = Math.Max(red, Math.Max(green, blue));
            double cMin = Math.Min(red, Math.Min(green, blue));
            double span = cMax - cMin == 0 ? 1 : cMax - cMin;

            hue = red == cMax ? (60 * ((green - blue) / span) + 360) % 360
                : green == cMax ? (60 * ((blue - red) / span) + 120) % 360
                : (60 * ((red - green) / span) + 240) % 360;

            saturation = cMax == 0 ? 0 : span / cMax;

            return new Hsv()
            {
                Hue = Math.Floor(hue),
                Saturation = Math.Floor(saturation * 100),
                Value = Math.Floor(cMax * 100)
            };
        }

        private Hex ToHex()
        {
            var myColor = Color.FromArgb(Convert.ToInt32(Rgb.Red), Convert.ToInt32(Rgb.Green), Convert.ToInt32(Rgb.Blue));
            return new Hex() { ColorCode = $"#{myColor.Name.Remove(0, 2)}" };
        }
        public string ToRgbCss()
        {
            return $"rgb({rgb.Red},{rgb.Green},{rgb.Blue})";
        }

        public string ToRgbaCss()
        {
            return $"rgba({rgb.Red},{rgb.Green},{rgb.Blue},{Alpha})";
        }

        public void SetColorByRgb(Rgb color)
        {
            rgb = color;
        }

        private void Parse(string color, double alpha = 1)
        {
            Alpha = alpha;

            try
            {
                if (color.StartsWith("#"))
                {
                    var red = int.Parse(color.Substring(1, 2), NumberStyles.AllowHexSpecifier);
                    var green = int.Parse(color.Substring(3, 2), NumberStyles.AllowHexSpecifier);
                    var blue = int.Parse(color.Substring(5, 2), NumberStyles.AllowHexSpecifier);

                    rgb = new Rgb()
                    {
                        Red = red,
                        Green = green,
                        Blue = blue
                    };

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


                        if (colorString.Length == 4)
                        {
                            Alpha = Convert.ToDouble(colorString[3]);
                        }
                    }
                }
                else
                {
                    rgb = defaultColor;
                }
            }

            catch (Exception exp)
            {
                rgb = defaultColor;
            }
        }
    }
}
