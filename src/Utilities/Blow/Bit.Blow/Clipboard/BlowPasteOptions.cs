namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowClipboard.Paste"/>.</summary>
public sealed class BlowPasteOptions
{
    /// <summary>
    /// How far the pasted copy is offset from the original, so it does not land exactly on top
    /// of what it was copied from and look like nothing happened.
    /// </summary>
    public BlowXYPosition Offset { get; set; } = new(24, 24);

    /// <summary>
    /// Ids already in use, so the paste can avoid colliding with them. Pass the current graph's
    /// ids; without them a second paste would produce duplicates of the first.
    /// </summary>
    public IEnumerable<string>? ExistingIds { get; set; }

    /// <summary>
    /// Names the copy of an element. Receives the original id and a de-duplication suffix that
    /// is only non-empty when the obvious name is taken. The default is <c>{id}-copy</c>,
    /// then <c>{id}-copy-2</c> and so on.
    /// </summary>
    public Func<string, int, string>? IdFactory { get; set; }

    /// <summary>
    /// Whether the pasted elements come back selected - usually what you want, since the user
    /// almost always wants to move or delete what they just pasted.
    /// </summary>
    public bool Select { get; set; } = true;
}
