namespace Bit.Bcrdt;

/// <summary>Describes the single edit that turns one string into another.</summary>
/// <param name="Index">Where the change starts.</param>
/// <param name="DeletedLength">How many characters were removed.</param>
/// <param name="Inserted">What was inserted in their place.</param>
public readonly record struct BcrdtTextEdit(int Index, int DeletedLength, string Inserted)
{
    /// <summary>Whether the two strings were already equal.</summary>
    public bool IsEmpty => DeletedLength == 0 && Inserted.Length == 0;
}

/// <summary>
/// Turns "the textbox now says this" into a minimal collaborative edit.
/// </summary>
/// <remarks>
/// <para>
/// Every UI framework, Blazor included, hands you the <i>new whole value</i> of an input, not the
/// change the user made. Writing that whole value into a collaborative text field would be a
/// catastrophe: it would delete every character and reinsert it, discarding every concurrent edit
/// anyone else made anywhere in the field, moving every remote cursor to the start, and producing an
/// update the size of the document for a single keystroke.
/// </para>
/// <para>
/// This recovers the actual edit by trimming the common prefix and suffix. For a person typing,
/// pasting or deleting a selection the result covers exactly the characters they changed, so it
/// merges with concurrent work, keeps remote cursors anchored, and costs a few bytes on the wire.
/// Where the change is ambiguous - typing an <c>a</c> into <c>aaa</c> could have happened at any of
/// four places - the caret-hinted overload of <see cref="Diff(string, string, int)"/> uses the
/// caret to pick the place the user actually typed.
/// </para>
/// <para>
/// It is a prefix/suffix diff, not a full edit-script diff. That is a deliberate trade: it is
/// linear to compute, and it is exact for contiguous edits, which is what interactive typing
/// produces. A change with two separated edits collapses into one span covering both, which is
/// still correct - merely less surgical than an optimal diff would be.
/// </para>
/// </remarks>
public static class BcrdtTextSynchronizer
{
    /// <summary>Computes the edit that turns <paramref name="current"/> into <paramref name="desired"/>.</summary>
    public static BcrdtTextEdit Diff(string current, string desired)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(desired);

        if (string.Equals(current, desired, StringComparison.Ordinal))
        {
            return new BcrdtTextEdit(0, 0, string.Empty);
        }

        int limit = Math.Min(current.Length, desired.Length);

        int prefix = 0;
        while (prefix < limit && current[prefix] == desired[prefix]) prefix++;

        int suffix = 0;
        int maxSuffix = limit - prefix;
        while (suffix < maxSuffix &&
               current[current.Length - 1 - suffix] == desired[desired.Length - 1 - suffix])
        {
            suffix++;
        }

