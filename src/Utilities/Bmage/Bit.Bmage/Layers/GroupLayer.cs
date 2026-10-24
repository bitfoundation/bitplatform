using System.Collections.Immutable;
using System.Numerics;
using Bit.Bmage.Drawing;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Layers;

/// <summary>
/// Layers kept together: shown, hidden, faded, masked, moved and reordered as one, the layer group of photo editors.
/// Its layers are listed bottom first, as a document's are, and may be groups themselves, up to <see cref="MaxDepth"/>
/// deep.
/// </summary>
/// <remarks>
/// <para>
/// By default a group passes through: its layers blend onto what is beneath the group exactly as they would without
/// it, an adjustment layer inside it editing everything beneath, and the group's <see cref="Layer.Opacity"/> and
/// <see cref="Layer.Mask"/> then fade the result back towards what was there before. Hiding or fading a group hides or
/// fades its layers together.
/// </para>
/// <para>
/// An <see cref="Isolated"/> group, or one with a <see cref="Layer.BlendMode"/> other than
/// <see cref="BlendMode.SourceOver"/>, first composites its layers on their own, over transparency, and then blends
/// that onto what is beneath with its blend mode, opacity and mask: an adjustment layer inside it edits only the
/// group's other layers. It costs a buffer the size of the image while it is drawn.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A caption and its backing plate, faded together and kept above the photo.
/// var caption = new GroupLayer([plate, text]) { Name = "Caption", Opacity = 0.9f };
/// document = document.AddLayer(caption);
///
/// // Or group layers already in the document, then edit the group as any layer.
/// document = document.GroupLayers([plate.Id, text.Id], out var groupId);
/// </code>
/// </example>
public sealed record GroupLayer : Layer
{
    /// <summary>How deep groups may nest. Each isolated level holds a buffer the size of the image while it is drawn.</summary>
    public const int MaxDepth = 10;

    /// <param name="layers">The group's layers, bottom first.</param>
    public GroupLayer(IEnumerable<Layer> layers) => Layers = [.. layers ?? throw new ArgumentNullException(nameof(layers))];

    /// <summary>The group's layers, bottom first.</summary>
    /// <exception cref="ArgumentException">A layer is null, or the groups would nest deeper than <see cref="MaxDepth"/>.</exception>
    public IReadOnlyList<Layer> Layers
    {
        get => LayerList;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var list = value as ImmutableList<Layer> ?? [.. value];
            var depth = 1;
            foreach (var layer in list)
            {
                if (layer is null) throw new ArgumentException("A group cannot hold a null layer.", nameof(Layers));
                if (layer is GroupLayer group) depth = Math.Max(depth, group.Depth + 1);
            }
            if (depth > MaxDepth) throw new ArgumentException($"Groups may nest at most {MaxDepth} deep.", nameof(Layers));
            LayerList = list;
            Depth = depth;
        }
    }

    internal ImmutableList<Layer> LayerList { get; private init; } = [];

    /// <summary>How many groups deep this one goes, itself included: 1 for a group of layers that are not groups.</summary>
    public int Depth { get; private init; } = 1;

    /// <summary>
    /// Whether the group composites its layers on their own before blending them onto what is beneath, rather than
    /// letting them blend through. A blend mode other than <see cref="BlendMode.SourceOver"/> isolates it too.
    /// </summary>
    public bool Isolated { get; init; }

    /// <summary>True when the group composites its layers on their own: <see cref="Isolated"/>, or blended other than normally.</summary>
    public bool IsIsolated => Isolated || BlendMode != BlendMode.SourceOver;

    public override string Kind => "Group";

    public override bool IsNoOp => base.IsNoOp || LayerList.All(layer => layer.IsNoOp);

    protected internal override void CompositeCore(ImageBuffer target, OperationContext context) => Composite(target, context, cache: null);

    /// <summary>Blends the group onto the target, its image layers taking their prepared images from <paramref name="cache"/>.</summary>
    internal void Composite(ImageBuffer target, OperationContext context, LayerImageCache? cache)
    {
        if (IsNoOp) return;
        var inner = context.WithProgress(null);
        if (IsIsolated)
        {
            using var own = ImageBuffer.Create(target.Width, target.Height, context.Allocator, clear: true, ImageLimits.Unrestricted);
            LayerCompositor.CompositeList(own, LayerList, inner, cache);
            BlendRows(target, target.Bounds, context, y => own.GetRow(y));
            return;
        }
        if (Opacity >= 1 && Mask is null)
        {
            LayerCompositor.CompositeList(target, LayerList, inner, cache);
            return;
        }
        // Passed through, then faded back towards what was beneath by the group's opacity and mask.
        using var before = target.Clone(context.Allocator);
        LayerCompositor.CompositeList(target, LayerList, inner, cache);
        BlendRows(before, before.Bounds, context, y => target.GetRow(y), mixForSourceOver: true);
        before.Pixels.CopyTo(target.Pixels);
    }

    protected override Layer ScaleCore(double scale) => this with { Layers = [.. LayerList.Select(layer => layer.ForScale(scale))] };

    protected override Layer TransformCore(Matrix3x2 matrix) => this with { Layers = [.. LayerList.Select(layer => layer.Transform(matrix))] };

    internal override Layer WithNewIds() => this with { Id = Guid.NewGuid(), Layers = [.. LayerList.Select(layer => layer.WithNewIds())] };
}
