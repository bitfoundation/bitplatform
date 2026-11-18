using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Two masks combined value by value.</summary>
public sealed record BmageCombinedMask(BmageMask First, BmageMask Second, BmageMaskCombineMode Mode) : BmageMask
{
    public BmageMask First { get; init; } = First ?? throw new ArgumentNullException(nameof(First));
    public BmageMask Second { get; init; } = Second ?? throw new ArgumentNullException(nameof(Second));

    public override string Kind => "Combined";

    private protected override bool DrawsWindows => true;

    /// <summary>True when the combination draws exactly what combining its sides gives: no feather, inversion or opacity.</summary>
    private bool IsPlain => Feather < 0.05f && !Inverted && Opacity >= 1;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        // Each side is rendered whole, its own feather, inversion and opacity included, then the two are combined. The
        // first side is drawn straight into the target and the second on its own. A chain built a step at a time,
        // a.Union(b).Union(c), nests to the left: its plain links are walked rather than recursed into, so however long
        // it grows it holds one mask beside the target and none of the stack.
        var links = new Stack<BmageCombinedMask>();
        links.Push(this);
        var first = First;
        while (first is BmageCombinedMask { IsPlain: true } link)
        {
            links.Push(link);
            first = link.First;
        }
        first.RenderInto(target, context);
        while (links.TryPop(out var link))
        {
            context.Operation.ThrowIfCancellationRequested();
            using var second = link.Second.RenderWindow(context.Size, context.Window, context.Source, context.Operation);
            target.Combine(second, link.Mode);
        }
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        var a = First.GetAffectedBounds(size);
        var b = Second.GetAffectedBounds(size);
        switch (Mode)
        {
            case BmageMaskCombineMode.Intersect:
                a.Intersect(b);
                return a;
            case BmageMaskCombineMode.Subtract:
                return a;
            default:
                if (a.IsEmptyArea()) return b;
                if (b.IsEmptyArea()) return a;
                return RectangleF.Union(a, b);
        }
    }

    /// <summary>Scales both sides, each according to its own units, then the combination's own feather.</summary>
    public override BmageMask ForScale(double scale) => scale == 1 ? this : this with
    {
        First = First.ForScale(scale),
        Second = Second.ForScale(scale),
        Feather = (float)(Feather * scale),
    };

    /// <summary>Transforms both sides, each according to its own units, then the combination's own feather.</summary>
    public override BmageMask Transform(Matrix3x2 matrix) => matrix.IsIdentity ? this : this with
    {
        First = First.Transform(matrix),
        Second = Second.Transform(matrix),
        Feather = Feather * BmageAffineParts.Of(matrix).Scale,
    };
}
