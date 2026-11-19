using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// An edit of everything beneath it, through the layer's mask, opacity and blend mode: the adjustment layer of photo
/// editors. Unlike a step in the document's pipeline it applies to the layers below it too, and it can be reordered,
/// hidden or faded without touching anything else.
/// </summary>
/// <remarks>
/// With <see cref="BmageBlendMode.SourceOver"/> the edited pixels replace the ones below by the layer's coverage, so at full
/// opacity and without a mask the result is exactly the pipeline's. Other modes blend the edited pixels onto the
/// unedited ones: an empty pipeline in <see cref="BmageBlendMode.Multiply"/> darkens the image by itself.
/// </remarks>
public sealed record BmageAdjustmentLayer : BmageLayer
{
    /// <param name="pipeline">The edit. Every operation in it must keep the image's size.</param>
    public BmageAdjustmentLayer(BmageImagePipeline pipeline) => Pipeline = pipeline;

    /// <summary>The edit applied to what is below.</summary>
    public BmageImagePipeline Pipeline
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(Pipeline));
    }

    public override string Kind => "Adjustment";

    public override bool IsNoOp => base.IsNoOp || (BlendMode == BmageBlendMode.SourceOver && Pipeline.All(operation => operation.IsIdentity));

    protected internal override void CompositeCore(BmageImageBuffer target, BmageOperationContext context)
    {
        var size = Pipeline.GetOutputSize(target.Size);
        if (size != target.Size)
            throw new InvalidOperationException($"An adjustment layer must keep the image's size, but its pipeline turns {target.Width}x{target.Height} into {size.Width}x{size.Height}.");
        using var adjusted = Pipeline.Execute(target, new BmagePipelineExecutionOptions
        {
            Allocator = context.Allocator,
            Limits = context.Limits,
            CancellationToken = context.CancellationToken,
            // The edit is most of what the layer costs, so it reports the layer's progress; blending it back ends it.
            Progress = context.Progress,
            MaxDegreeOfParallelism = context.MaxDegreeOfParallelism,
        });
        // A custom operation may produce another size than it said it would.
        if (adjusted.Size != target.Size)
            throw new InvalidOperationException($"An adjustment layer must keep the image's size, but its pipeline produced {adjusted.Width}x{adjusted.Height} from {target.Width}x{target.Height}.");
        BlendRows(target, target.Bounds, context.WithProgress(null), y => adjusted.GetRow(y), mixForSourceOver: true);
    }

    protected override BmageLayer ScaleCore(double scale) => this with { Pipeline = Pipeline.ForScale(scale) };
}
