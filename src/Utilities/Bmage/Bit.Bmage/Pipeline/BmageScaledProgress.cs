namespace Bit.Bmage;

/// <summary>Scales a child progress range into a parent's [0,1].</summary>
internal sealed class BmageScaledProgress : IProgress<double>
{
    private readonly IProgress<double> _inner;
    private readonly double _offset, _scale;
    public BmageScaledProgress(IProgress<double> inner, double offset, double scale) { _inner = inner; _offset = offset; _scale = scale; }
    public void Report(double value) => _inner.Report(_offset + Math.Clamp(value, 0, 1) * _scale);
}
