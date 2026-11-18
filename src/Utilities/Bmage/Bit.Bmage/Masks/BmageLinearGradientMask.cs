using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// A graduated mask: fully covered up to <see cref="Start"/>, fading linearly to uncovered at <see cref="End"/> and
/// beyond, across the whole width of the image. The graduated filter of photo editors, for skies and horizons.
/// </summary>
/// <param name="Start">Where the coverage starts to fade, in the mask's units.</param>
/// <param name="End">Where the coverage reaches nothing, in the mask's units.</param>
public sealed record BmageLinearGradientMask(Vector2 Start, Vector2 End) : BmageMask
{
    public override string Kind => "Linear gradient";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        var m = context.ToPixels;
        var start = Vector2.Transform(Start, m);
        var axis = Vector2.Transform(End, m) - start;
        var lengthSquared = axis.LengthSquared();
        if (!(lengthSquared > 1e-6f))
        {
            // No direction to fade along: everything is on the covered side.
            target.Fill(255);
            return;
        }
        var step = axis / lengthSquared;
        var window = context.Window;
        BmageParallelRows.For(context.Operation, window.Top, window.Bottom, window.Width, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 63) == 0) band.ThrowIfCancellationRequested();
                var row = target.GetRow(y - window.Top);
                var t0 = Vector2.Dot(new Vector2(0.5f, y + 0.5f) - start, step);
                for (var x = window.Left; x < window.Right; x++)
                {
                    var t = t0 + x * step.X;
                    row[x - window.Left] = t <= 0 ? (byte)255 : t >= 1 ? (byte)0 : BmageRgba32.ClampToByte((1 - t) * 255f);
                }
            }
        });
    }

    protected override BmageMask ScaleGeometry(double scale) => this with { Start = Start * (float)scale, End = End * (float)scale };

    protected override BmageMask TransformGeometry(Matrix3x2 matrix) => this with { Start = Vector2.Transform(Start, matrix), End = Vector2.Transform(End, matrix) };
}
