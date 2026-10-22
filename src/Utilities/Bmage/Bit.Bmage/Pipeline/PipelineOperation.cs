using System.Drawing;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Pipeline;

/// <summary>
/// A whole pipeline run as one operation, so it can be nested: inside a masked edit, an adjustment layer, or anywhere a
/// list of operations should be treated as one step. The nested pipeline is optimised for the size it receives.
/// </summary>
public sealed class PipelineOperation : ImageOperation
{
    private readonly string? _name;

    public PipelineOperation(ImagePipeline pipeline, string? name = null)
    {
        Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _name = name;
    }

    /// <summary>The nested operations, in order.</summary>
    public ImagePipeline Pipeline { get; }

    public override string Name => _name ?? "Pipeline";

    public override bool IsIdentity => Pipeline.All(operation => operation.IsIdentity);

    public override Size GetOutputSize(Size inputSize) => Pipeline.GetOutputSize(inputSize);

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        var steps = Pipeline.Optimize(source.Size);
        if (steps.IsEmpty) return context.CanMutateSource ? source : source.Clone(context.Allocator);

        var current = source;
        try
        {
            for (var i = 0; i < steps.Count; i++)
            {
                context.ThrowIfCancellationRequested();
                // The source may be changed in place only when the caller allowed it; every buffer made here may.
                var mutable = ReferenceEquals(current, source) ? context.CanMutateSource : true;
                var progress = context.Progress is null ? null : new ScaledProgress(context.Progress, i / (double)steps.Count, 1.0 / steps.Count);
                var next = steps[i].Apply(current, new OperationContext(context.Allocator, context.Limits, context.CancellationToken, progress, mutable, context.MaxDegreeOfParallelism));
                // The caller releases the source when the result is another buffer; the steps in between are ours.
                if (!ReferenceEquals(next, current) && !ReferenceEquals(current, source)) current.Dispose();
                current = next;
            }
            context.ReportProgress(1);
            return ReferenceEquals(current, source) && !context.CanMutateSource ? source.Clone(context.Allocator) : current;
        }
        catch
        {
            if (!ReferenceEquals(current, source)) current.Dispose();
            throw;
        }
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new PipelineOperation(Pipeline.ForScale(scale), _name);

    public override string ToString() => $"{Name}[{Pipeline}]";
}
