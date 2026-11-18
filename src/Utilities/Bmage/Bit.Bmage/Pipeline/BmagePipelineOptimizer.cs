using System.Drawing;

namespace Bit.Bmage;

/// <summary>
/// Rewrites a pipeline into an equivalent but cheaper one.
/// </summary>
/// <remarks>
/// <para>
/// <b>Correctness contract.</b> Every rule preserves the framing and the output size exactly. Rules differ in whether
/// they also preserve the exact pixel values:
/// </para>
/// <list type="bullet">
/// <item><b>Bit-identical.</b> Dropping identity operations, merging consecutive crops, merging consecutive
/// orientations, and fusing a resize with a following crop. The fused resize builds its filter kernels for the full
/// resize output and only writes the surviving region, so the pixels match exactly.</item>
/// <item><b>More accurate, not identical.</b> Fusing per-pixel operations removes the intermediate 8-bit quantisation
/// that separately executed adjustments perform between steps. Values are still clamped to [0,1] between fused stages,
/// matching CSS filter chain semantics, so clipping behaviour is unchanged; only rounding differs, typically by a
/// couple of units per channel over a long chain.</item>
/// <item><b>Higher quality, visibly different.</b> Dropping a resize whose result is entirely replaced by a following
/// resize removes an intermediate resample. The framing is unchanged but the surviving resample runs from the original
/// pixels instead of already-downscaled ones, so the result is sharper and can differ from the unoptimised output by
/// tens of units per channel on detailed images. This is the intended benefit of the rule; set
/// <see cref="BmagePipelineExecutionOptions.Optimize"/> to false when byte-for-byte reproduction of an unoptimised run
/// matters more than quality.</item>
/// </list>
/// </remarks>
/// <remarks>
/// Rules applied, repeated until no further change:
/// <list type="bullet">
/// <item>Drop operations that cannot change any pixel (<see cref="IBmageImageOperation.IsIdentity"/>).</item>
/// <item>Merge consecutive crops into one crop.</item>
/// <item>Merge consecutive lossless orientations (rotate 90 / flip) into one.</item>
/// <item>Drop a resize immediately followed by another resize when the first one's result is entirely replaced, i.e.
/// when both are unconditional scale-to-size operations. This removes the intermediate resample and the quality loss
/// it causes.</item>
/// <item>Fuse a resize followed by a crop into one resample that only computes the surviving region, skipping the work
/// for pixels that would be discarded.</item>
/// <item>Fuse runs of per-pixel operations into a single pass over the image.</item>
/// </list>
/// </remarks>
public static class BmagePipelineOptimizer
{
    /// <summary>Returns an optimised equivalent of <paramref name="pipeline"/>.</summary>
    public static BmageImagePipeline Optimize(BmageImagePipeline pipeline, Size? inputSize = null)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        if (pipeline.Count == 0) return pipeline;

