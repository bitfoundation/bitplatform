namespace Bit.Bmage;

/// <summary>Reduces each channel to a fixed number of levels (2..256).</summary>
public sealed class BmagePosterizeFilter : BmageCurveOperation
{
    public BmagePosterizeFilter(int levels) : base(Table(levels), Table(levels), Table(levels), null, "Posterize") => Levels = levels;
    public int Levels { get; }

    private static float[] Table(int levels)
    {
        if (levels is < 2 or > 256) throw new ArgumentOutOfRangeException(nameof(levels), "Levels must be between 2 and 256.");
        var steps = levels - 1;
        return BuildTable(v => MathF.Round(v * steps) / steps);
    }
}
