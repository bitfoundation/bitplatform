using System;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Globalization;

namespace Bit.Client.Web.BlazorUI
{
    internal class BitColor
    {
        private string? hex;
        private string? rgb;
        private string? rgba;
        private (int Hue, int Saturation, int Value) hsv;

        public string? Hex => hex;
        public string? Rgb => rgb;
        public string? Rgba => rgba;
        public (int Hue, int Saturation, int Value) Hsv => hsv;

        public int Red { get; private set; } = 255;
        public int Green { get; private set; } = 255;
        public int Blue { get; private set; } = 255;
        public double Alpha { get; set; } = 1;

        public BitColor(string color = "", double alpha = 1)
        {
            Parse(color, alpha);
            CalculateHsv();
            CalculateHex();
            CalculateRgbCss();
            CalculateRgbaCss();
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

            hsv = new(
                Convert.ToInt32(Math.Floor(hue)),
                Convert.ToInt32(Math.Floor(saturation * 100)),
                Convert.ToInt32(Math.Floor(value * 100))
                );

            CalculateHex();
            CalculateRgbCss();
            CalculateRgbaCss();
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
                CalculateRgbCss();
            }

            CalculateRgbaCss();
        }

        private void Parse(string color, double alpha = 1)
        {
            Alpha = alpha;

            try
            {
                if (color.StartsWith("#", StringComparison.InvariantCultureIgnoreCase))
                {
                    Red = int.Parse(color.AsSpan(1, 2), NumberStyles.AllowHexSpecifier);
                    Green = int.Parse(color.AsSpan(3, 2), NumberStyles.AllowHexSpecifier);
                    Blue = int.Parse(color.AsSpan(5, 2), NumberStyles.AllowHexSpecifier);
                }
                else if (color.Contains("rgb", StringComparison.InvariantCultureIgnoreCase))
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
                    }
                }
                else
                {
                    SetDefaultColor();
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
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

        private void CalculateHsv()
        {
            var myColor = Color.FromArgb(Red, Green, Blue);

            int max = Math.Max(myColor.R, Math.Max(myColor.G, myColor.B));
            int min = Math.Min(myColor.R, Math.Min(myColor.G, myColor.B));

            var hue = myColor.GetHue();
            var saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            var value = max / 255d;

            hsv = new(
                Convert.ToInt32(Math.Floor(hue)),
                Convert.ToInt32(Math.Floor(saturation * 100)),
                Convert.ToInt32(Math.Floor(value * 100))
                );
        }

        private void CalculateHex()
        {
            var myColor = Color.FromArgb(Red, Green, Blue);
            hex = $"#{myColor.Name.Remove(0, 2)}";
        }

        private void CalculateRgbCss()
        {
            rgb = $"rgb({Red},{Green},{Blue})";
        }

        private void CalculateRgbaCss()
        {
            rgba = $"rgba({Red},{Green},{Blue},{Alpha})";
        }

        public override bool Equals(object? obj)
        {
            return obj is BitColor color && this == color;
        }
        public override int GetHashCode()
        {
            return Red.GetHashCode() ^ Green.GetHashCode() ^ Blue.GetHashCode() ^ Alpha.GetHashCode();
        }
        public static bool operator ==(BitColor x, BitColor y)
        {
            return x.Red == y.Red && x.Green == y.Green && x.Blue == y.Blue && x.Alpha == y.Alpha;
        }
        public static bool operator !=(BitColor x, BitColor y)
        {
            return !(x == y);
        }
    }
}
