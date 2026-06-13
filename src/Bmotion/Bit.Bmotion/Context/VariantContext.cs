using Bit.Bmotion.Models;

namespace Bit.Bmotion.Context;

/// <summary>
/// Cascaded by a parent Motion component to propagate the active variant name,
/// shared variants dictionary, and stagger configuration to descendant Motion components.
/// </summary>
public class VariantContext
{
    private int _nextChildIndex;

    /// <summary>The currently active variant name selected by the nearest ancestor.</summary>
    public string? ActiveVariant { get; internal set; }

    /// <summary>The initial variant name provided by the nearest ancestor.</summary>
    public string? InitialVariant { get; internal set; }

    /// <summary>Shared variants dictionary from the nearest ancestor that defined variants.</summary>
    public MotionVariants? Variants { get; internal set; }

    /// <summary>Seconds to stagger each child's animation start.</summary>
    public double StaggerChildren { get; internal set; }

    /// <summary>Seconds to delay the first child's animation start.</summary>
    public double DelayChildren { get; internal set; }

    /// <summary>
    /// Called by a child Motion component once on first render to obtain a stable
    /// position in the stagger sequence. Returns the child's index.
    /// </summary>
    internal int RegisterChild() => _nextChildIndex++;

    /// <summary>Returns the stagger delay in seconds for a child at the given index.</summary>
    public double GetChildDelay(int childIndex) => DelayChildren + childIndex * StaggerChildren;
}
