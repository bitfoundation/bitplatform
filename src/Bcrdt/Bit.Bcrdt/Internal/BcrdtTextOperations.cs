namespace Bit.Bcrdt;

/// <summary>
/// A cursor into a text sequence: where it is, and what formatting is in force there.
/// </summary>
/// <remarks>
/// Text operations cannot work from an index alone, because inserting at a position also has to
/// decide which formatting the new text inherits and which marks have to be re-established after
/// it. Carrying the attribute state along with the position is what lets a single left-to-right
/// pass answer both questions.
/// </remarks>
internal sealed class BcrdtTextCursor(BcrdtItem? left, BcrdtItem? right, int index, Dictionary<string, object?> attributes)
{
    public BcrdtItem? Left = left;
    public BcrdtItem? Right = right;
    public int Index = index;
    public readonly Dictionary<string, object?> Attributes = attributes;

    /// <summary>Steps one block to the right, folding any mark it passes into the attribute state.</summary>
    public void Forward()
    {
        if (Right is null) throw new InvalidOperationException("Cannot advance a cursor past the end of the text.");

        if (Right.Content is BcrdtContentFormat format)
        {
            if (!Right.Deleted) BcrdtAttributeComparer.Apply(Attributes, format);
        }
        else if (!Right.Deleted)
        {
            Index += Right.Length;
        }

        Left = Right;
        Right = Right.Right;
    }
}

/// <summary>
/// Position, insertion, formatting and deletion for rich text.
/// </summary>
/// <remarks>
/// <para>
/// Rich text is harder than a plain sequence because formatting is represented by zero-width marks
/// stored inline. Marks have to be anchored between characters - that is what makes formatting
/// survive concurrent editing - but that means every operation has to reason about the marks it
/// passes, the marks it must insert, and the marks it must re-establish afterwards so that text
/// beyond the edit keeps the formatting it had.
/// </para>
/// <para>
/// The recurring pattern below is: minimize (skip marks that already say what we want), insert the
/// marks we need while remembering their negations, do the work, then insert the negations so the
/// change stops where it should.
/// </para>
/// </remarks>
internal static class BcrdtTextOperations
{
    /// <summary>Advances a cursor by <paramref name="count"/> visible units, splitting if needed.</summary>
    public static BcrdtTextCursor FindNextPosition(BcrdtTransaction transaction, BcrdtTextCursor cursor, int count)
    {
        while (cursor.Right is not null && count > 0)
        {
            if (cursor.Right.Content is BcrdtContentFormat format)
            {
                if (!cursor.Right.Deleted) BcrdtAttributeComparer.Apply(cursor.Attributes, format);
            }
            else if (!cursor.Right.Deleted)
            {
                if (count < cursor.Right.Length)
                {
                    BcrdtStructStore.GetItemCleanStart(
                        transaction, new BcrdtItemId(cursor.Right.Id.Client, cursor.Right.Id.Clock + count));
                }
                cursor.Index += cursor.Right.Length;
                count -= cursor.Right.Length;
            }

            cursor.Left = cursor.Right;
            cursor.Right = cursor.Right.Right;
        }
        return cursor;
    }

    /// <summary>Builds a cursor at a character index.</summary>
    /// <param name="transaction">The transaction the lookup happens in.</param>
    /// <param name="parent">The text type being addressed.</param>
    /// <param name="index">The character index to position at.</param>
    /// <param name="useSearchMarker">
    /// Whether the index cache may be used. The cache jumps straight to a nearby block, which skips
    /// the marks in between and so cannot reconstruct the formatting in force there. Callers that
    /// need accurate inherited formatting must pass <see langword="false"/> and pay for the scan.
    /// </param>
    public static BcrdtTextCursor FindPosition(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        int index,
        bool useSearchMarker)
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        BcrdtSearchMarker? marker = useSearchMarker ? BcrdtSearchMarkerCache.Find(parent, index) : null;

        if (marker is not null)
        {
            var cursor = new BcrdtTextCursor(marker.Item.Left, marker.Item, marker.Index, attributes);
            return FindNextPosition(transaction, cursor, index - marker.Index);
        }

