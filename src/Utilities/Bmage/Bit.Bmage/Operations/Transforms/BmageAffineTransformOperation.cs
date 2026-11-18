using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Applies an arbitrary affine transform (rotation, scale, skew, translation). The output canvas is either given explicitly
/// or computed to contain the transformed bounds of the input.
/// </summary>
public sealed class BmageAffineTransformOperation : BmageImageOperation, IBmageGeometricOperation
{
    public BmageAffineTransformOperation(Matrix3x2 matrix, Size? outputSize = null, BmageRgba32 background = default, BmageWarpSampling sampling = BmageWarpSampling.Bilinear, string? name = null)
    {
        if (!Matrix3x2.Invert(matrix, out _)) throw new ArgumentException("The matrix is not invertible.", nameof(matrix));
        Matrix = matrix;
        OutputSize = outputSize;
        Background = background;
        Sampling = sampling;
        _name = name ?? "Transform";
    }

    private readonly string _name;

    /// <summary>Maps input pixel coordinates to output coordinates. When <see cref="OutputSize"/> is null the output is translated so the transformed bounds start at the origin.</summary>
    public Matrix3x2 Matrix { get; }
    public Size? OutputSize { get; }
    public BmageRgba32 Background { get; }
    public BmageWarpSampling Sampling { get; }

    public override string Name => _name;

    public override bool IsIdentity => Matrix.IsIdentity && OutputSize is null;

    private (Matrix3x2 Forward, Size Size) Resolve(Size input)
    {
        if (OutputSize is { } s) return (Matrix, s);
        var bounds = TransformBounds(Matrix, input);
        var translate = Matrix3x2.CreateTranslation(-bounds.X, -bounds.Y);
        var w = Math.Max(1, (int)Math.Round(bounds.Width));
        var h = Math.Max(1, (int)Math.Round(bounds.Height));
        return (Matrix * translate, new Size(w, h));
    }

    internal static RectangleF TransformBounds(Matrix3x2 m, Size input)
    {
        Span<Vector2> pts = [Vector2.Transform(Vector2.Zero, m), Vector2.Transform(new Vector2(input.Width, 0), m), Vector2.Transform(new Vector2(0, input.Height), m), Vector2.Transform(new Vector2(input.Width, input.Height), m)];
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in pts)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        // Snap to avoid 1px overflow from floating point noise.
        minX = MathF.Round(minX, 3); minY = MathF.Round(minY, 3); maxX = MathF.Round(maxX, 3); maxY = MathF.Round(maxY, 3);
        return RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }

    public override Size GetOutputSize(Size inputSize) => Resolve(inputSize).Size;

    public Matrix3x2? GetTransform(Size inputSize) => Resolve(inputSize).Forward;

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var (forward, size) = Resolve(source.Size);
        Matrix3x2.Invert(forward, out var inverse);
        var output = context.Allocate(size.Width, size.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            BmageWarp.RunAffine(source, output, inverse, Sampling, Background, context);
            return output;
        }
        catch { output.Dispose(); throw; }
    }

    public override IBmageImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        var s = (float)scale;
        // Conjugate: scale down to original space, apply, scale back up.
        var m = Matrix3x2.CreateScale(1 / s) * Matrix * Matrix3x2.CreateScale(s);
        Size? os = OutputSize is { } o ? new Size(Math.Max(1, (int)Math.Round(o.Width * scale)), Math.Max(1, (int)Math.Round(o.Height * scale))) : null;
        return new BmageAffineTransformOperation(m, os, Background, Sampling, _name);
    }
}
