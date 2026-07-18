// PDF function evaluation (Types 0, 2, 3, 4).


namespace Bit.BlazorUI;

/// <summary>A function that always returns a fixed output (fallback).</summary>
internal sealed class BitPdfConstantFunction : BitPdfFunction
{
    private readonly double[] _value;
    public BitPdfConstantFunction(double[] value) => _value = value;
    public override double[] Eval(double[] input) => _value;
}
