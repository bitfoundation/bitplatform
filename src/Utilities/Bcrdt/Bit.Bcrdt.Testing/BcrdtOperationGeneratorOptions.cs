namespace Bit.Bcrdt;

/// <summary>What a randomized run is allowed to do.</summary>
public sealed class BcrdtOperationGeneratorOptions
{
    /// <summary>Whether text edits are generated.</summary>
    public bool Text { get; init; } = true;

    /// <summary>Whether sequence edits are generated.</summary>
    public bool Array { get; init; } = true;

    /// <summary>Whether map edits are generated.</summary>
    public bool Map { get; init; } = true;

    /// <summary>Whether rich-text formatting is generated.</summary>
    public bool Formatting { get; init; } = true;

    /// <summary>Whether nested types are created.</summary>
    public bool NestedTypes { get; init; } = true;

    /// <summary>How many mutations a single generated transaction may contain.</summary>
    public int MaxOperationsPerTransaction { get; init; } = 4;

    /// <summary>How many distinct map keys to draw from. Fewer means more write conflicts.</summary>
    public int KeySpace { get; init; } = 8;

    /// <summary>Whether set edits are generated, on the <c>tags</c> root set.</summary>
    /// <remarks>
    /// Off by default, like <see cref="Counter"/>, so a seed recorded before these existed still replays
    /// the same schedule.
    /// </remarks>
    public bool Set { get; init; }

    /// <summary>
    /// Whether counter edits are generated, on the <c>votes</c> root counter and - with
    /// <see cref="NestedTypes"/> - on counters nested in the <c>meta</c> map.
    /// </summary>
    public bool Counter { get; init; }

    /// <summary>
    /// Whether XML tree edits are generated, on the <c>tree</c> root fragment: elements and texts inserted
    /// and removed anywhere in the tree - whole subtrees built detached included - attributes set and
    /// removed, and the texts edited and formatted.
    /// </summary>
    /// <remarks>Off by default, like <see cref="Set"/> and <see cref="Counter"/>.</remarks>
    public bool Xml { get; init; }
}
