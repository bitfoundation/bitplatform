
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
