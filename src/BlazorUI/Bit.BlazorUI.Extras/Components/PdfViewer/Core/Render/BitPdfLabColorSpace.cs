// Color-space handling: the device, ICC-by-component, indexed and separation cases.


namespace Bit.BlazorUI;

/// <summary>
/// A CIE 1976 L*a*b* color space. Converts L*a*b* (with the document's white
/// point) to sRGB via the XYZ intermediate (PDF 32000-1 §8.6.5.4).
/// </summary>
internal sealed class BitPdfLabColorSpace : BitPdfColorSpace
{
    private readonly double _xw, _yw, _zw;
    private readonly double _amin, _amax, _bmin, _bmax;

    private BitPdfLabColorSpace(double xw, double yw, double zw, double[] range)
    {
        _xw = xw; _yw = yw; _zw = zw;
        _amin = range[0]; _amax = range[1]; _bmin = range[2]; _bmax = range[3];
    }

    public override int Components => 3;

    // L* spans [0,100]; a*/b* span the space's Range. Used to scale Indexed
    // palette bytes into Lab component space.
    public override double[] ComponentRanges() => [0, 100, _amin, _amax, _bmin, _bmax];

    public static BitPdfLabColorSpace Build(List<object?> arr, IBitPdfXRef xref)
    {
        double xw = 1, yw = 1, zw = 1;
        double[] range = [-100, 100, -100, 100];
        if (xref.FetchIfRef(arr.Count > 1 ? arr[1] : null) is BitPdfDict dict)
        {
            if (dict.Get("WhitePoint") is List<object?> wp && wp.Count >= 3)
            {
                xw = Num(wp[0]); yw = Num(wp[1]); zw = Num(wp[2]);
            }
            if (dict.Get("Range") is List<object?> r && r.Count >= 4)
            {
                range = [Num(r[0]), Num(r[1]), Num(r[2]), Num(r[3])];
            }
        }
        return new BitPdfLabColorSpace(xw, yw, zw, range);
    }

    public override (byte, byte, byte) GetRgb(double[] c)
    {
        double ls = Math.Clamp(c.Length > 0 ? c[0] : 0, 0, 100);
        double as_ = Math.Clamp(c.Length > 1 ? c[1] : 0, _amin, _amax);
        double bs = Math.Clamp(c.Length > 2 ? c[2] : 0, _bmin, _bmax);

        double m = (ls + 16) / 116;
        double l = m + as_ / 500;
        double n = m - bs / 200;

        double x = _xw * Decode(l);
        double y = _yw * Decode(m);
        double z = _zw * Decode(n);

        // XYZ (D50-ish, per the white point) to linear sRGB.
        double r = 3.1339 * x - 1.6169 * y - 0.4906 * z;
        double g = -0.9785 * x + 1.9160 * y + 0.0333 * z;
        double b = 0.0720 * x - 0.2290 * y + 1.4057 * z;

        return (ToByte(Gamma(r)), ToByte(Gamma(g)), ToByte(Gamma(b)));
    }

    private static double Decode(double t)
        => t >= 6.0 / 29.0 ? t * t * t : 3 * (6.0 / 29.0) * (6.0 / 29.0) * (t - 4.0 / 29.0);

    private static double Gamma(double v)
    {
        v = Math.Clamp(v, 0, 1);
        return v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - 0.055;
    }

    private static double Num(object? o) => o is double d ? d : 0;
}
