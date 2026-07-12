// PDF function evaluation (Types 0, 2, 3, 4).


namespace Bit.BlazorUI;

/// <summary>Type 2: exponential interpolation between C0 and C1.</summary>
internal sealed class BitPdfExponentialFunction : BitPdfFunction
{
    private readonly double[] _c0;
    private readonly double[] _c1;
    private readonly double _n;
    private readonly double[] _domain;

    private BitPdfExponentialFunction(double[] c0, double[] c1, double n, double[] domain)
    {
        _c0 = c0;
        _c1 = c1;
        _n = n;
        _domain = domain;
    }

    public static BitPdfExponentialFunction Build(BitPdfDict dict, IBitPdfXRef? xref = null)
    {
        double[] c0 = ReadNumbers(dict.Get("C0"), xref);
        double[] c1 = ReadNumbers(dict.Get("C1"), xref);
        if (c0.Length == 0) c0 = [0];
        if (c1.Length == 0) c1 = [1];
        double n = dict.Get("N") is double d ? d : 1;
        double[] domain = ReadNumbers(dict.Get("Domain"), xref);
        if (domain.Length < 2) domain = [0, 1];
        return new BitPdfExponentialFunction(c0, c1, n, domain);
    }

    public override double[] Eval(double[] input)
    {
        double x = Clamp(input.Length > 0 ? input[0] : 0, _domain[0], _domain[1]);
        double xn = _n == 1 ? x : Math.Pow(x, _n);
        int len = Math.Max(_c0.Length, _c1.Length);
        var output = new double[len];
        for (int i = 0; i < len; i++)
        {
            double a = i < _c0.Length ? _c0[i] : 0;
            double b = i < _c1.Length ? _c1[i] : 0;
            output[i] = a + xn * (b - a);
        }
        return output;
    }
}