        var ops = new List<IBmageImageOperation>(pipeline);
        var changed = true;
        var guard = 0;
        while (changed && guard++ < 16)
        {
            changed = false;
            changed |= RemoveIdentities(ops);
            changed |= MergeAdjacent(ops, inputSize);
            changed |= FuseResizeCrop(ops, inputSize);
        }
        FusePointOperations(ops);
        return BmageImagePipeline.From(ops);
    }

    /// <summary>
    /// True when no rule ever rewrites <paramref name="operation"/> together with a neighbour, so optimising the steps
    /// before it and the steps after it separately gives exactly what optimising the whole pipeline does. Identities are
    /// not barriers: they are dropped, and what was on either side becomes neighbours.
    /// </summary>
    /// <remarks>Keep in step with the rules below: the editor's preview cache splits pipelines at barriers.</remarks>
    internal static bool IsBarrier(IBmageImageOperation operation)
        => !operation.IsIdentity && operation is not (BmagePointOperation or BmageCropOperation or BmageOrientationOperation or BmageAutoOrientOperation or BmageResizeOperation);

    private static bool RemoveIdentities(List<IBmageImageOperation> ops)
    {
        var changed = false;
        for (var i = ops.Count - 1; i >= 0; i--)
        {
            if (ops[i].IsIdentity)
            {
                ops.RemoveAt(i);
                changed = true;
            }
        }
        return changed;
    }

    private static bool MergeAdjacent(List<IBmageImageOperation> ops, Size? inputSize)
    {
        var changed = false;
        for (var i = 0; i + 1 < ops.Count; i++)
        {
            var a = ops[i];
            var b = ops[i + 1];

            // Disjoint crops have no single-crop equivalent. Leaving them alone lets execution report the real error
            // instead of the optimiser inventing a region the user never asked for.
            if (a is BmageCropOperation ca && b is BmageCropOperation cb && ca.TryThen(cb) is { } merged)
            {
                ops[i] = merged;
                ops.RemoveAt(i + 1);
                changed = true;
                i--;
                continue;
            }
            if (a is BmageOrientationOperation oa && b is BmageOrientationOperation ob)
            {
                ops[i] = oa.Then(ob);
                ops.RemoveAt(i + 1);
                changed = true;
                i--;
                continue;
            }
            // resize → resize: the first result is entirely replaced by the second when both fully determine
            // their output from the input size (no padding, no anchoring that depends on content).
            if (a is BmageResizeOperation ra && b is BmageResizeOperation rb && CanDropFirstResize(ra, rb, SizeBefore(ops, i, inputSize)))
            {
                ops.RemoveAt(i);
                changed = true;
                i--;
                continue;
            }
        }
        return changed;
    }

    private static bool CanDropFirstResize(BmageResizeOperation first, BmageResizeOperation second, Size? inputSize)
    {
        // Dropping the first resize changes the second's input size, which changes its output for modes whose
        // output depends on the input aspect ratio. Only safe when the second produces a fixed canvas covering
        // the whole output regardless of what came before.
        if (second.Mode is not (BmageResizeMode.Stretch or BmageResizeMode.Cover)) return false;
        if (second.Width is null || second.Height is null) return false;
        // Cover crops based on the input aspect ratio; a preceding proportional resize does not change the ratio,
        // but Contain/Cover/Stretch in the first step can. Only drop when the first preserves aspect ratio.
        if (first.Mode is not (BmageResizeMode.Fit or BmageResizeMode.Max or BmageResizeMode.Min)) return false;
        if (second.Mode == BmageResizeMode.Stretch) return true;
        // Proportional modes round to whole pixels, so they only nearly keep the ratio, and Cover frames from the exact
        // ratio. Drop the first only when its output ratio is provably identical to its input's.
        if (inputSize is not { } size) return false;
        Size output;
        try { output = first.GetOutputSize(size); }
        catch (ArgumentException) { return false; }
        return (long)output.Width * size.Height == (long)output.Height * size.Width;
    }

    /// <summary>The size entering <c>ops[index]</c>, or null when it cannot be known ahead of execution.</summary>
    internal static Size? SizeBefore(IReadOnlyList<IBmageImageOperation> ops, int index, Size? inputSize)
    {
        if (inputSize is not { } current) return null;
        for (var i = 0; i < index; i++)
        {
            if (ops[i] is BmageAutoOrientOperation) return null;
            try { current = ops[i].GetOutputSize(current); }
            catch (ArgumentException) { return null; }
        }
        return current;
    }

    /// <summary>
    /// Fuses a resize immediately followed by a crop into a single resample that only computes the surviving region.
    /// The filter kernels are still built for the full resize output, so the pixels are identical.
    /// </summary>
    private static bool FuseResizeCrop(List<IBmageImageOperation> ops, Size? inputSize)
    {
        if (inputSize is not { } size) return false;
        var current = size;
        for (var i = 0; i < ops.Count; i++)
        {
            if (i + 1 < ops.Count && ops[i] is BmageResizeOperation resize && ops[i + 1] is BmageCropOperation crop)
            {
                var fused = BmageResizeCropOperation.TryCreate(resize, crop, current);
                if (fused is not null)
                {
                    ops[i] = fused;
                    ops.RemoveAt(i + 1);
                    return true;
                }
            }
            // AutoOrient's output size depends on metadata, not on the input size, so nothing after it can be planned.
            if (ops[i] is BmageAutoOrientOperation) return false;
            // Optimising is best effort. When an operation cannot be applied to the size it would receive (a crop
            // that falls outside the image, say) there is nothing to fuse past this point, and the error belongs to
            // execution, which reports which step failed. Analysis must not be the thing that throws.
            try { current = ops[i].GetOutputSize(current); }
            catch (ArgumentException) { return false; }
        }
        return false;
    }

    private static void FusePointOperations(List<IBmageImageOperation> ops)
    {
        for (var i = 0; i < ops.Count; i++)
        {
            if (ops[i] is not BmagePointOperation) continue;
            var end = i;
            while (end + 1 < ops.Count && ops[end + 1] is BmagePointOperation) end++;
            if (end == i) continue;
            var run = new List<BmagePointOperation>(end - i + 1);
            foreach (var op in ops.GetRange(i, end - i + 1)) run.Add((BmagePointOperation)op);

            // Algebraic fusion first (e.g. colour matrices multiply into one matrix).
            var merged = new List<BmagePointOperation>();
            foreach (var op in run)
            {
                if (merged.Count > 0 && merged[^1].TryFuse(op) is { } fused) merged[^1] = fused;
                else merged.Add(op);
            }
            ops.RemoveRange(i, end - i + 1);
            // Whatever remains still runs in a single pass over the pixels.
            ops.Insert(i, merged.Count == 1 ? merged[0] : new BmageFusedPointOperation(merged));
        }
    }
}
