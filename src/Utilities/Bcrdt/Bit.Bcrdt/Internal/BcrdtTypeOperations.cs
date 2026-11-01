namespace Bit.Bcrdt;

/// <summary>
/// The sequence and key operations shared by every collaborative type.
/// </summary>
/// <remarks>
/// Keeping insert, delete, read and iterate in one place is what lets a new collaborative type be
/// written as a thin API surface over the same proven mechanics, rather than as a fresh
/// re-derivation of positioning logic that has to be re-verified for convergence.
/// </remarks>
public static class BcrdtTypeOperations
{
    /// <summary>Inserts values into a sequence at an index.</summary>
    public static void ListInsert(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        int index,
        IReadOnlyList<object?> content)
    {
        if (index < 0 || index > parent.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), $"Index {index} is outside a sequence of length {parent.Length}.");
        }

        if (index == 0)
        {
            BcrdtSearchMarkerCache.ApplyChange(parent.SearchMarkers, index, content.Count);
            ListInsertAfter(transaction, parent, null, content);
            return;
        }

        int startIndex = index;
        BcrdtSearchMarker? marker = BcrdtSearchMarkerCache.Find(parent, index);
        BcrdtItem? cursor = parent.Start;

        if (marker is not null)
        {
            cursor = marker.Item;
            index -= marker.Index;

            // The scan below needs a block strictly to the left of the target position. Landing
            // exactly on a marker means stepping back one visible block first.
            if (index == 0)
            {
                cursor = cursor.Previous;
                index += cursor is not null && cursor.Countable && !cursor.Deleted ? cursor.Length : 0;
            }
        }

        for (; cursor is not null; cursor = cursor.Right)
        {
            if (cursor.Deleted || !cursor.Countable) continue;
            if (index <= cursor.Length)
            {
                if (index < cursor.Length)
                {
                    BcrdtStructStore.GetItemCleanStart(
                        transaction, new BcrdtItemId(cursor.Id.Client, cursor.Id.Clock + index));
                }
                break;
            }
            index -= cursor.Length;
        }

        BcrdtSearchMarkerCache.ApplyChange(parent.SearchMarkers, startIndex, content.Count);
        ListInsertAfter(transaction, parent, cursor, content);
    }

    /// <summary>
    /// Inserts values immediately after a block, packing consecutive primitives into single blocks.
    /// </summary>
    /// <remarks>
    /// Run packing is why inserting a thousand array elements costs a handful of blocks rather than
    /// a thousand. Values that cannot share a block - a nested type, a byte array - flush the run
    /// and take a block of their own.
    /// </remarks>
    public static void ListInsertAfter(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        BcrdtItem? reference,
        IReadOnlyList<object?> content)
    {
        // Validate everything before integrating anything. A value that cannot be stored must be
        // rejected while that is still free; discovering it halfway through would leave the elements
        // before it inserted and the rest missing.
        object?[] prepared = PrepareValues(content, parent);

        BcrdtItem? left = reference;
        BcrdtItem? right = reference is null ? parent.Start : reference.Right;

        List<object?> run = [];

        void FlushRun()
        {
            if (run.Count == 0) return;
            left = CreateAndIntegrate(transaction, parent, left, right, null, BcrdtContentAny.FromCanonical(run));
            run = [];
        }

        foreach (object? value in prepared)
        {
            switch (value)
            {
                case BcrdtAbstractType nested:
                    FlushRun();
                    left = CreateAndIntegrate(transaction, parent, left, right, null, new BcrdtContentType(nested));
                    break;

                case byte[] binary:
                    FlushRun();
                    left = CreateAndIntegrate(transaction, parent, left, right, null, new BcrdtContentBinary(binary));
                    break;

                default:
                    run.Add(value);
                    break;
            }
        }

        FlushRun();
    }

    /// <summary>
    /// Validates values for insertion and converts them to their stored form, without changing anything.
    /// </summary>
    /// <exception cref="NotSupportedException">A value is outside the collaborative value model.</exception>
    /// <exception cref="InvalidOperationException">
    /// A nested type already belongs to a document, or appears more than once.
    /// </exception>
    internal static object?[] PrepareValues(IReadOnlyList<object?> content, BcrdtAbstractType? owner = null)
    {
        var prepared = new object?[content.Count];
        HashSet<BcrdtAbstractType>? nestedTypes = null;

        for (int i = 0; i < content.Count; i++)
        {
            object? value = content[i];
            switch (value)
            {
                case BcrdtAbstractType nested:
                    EnsureInsertable(nested, owner);
                    if (!(nestedTypes ??= new HashSet<BcrdtAbstractType>(ReferenceEqualityComparer.Instance)).Add(nested))
                    {
                        throw new InvalidOperationException(
                            "The same collaborative type instance appears more than once. Each insertion needs its own instance.");
                    }
                    prepared[i] = nested;
                    break;

                case byte[] binary:
                    prepared[i] = binary;
                    break;

                default:
                    prepared[i] = BcrdtAnyCodec.Canonicalize(value);
                    break;
            }
        }

        return prepared;
    }

    /// <summary>Rejects a nested type that cannot be inserted, before anything is changed.</summary>
    /// <remarks>
    /// A type instance is bound to exactly one place in one document. Inserting it a second time
    /// would point two blocks at one set of contents, and deleting either - which is how a move is
    /// done - would delete the contents out from under the other. Remote replicas only ever receive
    /// an empty type for the second insertion, so the replicas would not even agree on the damage.
    /// </remarks>
    /// <param name="nested">The type about to be inserted.</param>
    /// <param name="owner">
    /// The container it is going into. A detached type waiting inside a detached container may be
    /// inserted by that container - that is how the container's buffered contents are replayed - but
    /// not by anyone else.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The type already belongs to a document, is already waiting in another detached container, or
    /// would end up containing itself.
    /// </exception>
    internal static void EnsureInsertable(BcrdtAbstractType nested, BcrdtAbstractType? owner = null)
    {
        if (nested.IsAttached)
        {
            throw new InvalidOperationException(
                "This collaborative type already belongs to a document and cannot be inserted again. " +
                "Insert a new instance, or a Clone() of this one.");
        }

        if (nested.PendingOwner is { } claimant && !ReferenceEquals(claimant, owner))
        {
            throw new InvalidOperationException(
                "This collaborative type has already been placed in another container. Each insertion " +
                "needs its own instance.");
        }

        // A detached container can be filled in any order, so a cycle has to be caught here: on
        // integration it would recurse until one of the two insertions found the other attached.
        for (BcrdtAbstractType? ancestor = owner; ancestor is not null; ancestor = ancestor.PendingOwner)
        {
            if (ReferenceEquals(ancestor, nested))
            {
                throw new InvalidOperationException("A collaborative type cannot contain itself.");
            }
        }
    }

    /// <summary>
    /// Validates values for a detached container's buffer, and records that its nested types now
    /// belong to that container.
    /// </summary>
    internal static object?[] PreparePendingValues(IReadOnlyList<object?> content, BcrdtAbstractType owner)
    {
        object?[] prepared = PrepareValues(content, owner);
        foreach (object? value in prepared)
        {
            if (value is BcrdtAbstractType nested)
            {
                if (ReferenceEquals(nested.PendingOwner, owner))
                {
                    throw new InvalidOperationException(
                        "The same collaborative type instance appears more than once. Each insertion needs its own instance.");
                }
            }
        }
        foreach (object? value in prepared)
        {
            if (value is BcrdtAbstractType nested) nested.PendingOwner = owner;
        }
        return prepared;
    }

    /// <summary>Releases a nested type removed from a detached container's buffer.</summary>
    internal static void ReleasePending(object? value, BcrdtAbstractType owner)
    {
        if (value is BcrdtAbstractType nested && ReferenceEquals(nested.PendingOwner, owner)) nested.PendingOwner = null;
    }

    /// <summary>Deletes a run of visible units from a sequence.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The run extends outside the sequence. Checked before anything is deleted.
    /// </exception>
    public static void ListDelete(BcrdtTransaction transaction, BcrdtAbstractType parent, int index, int length)
    {
        if (index < 0 || index > parent.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), $"Index {index} is outside a sequence of length {parent.Length}.");
        }
        if (length < 0 || length > parent.Length - index)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), $"Cannot delete {length} unit(s) at index {index} of a sequence of length {parent.Length}.");
        }
        if (length == 0) return;

        int startIndex = index;
        int startLength = length;

        BcrdtSearchMarker? marker = BcrdtSearchMarkerCache.Find(parent, index);
        BcrdtItem? cursor = parent.Start;
        if (marker is not null)
        {
            cursor = marker.Item;
            index -= marker.Index;
        }

        for (; cursor is not null && index > 0; cursor = cursor.Right)
        {
            if (cursor.Deleted || !cursor.Countable) continue;
            if (index < cursor.Length)
            {
                BcrdtStructStore.GetItemCleanStart(transaction, new BcrdtItemId(cursor.Id.Client, cursor.Id.Clock + index));
            }
            index -= cursor.Length;
        }

        while (length > 0 && cursor is not null)
        {
            if (!cursor.Deleted)
            {
                if (length < cursor.Length)
                {
                    BcrdtStructStore.GetItemCleanStart(
                        transaction, new BcrdtItemId(cursor.Id.Client, cursor.Id.Clock + length));
                }
                cursor.Delete(transaction);
                length -= cursor.Length;
            }
            cursor = cursor.Right;
        }

        if (length > 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), "Deletion range extends past the end of the sequence.");
        }

        BcrdtSearchMarkerCache.ApplyChange(parent.SearchMarkers, startIndex, -startLength + length);
    }

    /// <summary>Reads the value at an index, or <see langword="null"/> if the index is out of range.</summary>
    public static object? ListGet(BcrdtAbstractType parent, int index)
    {
        if (index < 0 || index >= parent.Length) return null;

        BcrdtSearchMarker? marker = BcrdtSearchMarkerCache.Find(parent, index);
        BcrdtItem? cursor = parent.Start;
        if (marker is not null)
        {
            cursor = marker.Item;
            index -= marker.Index;
        }

        for (; cursor is not null; cursor = cursor.Right)
        {
            if (cursor.Deleted || !cursor.Countable) continue;
            if (index < cursor.Length) return cursor.Content is BcrdtContentAny values ? values[index] : cursor.Content.GetContent()[index];
            index -= cursor.Length;
        }
        return null;
    }

    /// <summary>Enumerates every visible value in a sequence.</summary>
    public static IEnumerable<object?> ListEnumerate(BcrdtAbstractType parent)
    {
        for (BcrdtItem? cursor = parent.Start; cursor is not null; cursor = cursor.Right)
        {
            if (cursor.Deleted || !cursor.Countable) continue;
            foreach (object? value in cursor.Content.GetContent()) yield return value;
        }
    }

    /// <summary>Materializes a sequence into a list.</summary>
    public static List<object?> ListToArray(BcrdtAbstractType parent)
    {
        var result = new List<object?>(parent.Length);
        for (BcrdtItem? cursor = parent.Start; cursor is not null; cursor = cursor.Right)
        {
            if (cursor.Deleted || !cursor.Countable) continue;
            result.AddRange(cursor.Content.GetContent());
        }
        return result;
    }

    /// <summary>Writes a value to a map key, superseding whatever was there.</summary>
    public static void MapSet(BcrdtTransaction transaction, BcrdtAbstractType parent, string key, object? value)
    {
        BcrdtItem? left = parent.MapGetRaw(key);
        IBcrdtContent content;
        switch (value)
        {
            case BcrdtAbstractType nested:
                EnsureInsertable(nested, parent);
                content = new BcrdtContentType(nested);
                break;
            case byte[] binary:
                content = new BcrdtContentBinary(binary);
                break;
            default:
                content = new BcrdtContentAny(value);
                break;
        }

        CreateAndIntegrate(transaction, parent, left, null, key, content);
    }

    /// <summary>Reads a map key's current value, or <see langword="null"/> if it has none.</summary>
    public static object? MapGet(BcrdtAbstractType parent, string key)
    {
        BcrdtItem? item = parent.MapGetRaw(key);
        if (item is null || item.Deleted) return null;
        IReadOnlyList<object?> content = item.Content.GetContent();
        return content.Count == 0 ? null : content[item.Length - 1];
    }

    /// <summary>Whether a map key currently has a value.</summary>
    public static bool MapHas(BcrdtAbstractType parent, string key)
    {
        BcrdtItem? item = parent.MapGetRaw(key);
        return item is not null && !item.Deleted;
    }

    /// <summary>Removes a map key.</summary>
    public static void MapDelete(BcrdtTransaction transaction, BcrdtAbstractType parent, string key) =>
        parent.MapGetRaw(key)?.Delete(transaction);

    /// <summary>Enumerates the keys that currently have values.</summary>
    public static IEnumerable<string> MapKeys(BcrdtAbstractType parent)
    {
        foreach ((string key, BcrdtItem item) in parent.KeyIndex)
        {
            if (!item.Deleted) yield return key;
        }
    }

    /// <summary>Enumerates the key/value pairs that currently have values.</summary>
    public static IEnumerable<KeyValuePair<string, object?>> MapEntries(BcrdtAbstractType parent)
    {
        foreach ((string key, BcrdtItem item) in parent.KeyIndex)
        {
            if (item.Deleted) continue;
            IReadOnlyList<object?> content = item.Content.GetContent();
            yield return new KeyValuePair<string, object?>(key, content.Count == 0 ? null : content[item.Length - 1]);
        }
    }

    /// <summary>Stamps a new block with the local client's next clock and places it.</summary>
    public static BcrdtItem CreateAndIntegrate(
        BcrdtTransaction transaction,
        BcrdtAbstractType parent,
        BcrdtItem? left,
        BcrdtItem? right,
        string? parentKey,
        IBcrdtContent content)
    {
        BcrdtDocument doc = transaction.Doc;
        var item = new BcrdtItem(
            new BcrdtItemId(doc.ClientId, doc.Store.GetState(doc.ClientId)),
            left,
            left?.LastId,
            right,
            right?.Id,
            parent,
            parentKey,
            content);

        item.Integrate(transaction, 0);
        return item;
    }
}
