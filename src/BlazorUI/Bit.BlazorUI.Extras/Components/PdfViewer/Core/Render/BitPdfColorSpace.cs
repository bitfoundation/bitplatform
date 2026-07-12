// Color-space handling: the device, ICC-by-component, indexed and separation cases.


namespace Bit.BlazorUI;

/// <summary>
/// Converts color component values into RGB. Supports the device spaces,
/// ICCBased/Cal spaces (approximated by component count), Indexed palettes and
/// Separation/DeviceN tint transforms.
/// </summary>
public abstract class BitPdfColorSpace
{
    /// <summary>Number of color components consumed per sample.</summary>
    public abstract int Components { get; }

    /// <summary>Converts normalized (0..1) components to an 8-bit RGB triple.</summary>
    public abstract (byte R, byte G, byte B) GetRgb(double[] comps);

    /// <summary>
    /// The initial color components for this space when it is selected with
    /// <c>cs</c>/<c>CS</c> (PDF 32000-1 §8.6.3): zero for device/CIE spaces,
    /// one for Separation/DeviceN tints.
    /// </summary>
    public virtual double[] DefaultComponents()
    {
        var comps = new double[Components];
        return comps;
    }

    /// <summary>Builds a color space from a PDF color-space object.</summary>
    public static BitPdfColorSpace Create(object? obj, IBitPdfXRef xref, BitPdfDict? resources)
    {
        obj = xref.FetchIfRef(obj);

        if (obj is BitPdfName name)
        {
            switch (name.Value)
            {
                case "DeviceGray":
                case "G":
                case "CalGray":
                    return ResolveDefault(resources, "DefaultGray", xref) ?? Gray;
                case "DeviceRGB":
                case "RGB":
                case "CalRGB":
                    return ResolveDefault(resources, "DefaultRGB", xref) ?? Rgb;
                case "DeviceCMYK":
                case "CMYK":
                    return ResolveDefault(resources, "DefaultCMYK", xref) ?? Cmyk;
                case "Pattern":
                    return Rgb;
            }
            // A named space defined in the resource dictionary.
            if (resources?.Get("ColorSpace") is BitPdfDict csDict && csDict.Has(name.Value))
            {
                return Create(csDict.Get(name.Value), xref, resources);
            }
            return Gray;
        }

        if (obj is List<object?> arr && arr.Count > 0)
        {
            string kind = (xref.FetchIfRef(arr[0]) as BitPdfName)?.Value ?? "";
            switch (kind)
            {
                case "ICCBased":
                    if (xref.FetchIfRef(arr[1]) is BitPdfStream icc && icc.Dict is not null)
                    {
                        int n = icc.Dict.Get("N") is double dn ? (int)dn : 3;
                        return n switch { 1 => Gray, 4 => Cmyk, _ => Rgb };
                    }
                    return Rgb;
                case "CalRGB":
                    return Rgb;
                case "Lab":
                    return BitPdfLabColorSpace.Build(arr, xref);
                case "CalGray":
                    return Gray;
                case "Indexed":
                case "I":
                    return BitPdfIndexedColorSpace.Build(arr, xref, resources);
                case "Separation":
                case "DeviceN":
                    return BitPdfSeparationColorSpace.Build(arr, xref, resources);
                case "Pattern":
                    return arr.Count > 1 ? Create(arr[1], xref, resources) : Rgb;
            }
        }

        return Gray;
    }

    public static readonly BitPdfColorSpace Gray = new DeviceGray();
    public static readonly BitPdfColorSpace Rgb = new DeviceRgb();
    public static readonly BitPdfColorSpace Cmyk = new DeviceCmyk();

    // Device names in a resource may be redirected to a calibrated default via
    // the resource's ColorSpace dict (/DefaultRGB etc.). Resolve that, but guard
    // against a default that just names the same device space (infinite loop).
    private static BitPdfColorSpace? ResolveDefault(BitPdfDict? resources, string key, IBitPdfXRef xref)
    {
        if (resources?.Get("ColorSpace") is not BitPdfDict csDict || !csDict.Has(key))
        {
            return null;
        }
        if (xref.FetchIfRef(csDict.Get(key)) is BitPdfName dn &&
            dn.Value is "DeviceRGB" or "DeviceGray" or "DeviceCMYK" or "RGB" or "G" or "CMYK")
        {
            return null;
        }
        return Create(csDict.Get(key), xref, resources);
    }