        return Build(current, desired, prefix, suffix);
    }

    /// <summary>
    /// Computes the edit that turns <paramref name="current"/> into <paramref name="desired"/>, using
    /// the caret to resolve ambiguity.
    /// </summary>
    /// <param name="current">The value before the edit.</param>
    /// <param name="desired">The value after the edit.</param>
    /// <param name="caretAfterEdit">
    /// Where the caret sits in <paramref name="desired"/> after the edit - the input's
    /// <c>selectionEnd</c>. A user's edit ends at the caret, so of the places the edit could have
    /// happened, the one ending there - or nearest to it - is chosen.
    /// </param>
    /// <remarks>
    /// <para>
    /// Typing an <c>a</c> after the first character of <c>aaa</c> produces <c>aaaa</c>, which the
    /// plain diff reports as an insertion at the end. The text is the same either way, but the
    /// position is not: a collaborator's concurrent insertion between the original characters would
    /// land on the wrong side of it, and their cursor anchors would move. The caret disambiguates.
    /// </para>
    /// <para>
    /// The caret only ever chooses among edits of the smallest size; it never makes the edit larger.
    /// It is not always next to the change: a value set by script (a browser extension correcting a
    /// word, say) leaves the caret at the end of the text, and the deleting half of a drag and drop
    /// leaves it where the text is about to be dropped. An edit stretched to reach the caret would
    /// then replace everything in between - every concurrent edit there lost, every cursor anchored
    /// there moved.
    /// </para>
    /// </remarks>
    public static BcrdtTextEdit Diff(string current, string desired, int caretAfterEdit)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(desired);

        if (string.Equals(current, desired, StringComparison.Ordinal))
        {
            return new BcrdtTextEdit(0, 0, string.Empty);
        }

        int limit = Math.Min(current.Length, desired.Length);

        // The longest common prefix and the longest common suffix, each on its own. A smallest edit
        // keeps `kept` characters in all, as any split of them into a prefix and a suffix no longer
        // than those two: the edit can slide across a run of repeated characters without changing
        // the result, and only there does the caret have anything to choose.
        int longestPrefix = 0;
        while (longestPrefix < limit && current[longestPrefix] == desired[longestPrefix]) longestPrefix++;

        int longestSuffix = 0;
        while (longestSuffix < limit &&
               current[current.Length - 1 - longestSuffix] == desired[desired.Length - 1 - longestSuffix])
        {
            longestSuffix++;
        }

        int kept = Math.Min(limit, longestPrefix + longestSuffix);

        // The edit ends at desired.Length - suffix; ending at the caret means prefix = caret - (desired.Length - kept).
        int prefix = Math.Clamp(caretAfterEdit - (desired.Length - kept), kept - longestSuffix, Math.Min(longestPrefix, kept));

        return Build(current, desired, prefix, kept - prefix);
    }

    private static BcrdtTextEdit Build(string current, string desired, int prefix, int suffix)
    {
        // Never cut between the halves of a surrogate pair. A cut lands between them exactly when the
        // character just after it is a low surrogate; widening the edit by one moves the cut outside
        // the pair, so the character is replaced whole rather than split into two lone halves that a
        // concurrent edit could separate for good.
        bool adjusted;
        do
        {
            adjusted = false;
            if (prefix > 0 && (IsLowSurrogateAt(current, prefix) || IsLowSurrogateAt(desired, prefix)))
            {
                prefix--;
                adjusted = true;
            }

            if (suffix > 0 &&
                (IsLowSurrogateAt(current, current.Length - suffix) || IsLowSurrogateAt(desired, desired.Length - suffix)))
            {
                suffix--;
                adjusted = true;
            }
        }
        while (adjusted);

        int deleted = current.Length - prefix - suffix;
        string inserted = desired[prefix..(desired.Length - suffix)];
        return new BcrdtTextEdit(prefix, deleted, inserted);
    }

    private static bool IsLowSurrogateAt(string value, int index) =>
        index >= 0 && index < value.Length && char.IsLowSurrogate(value[index]);

    /// <summary>
    /// Applies the minimal edit that makes a collaborative text field read <paramref name="desired"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="desired"/> is compared with <see cref="BcrdtText.ToString"/>, which omits
    /// embedded objects. Embeds are therefore invisible to the edit: positions are translated around
    /// them, and they are never deleted by it.
    /// </remarks>
    /// <returns><see langword="true"/> if anything changed.</returns>
    public static bool Apply(BcrdtText text, string desired, object? origin = null) =>
        Apply(text, desired, origin, caretAfterEdit: null);

    /// <summary>
    /// Applies the minimal edit that makes a collaborative text field read <paramref name="desired"/>,
    /// using the caret to resolve ambiguity. See <see cref="Diff(string, string, int)"/>.
    /// </summary>
    /// <returns><see langword="true"/> if anything changed.</returns>
    public static bool Apply(BcrdtText text, string desired, object? origin, int? caretAfterEdit)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(desired);

        return ApplyProjected(text, BcrdtTextProjection.Create(text), desired, origin, caretAfterEdit);
    }

    /// <summary>
    /// Applies the edit that makes <paramref name="projection"/>'s display read
    /// <paramref name="desired"/>, translated into document positions.
    /// </summary>
    internal static bool ApplyProjected(
        BcrdtText text,
        BcrdtTextProjection projection,
        string desired,
        object? origin,
        int? caretAfterEdit)
    {
        BcrdtTextEdit edit = caretAfterEdit is int caret
            ? Diff(projection.Display, desired, caret)
            : Diff(projection.Display, desired);
        if (edit.IsEmpty) return false;

        IReadOnlyList<(int Start, int Length)> deletions =
            projection.ToDocumentRanges(edit.Index, edit.Index + edit.DeletedLength);

        // A replacement goes where the replaced text was; a pure insertion goes immediately after the
        // character to its left, which keeps it on the correct side of any embed.
        int insertAt = deletions.Count > 0 ? deletions[0].Start : projection.ToDocumentInsertion(edit.Index);

        // One transaction, so the delete and the insert reach peers as a single replacement rather
        // than as a moment where the text is missing.
        text.Doc.Transact(_ =>
        {
            for (int i = deletions.Count - 1; i >= 0; i--) text.Delete(deletions[i].Start, deletions[i].Length);
            if (edit.Inserted.Length > 0) text.Insert(insertAt, edit.Inserted);
        }, origin);

        return true;
    }

    /// <summary>
    /// Maps a caret position across an edit, so a local caret does not jump when text changes.
    /// </summary>
    /// <remarks>
    /// A caret before the edit is unaffected; one after it shifts by the change in length; one
    /// inside the deleted span collapses to the end of the inserted text, which is where a user
    /// expects to be left after a replacement.
    /// </remarks>
    public static int MapCaret(int caret, BcrdtTextEdit edit)
    {
        if (caret <= edit.Index) return caret;
        if (caret >= edit.Index + edit.DeletedLength) return caret - edit.DeletedLength + edit.Inserted.Length;
        return edit.Index + edit.Inserted.Length;
    }

    /// <summary>
    /// Maps a caret position across a remote change, so somebody else typing does not move your caret.
    /// </summary>
    /// <remarks>
    /// Without this, every keystroke from a collaborator earlier in the document shifts the local
    /// caret by one and the user finds themselves typing in the wrong place. Walking the delta and
    /// accumulating only the changes that landed before the caret keeps it on the character it was on.
    /// Positions are document positions, as the delta's are.
    /// </remarks>
    public static int MapCaretThroughDelta(int caret, IReadOnlyList<BcrdtDeltaOperation> delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        int position = 0;
        int result = caret;

        foreach (BcrdtDeltaOperation operation in delta)
        {
            switch (operation.Kind)
            {
                case BcrdtDeltaKind.Retain:
                    position += operation.Length;
                    break;

                case BcrdtDeltaKind.Insert:
                    // The length in document units: an embedded string occupies one position.
                    int inserted = operation.Length;
                    if (position < result) result += inserted;
                    position += inserted;
                    break;

                case BcrdtDeltaKind.Delete:
                    if (position < result) result -= Math.Min(operation.Length, result - position);
                    break;
            }

            if (position > result) break;
        }

        return Math.Max(0, result);
    }
}
