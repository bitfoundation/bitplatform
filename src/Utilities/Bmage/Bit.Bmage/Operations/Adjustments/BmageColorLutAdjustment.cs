using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Maps every colour through a <see cref="BmageColorLookupTable"/>: a film look, a grade exported from a colour tool, a
/// brand's house style. <see cref="Strength"/> blends the result with the original.
/// </summary>
public sealed class BmageColorLutAdjustment : BmagePointOperation
{
    /// <param name="table">The table to apply.</param>
    /// <param name="strength">How much of the table's result to use, in [0,1]: 1 is the table as made, 0 the original.</param>
    public BmageColorLutAdjustment(BmageColorLookupTable table, float strength = 1f)
    {
        Table = table ?? throw new ArgumentNullException(nameof(table));
        if (!float.IsFinite(strength)) throw new ArgumentOutOfRangeException(nameof(strength), "The strength must be a finite number.");
        Strength = Math.Clamp(strength, 0f, 1f);
    }

    /// <summary>The table applied.</summary>
    public BmageColorLookupTable Table { get; }

    /// <summary>How much of the table's result is used, in [0,1].</summary>
    public float Strength { get; }

    public override string Name => Table.Title is { Length: > 0 } title ? $"LUT: {title}" : "LUT";

    public override bool IsIdentity => Strength == 0f;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var table = Table;
        var strength = Strength;
        for (var i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            var original = new Vector3(p.X, p.Y, p.Z);
            var mapped = table.Lookup(original);
            var c = strength >= 1f ? mapped : Vector3.Lerp(original, mapped, strength);
            pixels[i] = new Vector4(c, p.W);
        }
    }
}
