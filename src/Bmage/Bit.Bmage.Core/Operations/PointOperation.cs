using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bit.Bmage.Operations;

/// <summary>
/// An operation that maps each pixel independently of its neighbours (colour adjustments, curves, matrices).
/// Point operations work on rows of <see cref="Vector4"/> pixels with channels in [0,1] (R,G,B,A, straight alpha),
/// which lets several of them run in a single pass over the image without intermediate quantisation.
/// </summary>
public abstract class PointOperation : ImageOperation
{
    /// <summary>Transforms a row of pixels in place. Values may temporarily leave [0,1]; they are clamped when written back.</summary>
    public abstract void ProcessRow(Span<Vector4> pixels);

    /// <summary>
    /// Attempts to combine this operation with the one that follows it into a single equivalent operation.
    /// Returns null when the pair cannot be merged algebraically (they may still be executed in one pass).
    /// </summary>
    public virtual PointOperation? TryFuse(PointOperation next) => null;

    /// <inheritdoc />
    public sealed override ImageBuffer Apply(ImageBuffer source, OperationContext context)
        => PointOperationExecutor.Run(source, context, this);
}

/// <summary>Executes one or more <see cref="PointOperation"/> stages in a single pass over an image.</summary>
internal static class PointOperationExecutor
{
    private const float Inv255 = 1f / 255f;

    public static ImageBuffer Run(ImageBuffer source, OperationContext context, PointOperation stage)
    {
        ReadOnlySpan<PointOperation> stages = [stage];
        return Run(source, context, stages);
    }

    public static ImageBuffer Run(ImageBuffer source, OperationContext context, ReadOnlySpan<PointOperation> stages)
    {
        ArgumentNullException.ThrowIfNull(source);
        var width = source.Width;
        var height = source.Height;
        var output = context.CanMutateSource ? source : context.Allocate(width, height);
        if (!ReferenceEquals(output, source)) output.Metadata = source.Metadata;

        var pool = ArrayPool<Vector4>.Shared;
        var rowArray = pool.Rent(width);
        try
        {
            var row = rowArray.AsSpan(0, width);
            for (var y = 0; y < height; y++)
            {
                if ((y & 15) == 0)
                {
                    context.ThrowIfCancellationRequested();
                    context.ReportProgress(y / (double)height);
                }
                Load(source.GetRow(y), row);
                foreach (var stage in stages) stage.ProcessRow(row);
                Store(row, output.GetRow(y));
            }
            context.ReportProgress(1);
            return output;
        }
        catch
        {
            if (!ReferenceEquals(output, source)) output.Dispose();
            throw;
        }
        finally
        {
            pool.Return(rowArray);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Load(ReadOnlySpan<Rgba32> src, Span<Vector4> dst)
    {
        for (var i = 0; i < src.Length; i++)
        {
            var p = src[i];
            dst[i] = new Vector4(p.R, p.G, p.B, p.A) * Inv255;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Store(ReadOnlySpan<Vector4> src, Span<Rgba32> dst)
    {
        var scale = new Vector4(255f);
        var half = new Vector4(0.5f);
        for (var i = 0; i < src.Length; i++)
        {
            var v = Vector4.Clamp(src[i], Vector4.Zero, Vector4.One) * scale + half;
            dst[i] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
        }
    }
}

/// <summary>A point operation that runs an ordered list of stages in one pass. Produced by the pipeline optimiser.</summary>
public sealed class FusedPointOperation : PointOperation
{
    public FusedPointOperation(IReadOnlyList<PointOperation> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);
        if (stages.Count == 0) throw new ArgumentException("At least one stage is required.", nameof(stages));
        Stages = stages;
    }

    /// <summary>The stages in execution order.</summary>
    public IReadOnlyList<PointOperation> Stages { get; }

    public override string Name => "Adjustments (" + string.Join(", ", Stages.Select(s => s.Name)) + ")";

    public override bool IsIdentity => Stages.All(s => s.IsIdentity);

    /// <summary>
    /// Runs every stage over the row. Values are clamped to [0,1] between stages so that fusing operations produces
    /// exactly the same pixels as running them separately (each stage would otherwise quantise and clamp on its own).
    /// </summary>
    public override void ProcessRow(Span<Vector4> pixels)
    {
        for (var i = 0; i < Stages.Count; i++)
        {
            Stages[i].ProcessRow(pixels);
            if (i + 1 < Stages.Count)
                for (var p = 0; p < pixels.Length; p++) pixels[p] = Vector4.Clamp(pixels[p], Vector4.Zero, Vector4.One);
        }
    }

    public override IImageOperation ForScale(double scale)
    {
        var scaled = new PointOperation[Stages.Count];
        for (var i = 0; i < scaled.Length; i++) scaled[i] = (PointOperation)Stages[i].ForScale(scale);
        return new FusedPointOperation(scaled);
    }
}
