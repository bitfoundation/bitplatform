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
        // The same loop as a pipeline run of its own: upright photos are not oriented again, and a failing step is named.
        var result = ImagePipeline.Run(steps, source, context);
        return ReferenceEquals(result, source) && !context.CanMutateSource ? source.Clone(context.Allocator) : result;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new PipelineOperation(Pipeline.ForScale(scale), _name);

    public override string ToString() => $"{Name}[{Pipeline}]";
}