    /// <summary>
    /// The per-component value ranges for this space (used, e.g., to scale an
    /// Indexed palette's lookup bytes into the base space). Defaults to [0,1] per
    /// component; CIE spaces with wider ranges (Lab) override this.
    /// </summary>
    public virtual double[] ComponentRanges()
    {
        var r = new double[Components * 2];
        for (int i = 0; i < Components; i++)
        {
            r[i * 2] = 0;
            r[i * 2 + 1] = 1;
        }
        return r;
    }

    /// <summary>
    /// Converts a CMYK quadruple (each 0..1) to sRGB using the polynomial fit
    /// from pdf.js (<c>DeviceCmykCS</c>), which is far closer to a real CMYK
    /// profile than the naive <c>(1-c)(1-k)</c> multiply. Single source of truth
    /// shared by the image, fill and stroke paths.
    /// </summary>
    public static (byte R, byte G, byte B) CmykToRgb(double c, double m, double y, double k)
    {
        c = Math.Clamp(c, 0, 1);
        m = Math.Clamp(m, 0, 1);
        y = Math.Clamp(y, 0, 1);
        k = Math.Clamp(k, 0, 1);

        double r = 255 +
            c * (-4.387332384609988 * c + 54.48615194189176 * m + 18.82290502165302 * y + 212.25662451639585 * k - 285.2331026137004) +
            m * (1.7149763477362134 * m - 5.6096736904047315 * y - 17.873870861415444 * k - 5.497006427196366) +
            y * (-2.5217340131683033 * y - 21.248923337353073 * k + 17.5119270841813) +
            k * (-21.86122147463605 * k - 189.48180835922747);
        double g = 255 +
            c * (8.841041422036149 * c + 60.118027045597366 * m + 6.871425592049007 * y + 31.159100130055922 * k - 79.2970844816548) +
            m * (-15.310361306967817 * m + 17.575251261109482 * y + 131.35250912493976 * k - 190.9453302588951) +
            y * (4.444339102852739 * y + 9.8632861493405 * k - 24.86741582555878) +
            k * (-20.737325471181034 * k - 187.80453709719578);
        double b = 255 +
            c * (0.8842522430003296 * c + 8.078677503112928 * m + 30.89978309703729 * y - 0.23883238689178934 * k - 14.183576799673286) +
            m * (10.49593273432072 * m + 63.02378494754052 * y + 50.606957656360734 * k - 112.23884253719248) +
            y * (0.03296041114873217 * y + 115.60384449646641 * k - 193.58209356861505) +
            k * (-22.33816807309886 * k - 180.12613974708367);

        return (Clamp255(r), Clamp255(g), Clamp255(b));
    }

    private static byte Clamp255(double v) => (byte)Math.Clamp((int)Math.Round(v), 0, 255);

    protected static byte ToByte(double v) => (byte)Math.Clamp((int)Math.Round(v * 255), 0, 255);

    private sealed class DeviceGray : BitPdfColorSpace
    {
        public override int Components => 1;
        public override (byte, byte, byte) GetRgb(double[] c)
        {
            byte g = ToByte(c.Length > 0 ? c[0] : 0);
            return (g, g, g);
        }
    }

    private sealed class DeviceRgb : BitPdfColorSpace
    {
        public override int Components => 3;
        public override (byte, byte, byte) GetRgb(double[] c)
            => (ToByte(c.Length > 0 ? c[0] : 0), ToByte(c.Length > 1 ? c[1] : 0), ToByte(c.Length > 2 ? c[2] : 0));
    }

    private sealed class DeviceCmyk : BitPdfColorSpace
    {
        public override int Components => 4;
        public override (byte, byte, byte) GetRgb(double[] c)
        {
            double cy = c.Length > 0 ? c[0] : 0;
            double m = c.Length > 1 ? c[1] : 0;
            double y = c.Length > 2 ? c[2] : 0;
            double k = c.Length > 3 ? c[3] : 0;
            return CmykToRgb(cy, m, y, k);
        }
    }
}
