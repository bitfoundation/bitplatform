using System;

namespace Bit.BlazorUI;

internal class BitInternalColor
{
    private string? _hex;
    private string? _rgb;
    private string? _rgba;
    private (double Hue, double Saturation, double Value) _hsv;
    private double _alpha = 1.0;

    public string? Hex => _hex;
    public string? Rgb => _rgb;
    public string? Rgba => _rgba;
    public (double Hue, double Saturation, double Value) Hsv => _hsv;

    public byte R { get; private set; } = 255;
    public byte G { get; private set; } = 255;
    public byte B { get; private set; } = 255;

    public double A
    {
        get => _alpha;
        set
        {
            _alpha = value;
            GenerateStringValues();
        }
    }
    public BitInternalColor()
    {
        CalculateHsv();
        GenerateStringValues();
    }

    public BitInternalColor(string color = "", double alpha = 1.0)
    {
        Parse(color, alpha);
        CalculateHsv();
        GenerateStringValues();
    }

    public BitInternalColor(double hue, double saturation, double value, double alpha)
    {
        Update(hue, saturation, value, alpha);
    }

    public BitInternalColor(byte red = 255, byte green = 255, byte blue = 255, double alpha = 1.0)
    {
        R = red;
        G = green;
        B = blue;
        A = alpha;

        CalculateHsv();
        GenerateStringValues();
    }



    public void Update(double hue, double saturation, double value, double alpha)
    {
        A = alpha;
        (R, G, B) = ToRgb(hue, saturation, value);

        _hsv = new(hue, saturation, value);

        CalculateHsv();
        GenerateStringValues();
    }


    public static (byte R, byte G, byte B) ToRgb(double hue, double saturation, double value)
    {
        if (value > 1) value /= 100;
        if (saturation > 1) saturation /= 100;

        var c = value * saturation;
        var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = value - c;

        (double r, double g, double b) color =
              hue >= 0 && hue < 60 ? (c, x, 0)
            : hue >= 60 && hue < 120 ? (x, c, 0)
            : hue >= 120 && hue < 180 ? (0, c, x)
            : hue >= 180 && hue < 240 ? (0, x, c)
            : hue >= 240 && hue < 300 ? (x, 0, c)
            : (c, 0, x);

        var r = (byte)Math.Floor((color.r + m) * 255);
        var g = (byte)Math.Floor((color.g + m) * 255);
        var b = (byte)Math.Floor((color.b + m) * 255);

        return (r, g, b);
    }


    private void Parse(string color, double alpha = 1.0)
    {
        ResetColor();
        A = alpha;

        try
        {
            if (color.StartsWith('#'))
            {
                var value = Convert.ToInt32(color[1..], 16);

                R = (byte)((value >> 16) & 255);
                G = (byte)((value >> 8) & 255);
                B = (byte)(value & 255);
            }
            else if (color.StartsWith("rgb"))
            {
                var colorValues = color.Replace("rgba", "").Replace("rgb", "")
                                       .TrimStart('(').TrimEnd(')')
                                       .Split(",");

                if (colorValues.Length >= 3)
                {
                    R = byte.Parse(colorValues[0]);
                    G = byte.Parse(colorValues[1]);
                    B = byte.Parse(colorValues[2]);

                    if (colorValues.Length == 4)
                    {
                        A = double.Parse(colorValues[3]);
                    }
                }
            }
        }
        catch
        {
            ResetColor();
        }
    }

    private void ResetColor()
    {
        R = 255;
        G = 255;
        B = 255;
        A = 1;
    }

    private void CalculateHsv()
    {
        var r = R / 255.0;
        var g = G / 255.0;
        var b = B / 255.0;

        var maxC = Math.Max(r, Math.Max(g, b));
        var minC = Math.Min(r, Math.Min(g, b));
        var v = maxC;

        if (minC == maxC || maxC == 0)
        {
            _hsv = new(0.0, 0.0, v);
            return;
        }

        var delta = maxC - minC;
        var s = delta / maxC;
        double h;

        if (maxC == r)
        {
            h = (g - b) / delta;
        }
        else if (maxC == g)
        {
            h = (b - r) / delta + 2;
        }
        else // if (maxC == b)
        {
            h = (r - g) / delta + 4;
        }

        h *= 60;

        if (h < 0)
        {
            h += 360;
        }

        _hsv = (h, s, v);
    }

    private void GenerateStringValues()
    {
        _hex = FormattableString.Invariant($"#{R:X2}{G:X2}{B:X2}");
        _rgb = FormattableString.Invariant($"rgb({R},{G},{B})");
        _rgba = FormattableString.Invariant($"rgba({R},{G},{B},{A})");
    }
}
