using System;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Globalization;

namespace Bit.Client.Web.BlazorUI
{
    internal class BitColor
    {
        private string? hex;
        private (int Hue, int Saturation, int Value) hsv;

        public (int Hue, int Saturation, int Value) Hsv => hsv;
        public string? Hex => hex;

        public int Red { get; private set; } = 255;
        public int Green { get; private set; } = 255;
        public int Blue { get; private set; } = 255;
        public double Alpha { get; set; } = 1;

        public BitColor(string color = "", double alpha = 1)
        {
            Parse(color, alpha);
            CalculateHsv();
            CalculateHex();
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

            Red = Convert.ToInt32(Math.Floor((color.r + m) * 255));
            Green = Convert.ToInt32(Math.Floor((color.g + m) * 255));
            Blue = Convert.ToInt32(Math.Floor((color.b + m) * 255));
            Alpha = alpha;

            hsv = new(Convert.ToInt32(hue), Convert.ToInt32(saturation), Convert.ToInt32(value));

            CalculateHex();
        }

        public BitColor(int red, int green, int blue, double alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;

            CalculateHex();
            CalculateHsv();
        }

        private void CalculateHsv()
        {
            double hue;
            double saturation;

            var red = Red / 255;
            var green = Green / 255;
            var blue = Blue / 255;

            double cMax = Math.Max(red, Math.Max(green, blue));
            double cMin = Math.Min(red, Math.Min(green, blue));
            double span = cMax - cMin == 0 ? 1 : cMax - cMin;

            hue = red == cMax ? (60 * ((green - blue) / span) + 360) % 360
                : green == cMax ? (60 * ((blue - red) / span) + 120) % 360
                : (60 * ((red - green) / span) + 240) % 360;

            saturation = cMax == 0 ? 0 : span / cMax;

            hsv = new(
                Convert.ToInt32(Math.Floor(hue)),
                Convert.ToInt32(Math.Floor(saturation * 100)),
                Convert.ToInt32(Math.Floor(cMax * 100))
                );
        }

        private void CalculateHex()
        {
            var myColor = Color.FromArgb(Red, Green, Blue);
            hex = $"#{myColor.Name.Remove(0, 2)}";
        }

        public string ToRgbCss()
        {
            return $"rgb({Red},{Green},{Blue})";
        }

        public string ToRgbaCss()
        {
            return $"rgba({Red},{Green},{Blue},{Alpha})";
        }

        public void SetColorByRgba(int? red = null, int? green = null, int? blue = null, double? alpha = null)
        {
            if (red.HasValue)
            {
                Red = red.Value;
            }

            if (green.HasValue)
            {
                Green = green.Value;
            }

            if (blue.HasValue)
            {
                Blue = blue.Value;
            }

            if (alpha.HasValue)
            {
                Alpha = alpha.Value;
            }

            if (red.HasValue || green.HasValue || blue.HasValue)
            {
                CalculateHsv();
                CalculateHex();
            }
        }

        private void Parse(string color, double alpha = 1)
        {
            Alpha = alpha;

            try
            {
                if (color.StartsWith("#", StringComparison.CurrentCultureIgnoreCase))
                {
                    Red = int.Parse(color.AsSpan(1, 2), NumberStyles.AllowHexSpecifier);
                    Green = int.Parse(color.AsSpan(3, 2), NumberStyles.AllowHexSpecifier);
                    Blue = int.Parse(color.AsSpan(5, 2), NumberStyles.AllowHexSpecifier);
                }
                else if (color.Contains("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    Regex rx = new Regex(@"\(([^)]+)\)");
                    var mathedColor = rx.Match(color).Value;

                    mathedColor = mathedColor.Trim('(');
                    mathedColor = mathedColor.Trim(')');

                    var colorString = mathedColor.Split(",");
                    if (colorString.Length >= 3)
                    {
                        Red = int.Parse(colorString[0], CultureInfo.InvariantCulture);
                        Green = int.Parse(colorString[1], CultureInfo.InvariantCulture);
                        Blue = int.Parse(colorString[2], CultureInfo.InvariantCulture);

                        if (colorString.Length == 4)
                        {
                            Alpha = Convert.ToDouble(colorString[3], CultureInfo.InvariantCulture);
                        }
                    }
                }
                else
                {
                    SetDefaultColor();
                }
            }
            catch
            {
                SetDefaultColor();
            }
        }

        private void SetDefaultColor()
        {
            Red = 255;
            Green = 255;
            Blue = 255;
        }
    }
}
