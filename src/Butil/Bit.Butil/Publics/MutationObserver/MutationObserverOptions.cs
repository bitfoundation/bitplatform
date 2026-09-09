namespace Bit.Butil;

/// <summary>
/// Options for <see href="https://developer.mozilla.org/en-US/docs/Web/API/MutationObserver/observe">MutationObserver.observe()</see>.
/// </summary>
public class MutationObserverOptions
{
    /// <summary>Watch for added or removed children.</summary>
    public bool ChildList { get; set; }

    /// <summary>Watch for attribute changes on the target.</summary>
    public bool Attributes { get; set; }

    /// <summary>Watch for character-data changes within the target.</summary>
    public bool CharacterData { get; set; }

    /// <summary>Apply the chosen options to the entire subtree, not just the immediate target.</summary>
    public bool Subtree { get; set; }

    /// <summary>Include the previous attribute value in each <see cref="MutationRecord"/>.</summary>
    public bool AttributeOldValue { get; set; }

    /// <summary>Include the previous character-data value in each <see cref="MutationRecord"/>.</summary>
    public bool CharacterDataOldValue { get; set; }

    /// <summary>Optional whitelist of attribute names to watch. <c>null</c> means all.</summary>
    public string[]? AttributeFilter { get; set; }

    /// <summary>
    /// The shortest time allowed between two calls into the .NET handler, in milliseconds.
    /// <c>0</c> - the default - forwards every batch of records.
    /// <br/>
    /// A subtree observer over a region Blazor re-renders sees a batch per render, so without an
    /// interval one render becomes one interop round trip.
    /// </summary>
    /// <remarks>
    /// Applied in JavaScript, before the round trip. Leading-edge with a trailing send, so the
    /// batch describing the settled tree always arrives.
    /// <br/>
    /// What the gate drops is whole batches, not individual records: a handler that has to see
    /// every mutation - a change log, an undo stack - must leave this at <c>0</c>. One that reacts
    /// to the current state of the tree, which is nearly all of them, does not.
    /// <br/>
    /// The unit is milliseconds rather than a <see cref="System.TimeSpan"/> because this type is
    /// serialized to the browser as-is.
    /// </remarks>
    public double MinInterval { get; set; }
}
