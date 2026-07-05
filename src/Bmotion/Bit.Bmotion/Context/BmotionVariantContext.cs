
namespace Bit.Bmotion;
/// <summary>
/// Cascaded by a parent Bmotion component to propagate the active variant name,
/// shared variants dictionary, and stagger configuration to descendant Bmotion components.
/// </summary>
internal class BmotionVariantContext
{
    private int _nextChildIndex;

    /// <summary>The currently active variant name selected by the nearest ancestor.</summary>
    public string? ActiveVariant { get; internal set; }

    /// <summary>The initial variant name provided by the nearest ancestor.</summary>
    public string? InitialVariant { get; internal set; }

    /// <summary>Shared variants dictionary from the nearest ancestor that defined variants.</summary>
    public BmVariants? Variants { get; internal set; }

    /// <summary>Seconds to stagger each child's animation start.</summary>
    public double StaggerChildren { get; internal set; }

    /// <summary>Seconds to delay the first child's animation start.</summary>
    public double DelayChildren { get; internal set; }

    /// <summary>
    /// Called by a child Bmotion component once on first render to obtain a stable
    /// position in the stagger sequence. Returns the child's index.
    /// </summary>
    internal int RegisterChild() => _nextChildIndex++;

    /// <summary>The next child index that would be handed out. Used to carry the counter across
    /// context instances so children registering after a variant change keep stable indices.</summary>
    internal int NextChildIndex => _nextChildIndex;

    /// <summary>Seeds the child-index counter (used when a fresh context instance replaces a prior one).</summary>
    internal void SeedChildIndex(int value) => _nextChildIndex = value;

    /// <summary>Returns the stagger delay in seconds for a child at the given index.</summary>
    public double GetChildDelay(int childIndex) => DelayChildren + childIndex * StaggerChildren;
}
