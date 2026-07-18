// Color-space handling: the device, ICC-by-component, indexed and separation cases.


namespace Bit.BlazorUI;

/// <summary>A Separation / DeviceN color space using a tint-transform function.</summary>
internal sealed class BitPdfSeparationColorSpace : BitPdfColorSpace
{
    private readonly BitPdfColorSpace _alternate;
    private readonly BitPdfFunction? _tint;
    private readonly int _components;
    private readonly bool _isNone;

    private BitPdfSeparationColorSpace(BitPdfColorSpace alternate, BitPdfFunction? tint, int components, bool isNone)
    {
        _alternate = alternate;
        _tint = tint;
        _components = components;
        _isNone = isNone;
    }

    public override int Components => _components;

    public override double[] DefaultComponents()
    {
        // Separation/DeviceN initial color is full tint (1.0) on each component.
        var comps = new double[_components];
        Array.Fill(comps, 1.0);
        return comps;
    }

    public static BitPdfSeparationColorSpace Build(List<object?> arr, IBitPdfXRef xref, BitPdfDict? resources)
    {
        // [/Separation name alt tint]  or  [/DeviceN [names] alt tint]
        bool isDeviceN = (xref.FetchIfRef(arr[0]) as BitPdfName)?.Value == "DeviceN";
        int components = 1;
        // A Separation whose single colorant is /None produces no visible marks
        // (PDF 32000-1 §8.6.6.4); DeviceN counts as "None" only if every colorant is.
        bool isNone;
        if (isDeviceN && xref.FetchIfRef(arr.Count > 1 ? arr[1] : null) is List<object?> names)
        {
            components = names.Count;
            isNone = names.Count > 0 && names.All(n => (xref.FetchIfRef(n) as BitPdfName)?.Value == "None");
        }
        else
        {
            isNone = (xref.FetchIfRef(arr.Count > 1 ? arr[1] : null) as BitPdfName)?.Value == "None";
        }
        var alternate = Create(arr.Count > 2 ? arr[2] : null, xref, resources);
        var tint = BitPdfFunction.Create(arr.Count > 3 ? arr[3] : null, xref);
        return new BitPdfSeparationColorSpace(alternate, tint, components, isNone);
    }

    public override (byte, byte, byte) GetRgb(double[] comps)
    {
        // The /None colorant is a no-op: it never paints ink, so on the default
        // page it maps to white (the closest this HTML renderer can get to "no marks").
        if (_isNone)
        {
            return (255, 255, 255);
        }
        if (_tint is null)
        {
            byte v = ToByte(1 - (comps.Length > 0 ? comps[0] : 0));
            return (v, v, v);
        }
        double[] alt = _tint.Eval(comps);
        return _alternate.GetRgb(alt);
    }
}
