
namespace Bit.Bink;

/// <summary>Controls which parts of a document are drawn.</summary>
public sealed record BinkRenderOptions
{
    /// <summary>Draw everything on visible layers.</summary>
    public static readonly BinkRenderOptions Default = new();

    /// <summary>Only draw elements whose bounds intersect this rectangle. Empty means no culling.</summary>
    public BinkRect ClipBounds { get; init; } = BinkRect.Empty;

    /// <summary>Whether hidden layers are drawn.</summary>
    public bool IncludeHiddenLayers { get; init; }

    /// <summary>When set, only these elements are drawn (eraser strokes are always kept so erasing still applies).</summary>
    public IReadOnlyCollection<Guid>? ElementFilter { get; init; }

    /// <summary>When set, this element is skipped. Used to hide an element that a tool is drawing on the overlay instead.</summary>
    public Guid? SuppressedElement { get; init; }
}

/// <summary>
/// Draws a document into any <see cref="IBinkRenderContext"/>. Canvas rendering, SVG export and PNG export all go
/// through this one path, which is what keeps the screen and the exported file identical.
/// </summary>
public static class BinkDocumentRenderer
{
    /// <summary>Draws the document's layers and their elements.</summary>
    /// <param name="document">The document.</param>
    /// <param name="context">Where to draw.</param>
    /// <param name="options">What to include.</param>
    /// <returns>The number of elements drawn.</returns>
    public static int Render(BinkDocument document, IBinkRenderContext context, BinkRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);
        options ??= BinkRenderOptions.Default;
        options = WithIndexedFilter(options);
        int drawn = 0;
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible && !options.IncludeHiddenLayers) continue;
            drawn += RenderLayer(layer, context, options);
        }
        return drawn;
    }

    /// <summary>Draws a single layer, isolating it so eraser strokes cannot erase content on the layers below.</summary>
    public static int RenderLayer(BinkLayer layer, IBinkRenderContext context, BinkRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(context);
        options = WithIndexedFilter(options ?? BinkRenderOptions.Default);
        if (layer.Count == 0) return 0;

        bool isolate = layer.Opacity < 1f || LayerHasEraser(layer);
        if (isolate) context.PushGroup(layer.Opacity, BinkBlendMode.Normal);

        int drawn = 0;
        BinkRect clip = options.ClipBounds;
        bool cull = !clip.IsEmpty;
        IReadOnlyCollection<Guid>? filter = options.ElementFilter;

        foreach (BinkElement element in layer.Elements)
        {
            if (options.SuppressedElement == element.Id) continue;
            bool isEraser = element is BinkStroke { IsEraser: true };
            if (filter is not null && !isEraser && !filter.Contains(element.Id)) continue;
            if (cull && !element.Bounds.Intersects(clip)) continue;
            element.Draw(context);
            drawn++;
        }

        if (isolate) context.PopGroup();
        return drawn;
    }

    /// <summary>
    /// Ensures the element filter can be probed in constant time. The filter arrives as a collection because
    /// that is what callers have (the selection hands out an array), and probing an array once per element turns
    /// a redraw into a quadratic scan: exporting a thousand selected elements out of ten thousand would compare
    /// ids ten million times.
    /// </summary>
    private static BinkRenderOptions WithIndexedFilter(BinkRenderOptions options)
    {
        IReadOnlyCollection<Guid>? filter = options.ElementFilter;
        // A handful of ids is faster to scan than to hash, and the common sets are already hash based.
        if (filter is null || filter.Count <= 8 || filter is HashSet<Guid> or IReadOnlySet<Guid>) return options;
        return options with { ElementFilter = new HashSet<Guid>(filter) };
    }

    private static bool LayerHasEraser(BinkLayer layer)
    {
        foreach (BinkElement element in layer.Elements)
        {
            if (element is BinkStroke { IsEraser: true }) return true;
        }
        return false;
    }

    /// <summary>Draws the document background over a region.</summary>
    public static void RenderBackground(IBinkRenderContext context, BinkColor color, BinkRect region)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (color.IsTransparent || region.IsEmpty) return;
        context.FillPath(new BinkPathBuilder(6).AddRect(region).ToGeometry(), new BinkFillPaint(color));
    }
}
