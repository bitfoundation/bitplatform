using System.Numerics;

namespace Bit.Bmage;

/// <summary>Applies a <see cref="BmageColorMatrix"/> to every pixel. Consecutive matrix operations fuse into one.</summary>
public class BmageColorMatrixOperation : BmagePointOperation
{
    public BmageColorMatrixOperation(BmageColorMatrix matrix, string? name = null)
    {
        Matrix = matrix;
        _name = name ?? "Color matrix";
    }

    private readonly string _name;

    /// <summary>The matrix applied by this operation.</summary>
    public BmageColorMatrix Matrix { get; }

    public override string Name => _name;

    public override bool IsIdentity => Matrix.IsIdentity;

    internal override bool IsChannelSeparable => IsBuiltIn && Matrix.IsDiagonal;

    public override void ProcessRow(Span<Vector4> pixels)
    {
        var m = Matrix;
        if (!m.AffectsAlpha)
        {
            var r = m.R; var g = m.G; var b = m.B; var o = m.Offset;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                pixels[i] = new Vector4(Vector4.Dot(r, p) + o.X, Vector4.Dot(g, p) + o.Y, Vector4.Dot(b, p) + o.Z, p.W);
            }
        }
        else
        {
            for (var i = 0; i < pixels.Length; i++) pixels[i] = m.Transform(pixels[i]);
        }
    }

    /// <summary>
    /// Concatenates with a following matrix. Only done when this matrix cannot produce out-of-range values, because a
    /// separate stage would clamp its result to [0,1] before the next one runs (matching CSS filter chain semantics).
    /// </summary>
    public override BmagePointOperation? TryFuse(BmagePointOperation next)
        => next is BmageColorMatrixOperation cm && Matrix.PreservesRange
            ? new BmageColorMatrixOperation(BmageColorMatrix.Concat(Matrix, cm.Matrix), Name == cm.Name ? Name : "Color matrix")
            : null;
}
