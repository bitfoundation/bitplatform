namespace Bit.Bcrdt;

/// <summary>One reversible step on an undo or redo stack.</summary>
/// <remarks>
/// A step records two ranges of clock space rather than a copy of anything: what the action created,
/// and what it removed. Undoing is then a matter of deleting the first and re-creating the second,
/// which touches only this user's own changes and leaves concurrent work by others untouched.
/// </remarks>
public sealed class BcrdtUndoStackItem(BcrdtDeleteSet deletions, BcrdtDeleteSet insertions)
{
    /// <summary>What the action deleted, and what undoing it will restore.</summary>
    public BcrdtDeleteSet Deletions { get; internal set; } = deletions;

    /// <summary>What the action created, and what undoing it will remove.</summary>
    public BcrdtDeleteSet Insertions { get; internal set; } = insertions;

    /// <summary>Arbitrary caller state, for attaching cursor positions or labels to a step.</summary>
    public Dictionary<string, object?> Meta { get; } = new(StringComparer.Ordinal);
}