        return FindNextPosition(transaction, new BcrdtTextCursor(null, parent.Start, 0, attributes), index);
    }

    /// <summary>Skips over marks that already say what the caller is about to say.</summary>
    private static void MinimizeAttributeChanges(BcrdtTextCursor cursor, IReadOnlyDictionary<string, object?> attributes)
    {
        while (cursor.Right is not null)
        {
            bool redundant =
                cursor.Right.Deleted ||
                (cursor.Right.Content is BcrdtContentFormat format &&
                 BcrdtAttributeComparer.Equal(attributes.GetValueOrDefault(format.Key), format.Value));

            if (!redundant) break;
            cursor.Forward();
        }
    }

    /// <summary>
    /// Inserts the marks needed to establish <paramref name="attributes"/> at the cursor.
    /// </summary>
    /// <returns>
    /// The marks needed to undo the change afterwards, so the formatting does not bleed into the
    /// text that follows.
    /// </returns>
    private static Dictionary<string, object?> InsertAttributes(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        BcrdtTextCursor cursor,
        IReadOnlyDictionary<string, object?> attributes)
    {
        var negated = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach ((string key, object? value) in attributes)
        {
            object? current = cursor.Attributes.GetValueOrDefault(key);
            if (BcrdtAttributeComparer.Equal(current, value)) continue;

            negated[key] = current;
            cursor.Right = BcrdtTypeOperations.CreateAndIntegrate(
                transaction, parent, cursor.Left, cursor.Right, null, new BcrdtContentFormat(key, value));
            cursor.Forward();
        }

        return negated;
    }

    /// <summary>Re-establishes the formatting that was in force before an insertion.</summary>
    private static void InsertNegatedAttributes(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        BcrdtTextCursor cursor,
        Dictionary<string, object?> negated)
    {
        // If the marks we were about to write already exist to the right, adopt them instead of
        // duplicating them. Without this every insertion at a formatting boundary would leave a
        // redundant pair of marks behind, and a long editing session would accumulate thousands.
        // Only a mark for a key we are about to re-establish can be adopted. A mark for any other
        // key - even one that clears an attribute, which a missing key would wrongly compare equal
        // to - belongs to the text that follows and must stay on its side of the new marks.
        while (cursor.Right is not null &&
               (cursor.Right.Deleted ||
                (cursor.Right.Content is BcrdtContentFormat existing &&
                 negated.TryGetValue(existing.Key, out object? pending) &&
                 BcrdtAttributeComparer.Equal(pending, existing.Value))))
        {
            if (!cursor.Right.Deleted && cursor.Right.Content is BcrdtContentFormat adopted)
            {
                negated.Remove(adopted.Key);
            }
            cursor.Forward();
        }

        foreach ((string key, object? value) in negated)
        {
            cursor.Right = BcrdtTypeOperations.CreateAndIntegrate(
                transaction, parent, cursor.Left, cursor.Right, null, new BcrdtContentFormat(key, value));
            cursor.Forward();
        }
    }

    /// <summary>Inserts text or an embedded value at the cursor with the given formatting.</summary>
    public static void InsertText(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        BcrdtTextCursor cursor,
        IBcrdtContent payload,
        Dictionary<string, object?> attributes)
    {
        // Any attribute in force at the cursor that the caller did not ask for must be explicitly
        // switched off, or the new text would silently inherit it.
        foreach (string key in cursor.Attributes.Keys)
        {
            attributes.TryAdd(key, null);
        }

        MinimizeAttributeChanges(cursor, attributes);
        Dictionary<string, object?> negated = InsertAttributes(transaction, parent, cursor, attributes);

        BcrdtSearchMarkerCache.ApplyChange(parent.SearchMarkers, cursor.Index, payload.Length);

        cursor.Right = BcrdtTypeOperations.CreateAndIntegrate(
            transaction, parent, cursor.Left, cursor.Right, null, payload);
        cursor.Forward();

        InsertNegatedAttributes(transaction, parent, cursor, negated);
    }

    /// <summary>An embedded value that has been validated, as distinct from text.</summary>
    /// <remarks>A string can be embedded too, which is why the two need telling apart.</remarks>
    internal sealed record BcrdtEmbedValue(object? Value);

    /// <summary>
    /// Validates text or an embedded value for insertion, without creating anything yet.
    /// </summary>
    /// <param name="content">The text, embedded value, byte array or nested type.</param>
    /// <param name="asEmbed">Whether a string is an embedded value rather than text.</param>
    /// <param name="owner">The text being inserted into.</param>
    /// <returns>
    /// A string for text, the nested type, the byte array, or a <see cref="BcrdtEmbedValue"/>. The
    /// payload itself is created at the moment of insertion by <see cref="CreatePayload"/>, so a
    /// payload object is never shared between two blocks.
    /// </returns>
    /// <exception cref="NotSupportedException">An embedded value is outside the value model.</exception>
    /// <exception cref="InvalidOperationException">A nested type already belongs somewhere else.</exception>
    public static object PrepareContent(object content, bool asEmbed, BcrdtAbstractType owner)
    {
        switch (content)
        {
            case string text when !asEmbed:
                return text;
            case BcrdtAbstractType nested:
                BcrdtTypeOperations.EnsureInsertable(nested, owner);
                return nested;
            case byte[] binary:
                return binary;
            default:
                return new BcrdtEmbedValue(BcrdtAnyCodec.Canonicalize(content));
        }
    }

    /// <summary>Creates the payload for content validated by <see cref="PrepareContent"/>.</summary>
    public static IBcrdtContent CreatePayload(object prepared) => prepared switch
    {
        string text => new BcrdtContentString(text),
        BcrdtAbstractType nested => new BcrdtContentType(nested),
        byte[] binary => new BcrdtContentBinary(binary),
        BcrdtEmbedValue embed => new BcrdtContentEmbed(embed.Value),
        _ => throw new ArgumentException($"Unprepared content of type {prepared.GetType()}.", nameof(prepared)),
    };

    /// <summary>How many index positions prepared content occupies.</summary>
    public static int PreparedLength(object prepared) => prepared is string text ? text.Length : 1;

    /// <summary>
    /// Copies caller-supplied formatting into the form the marks will store, validating every value
    /// before any mark is written.
    /// </summary>
    /// <exception cref="NotSupportedException">A value is outside the collaborative value model.</exception>
    public static Dictionary<string, object?> PrepareAttributes(IReadOnlyDictionary<string, object?> attributes)
    {
        var prepared = new Dictionary<string, object?>(attributes.Count, StringComparer.Ordinal);
        foreach ((string key, object? value) in attributes)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(attributes));
            prepared[key] = BcrdtAnyCodec.Canonicalize(value);
        }
        return prepared;
    }

    /// <summary>Applies formatting to a run of text starting at the cursor.</summary>
    public static void FormatText(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        BcrdtTextCursor cursor,
        int length,
        IReadOnlyDictionary<string, object?> attributes)
    {
        MinimizeAttributeChanges(cursor, attributes);
        Dictionary<string, object?> negated = InsertAttributes(transaction, parent, cursor, attributes);

        while (cursor.Right is not null &&
               (length > 0 ||
                (negated.Count > 0 && (cursor.Right.Deleted || cursor.Right.Content is BcrdtContentFormat))))
        {
            if (!cursor.Right.Deleted)
            {
                if (cursor.Right.Content is BcrdtContentFormat format)
                {
                    if (attributes.TryGetValue(format.Key, out object? desired))
                    {
                        if (BcrdtAttributeComparer.Equal(desired, format.Value))
                        {
                            // The run already carries this mark, so the closing mark we reserved is
                            // unnecessary; this one will do the job.
                            negated.Remove(format.Key);
                        }
                        else if (length == 0)
                        {
                            // Past the end of the formatted run, a mark setting this attribute to
                            // something else belongs to the text that follows. Stop here, so the
                            // closing marks land before it rather than after it - after it, they
                            // would override the formatting it establishes.
                            break;
                        }
                        else
                        {
                            negated[format.Key] = format.Value;
                        }

                        cursor.Right.Delete(transaction);
                    }
                }
                else
                {
                    if (length < cursor.Right.Length)
                    {
                        BcrdtStructStore.GetItemCleanStart(
                            transaction, new BcrdtItemId(cursor.Right.Id.Client, cursor.Right.Id.Clock + length));
                    }
                    length -= cursor.Right.Length;
                }
            }

            cursor.Forward();
        }

        // Formatting past the end of the text implies the text should be that long. Editors that
        // model a document as always ending in a newline rely on this.
        if (length > 0)
        {
            cursor.Right = BcrdtTypeOperations.CreateAndIntegrate(
                transaction, parent, cursor.Left, cursor.Right, null,
                new BcrdtContentString(new string('\n', length)));
            cursor.Forward();
        }

        InsertNegatedAttributes(transaction, parent, cursor, negated);
    }

    /// <summary>Deletes a run of text starting at the cursor.</summary>
    public static void DeleteText(BcrdtTransaction transaction, BcrdtTextCursor cursor, int length)
    {
        int startLength = length;
        var startAttributes = new Dictionary<string, object?>(cursor.Attributes, StringComparer.Ordinal);
        BcrdtItem? start = cursor.Right;

        while (length > 0 && cursor.Right is not null)
        {
            if (!cursor.Right.Deleted && cursor.Right.Content is BcrdtContentType or BcrdtContentEmbed or BcrdtContentString or BcrdtContentBinary)
            {
                if (length < cursor.Right.Length)
                {
                    BcrdtStructStore.GetItemCleanStart(
                        transaction, new BcrdtItemId(cursor.Right.Id.Client, cursor.Right.Id.Clock + length));
                }
                length -= cursor.Right.Length;
                cursor.Right.Delete(transaction);
            }
            cursor.Forward();
        }

        if (start is not null)
        {
            CleanupFormattingGap(transaction, start, cursor.Right, startAttributes, cursor.Attributes);
        }

        BcrdtAbstractType? parent = (cursor.Left ?? cursor.Right)?.Parent as BcrdtAbstractType;
        if (parent is not null)
        {
            BcrdtSearchMarkerCache.ApplyChange(parent.SearchMarkers, cursor.Index, -startLength + length);
        }
    }

    /// <summary>
    /// Removes marks that a deletion has made meaningless.
    /// </summary>
    /// <remarks>
    /// Deleting the text between an opening and a closing mark leaves both marks behind, applying
    /// formatting to nothing. Left alone they accumulate: a document edited heavily enough would
    /// carry more formatting metadata than text. This pass removes any mark that is either
    /// overwritten by a later one in the same gap or already implied by the formatting that was in
    /// force before the deletion.
    /// </remarks>
    private static void CleanupFormattingGap(
        BcrdtTransaction transaction,
        BcrdtItem? start,
        BcrdtItem? current,
        Dictionary<string, object?> startAttributes,
        Dictionary<string, object?> currentAttributes)
    {
        BcrdtItem? end = start;
        var lastFormatPerKey = new Dictionary<string, BcrdtContentFormat>(StringComparer.Ordinal);

        while (end is not null && (!end.Countable || end.Deleted))
        {
            if (!end.Deleted && end.Content is BcrdtContentFormat format) lastFormatPerKey[format.Key] = format;
            end = end.Right;
        }

        bool reachedCurrent = false;
        while (start is not null && start != end)
        {
            if (current == start) reachedCurrent = true;

            if (!start.Deleted && start.Content is BcrdtContentFormat format)
            {
                object? startValue = startAttributes.GetValueOrDefault(format.Key);
                bool overwritten = !ReferenceEquals(lastFormatPerKey.GetValueOrDefault(format.Key), format);

                if (overwritten || BcrdtAttributeComparer.Equal(startValue, format.Value))
                {
                    start.Delete(transaction);

                    if (!reachedCurrent &&
                        BcrdtAttributeComparer.Equal(currentAttributes.GetValueOrDefault(format.Key), format.Value) &&
                        !BcrdtAttributeComparer.Equal(startValue, format.Value))
                    {
                        if (startValue is null) currentAttributes.Remove(format.Key);
                        else currentAttributes[format.Key] = startValue;
                    }
                }

                if (!reachedCurrent && !start.Deleted) BcrdtAttributeComparer.Apply(currentAttributes, format);
            }

            start = start.Right;
        }
    }
}
