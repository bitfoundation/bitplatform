using System.Numerics;

namespace Bit.Bmage;

/// <summary>A point operation that runs an ordered list of stages in one pass. Produced by the pipeline optimiser.</summary>
public sealed class BmageFusedPointOperation : BmagePointOperation
{
    public BmageFusedPointOperation(IReadOnlyList<BmagePointOperation> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);
        if (stages.Count == 0) throw new ArgumentException("At least one stage is required.", nameof(stages));
        Stages = stages;
    }

    /// <summary>The stages in execution order.</summary>
    public IReadOnlyList<BmagePointOperation> Stages { get; }

    public override string Name => "Adjustments (" + string.Join(", ", Stages.Select(s => s.Name)) + ")";

    public override bool IsIdentity => Stages.All(s => s.IsIdentity);

    /// <summary>True when every stage may run on several rows at once.</summary>
    public override bool CanProcessRowsConcurrently => Stages.All(s => s.CanProcessRowsConcurrently);

    /// <summary>
    /// Runs every stage over the row. Values are clamped to [0,1] between stages so that fusing operations produces
    /// exactly the same pixels as running them separately (each stage would otherwise quantise and clamp on its own).
    /// </summary>
    public override void ProcessRow(Span<Vector4> pixels) => ProcessRowRange(0, Stages.Count, pixels);

    /// <summary>
    /// Runs stages <paramref name="start"/> to <paramref name="end"/> (exclusive), clamping after every stage that has
    /// another one after it, so running two consecutive ranges equals running the whole list. Without
    /// <paramref name="clampLast"/> the range's last stage is left unclamped, for a caller that clamps next anyway.
    /// </summary>
    internal void ProcessRowRange(int start, int end, Span<Vector4> pixels, bool clampLast = true)
    {
        for (var i = start; i < end; i++)
        {
            Stages[i].ProcessRow(pixels);
            if (i + 1 < Stages.Count && (clampLast || i + 1 < end))
                for (var p = 0; p < pixels.Length; p++) pixels[p] = Vector4.Clamp(pixels[p], Vector4.Zero, Vector4.One);
        }
    }

    internal override bool IsChannelSeparable => SeparablePrefixLength == Stages.Count;

    /// <summary>
    /// Where the run of built-in curves that ends the list starts, or the stage count when it ends with something else.
    /// </summary>
    internal int CurveSuffixStart
    {
        get
        {
            var start = Stages.Count;
            while (start > 0 && Stages[start - 1] is BmageCurveOperation curve && curve.IsChannelSeparable) start--;
            return start;
        }
    }

    /// <summary>How many leading stages map each channel from that channel alone.</summary>
    internal int SeparablePrefixLength
    {
        get
        {
            var count = 0;
            while (count < Stages.Count && Stages[count].IsChannelSeparable) count++;
            return count;
        }
    }

    public override IBmageImageOperation ForScale(double scale)
    {
        var scaled = new BmagePointOperation[Stages.Count];
        for (var i = 0; i < scaled.Length; i++) scaled[i] = (BmagePointOperation)Stages[i].ForScale(scale);
        return new BmageFusedPointOperation(scaled);
    }
}
