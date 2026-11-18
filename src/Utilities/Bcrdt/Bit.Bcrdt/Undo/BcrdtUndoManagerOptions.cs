namespace Bit.Bcrdt;

/// <summary>Settings for a <see cref="BcrdtUndoManager"/>.</summary>
public sealed class BcrdtUndoManagerOptions
{
    /// <summary>
    /// How long consecutive changes keep merging into one undo step.
    /// </summary>
    /// <remarks>
    /// Typing a word should be one undo step, not one per keystroke. Set to zero to make every
    /// transaction its own step.
    /// </remarks>
    public TimeSpan CaptureTimeout { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Which transaction origins this manager is responsible for.
    /// </summary>
    /// <remarks>
    /// Defaults to untagged local changes, which is what application code produces. Remote updates
    /// are tagged by their sync provider and therefore excluded - undo must never revert somebody
    /// else's work.
    /// </remarks>
    public HashSet<object?> TrackedOrigins { get; init; } = [null];

    /// <summary>An optional veto on deleting a particular block while undoing.</summary>
    public Func<BcrdtItem, bool>? DeleteFilter { get; init; }

    /// <summary>
    /// How many steps each stack keeps; the oldest is dropped when another is added. Zero keeps every
    /// step.
    /// </summary>
    /// <remarks>
    /// Every step pins the content it deleted so that undoing it can bring that content back, which is
    /// memory a long session never gets back otherwise. A dropped step releases what it pinned, and the
    /// content is reclaimed.
    /// </remarks>
    public int MaxStackItems { get; init; } = 100;

    /// <summary>
    /// Whether a map key redone locally should ignore a concurrent remote write to the same key.
    /// </summary>
    /// <remarks>
    /// Off by default: if somebody else has since written the key, redoing over their value would
    /// silently discard it, so the redo is abandoned instead.
    /// </remarks>
    public bool IgnoreRemoteMapChanges { get; init; }
}
