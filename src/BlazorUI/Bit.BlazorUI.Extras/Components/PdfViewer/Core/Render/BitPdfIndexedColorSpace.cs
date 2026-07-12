// Color-space handling: the device, ICC-by-component, indexed and separation cases.


namespace Bit.BlazorUI;

/// <summary>An Indexed (palette) color space.</summary>
internal sealed class BitPdfIndexedColorSpace : BitPdfColorSpace
{
    private readonly BitPdfColorSpace _base;
    private readonly byte[] _lookup;
    private readonly int _hival;

    private BitPdfIndexedColorSpace(BitPdfColorSpace baseCs, byte[] lookup, int hival)
    {
        _base = baseCs;
        _lookup = lookup;
        _hival = hival;
    }

    public override int Components => 1;

    public static BitPdfIndexedColorSpace Build(List<object?> arr, IBitPdfXRef xref, BitPdfDict? resources)
    {
        var baseCs = Create(arr.Count > 1 ? arr[1] : null, xref, resources);
        int hival = xref.FetchIfRef(arr.Count > 2 ? arr[2] : null) is double d ? (int)d : 0;
        byte[] lookup;
        object? lookupObj = xref.FetchIfRef(arr.Count > 3 ? arr[3] : null);
        if (lookupObj is BitPdfString s)
        {
            lookup = s.Bytes;
        }
        else if (lookupObj is BitPdfStream stream)
        {
            lookup = BitPdfStreamDecoder.Decode(stream);
        }
        else
        {
            lookup = [];
        }
        return new BitPdfIndexedColorSpace(baseCs, lookup, hival);
    }

    public override (byte, byte, byte) GetRgb(double[] comps)
    {
        int index = (int)Math.Round(comps.Length > 0 ? comps[0] : 0);
        index = Math.Clamp(index, 0, _hival);
        int n = _base.Components;
        double[] ranges = _base.ComponentRanges();
        var baseComps = new double[n];
        for (int i = 0; i < n; i++)
        {
            int pos = index * n + i;
            double raw = pos < _lookup.Length ? _lookup[pos] / 255.0 : 0;
            // Scale the 0..255 lookup byte into the base space's component range,
            // so an Indexed-over-Lab (or any non-0..1 base) resolves correctly.
            double lo = ranges[i * 2];
            double hi = ranges[i * 2 + 1];
            baseComps[i] = lo + raw * (hi - lo);
        }
        return _base.GetRgb(baseComps);
    }
}
