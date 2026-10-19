using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// What changed in a type that has no more specific event, such as a root whose kind is not yet known.
/// </summary>
public sealed class BcrdtGenericEvent(BcrdtAbstractType target, BcrdtTransaction transaction)
    : BcrdtEvent(target, transaction);

/// <summary>What changed in a <see cref="BcrdtMap"/> during one transaction.</summary>
public sealed class BcrdtMapEvent(BcrdtMap target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The map that changed.</summary>
    public new BcrdtMap Target => (BcrdtMap)base.Target;
}

/// <summary>What changed in a <see cref="BcrdtSet"/> during one transaction.</summary>
public sealed class BcrdtSetEvent(BcrdtSet target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The set that changed.</summary>
    public new BcrdtSet Target => (BcrdtSet)base.Target;

    /// <summary>The elements added by the transaction.</summary>
    public IEnumerable<object?> Added => Keys
        .Where(p => p.Value.Action is BcrdtKeyChangeAction.Add)
        .Select(p => p.Value.NewValue);

    /// <summary>The elements removed by the transaction.</summary>
    public IEnumerable<object?> Removed => Keys
        .Where(p => p.Value.Action is BcrdtKeyChangeAction.Delete)
        .Select(p => p.Value.OldValue);
}

/// <summary>What changed in a <see cref="BcrdtArray"/> during one transaction.</summary>
public sealed class BcrdtArrayEvent(BcrdtArray target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The array that changed.</summary>
    public new BcrdtArray Target => (BcrdtArray)base.Target;
}

/// <summary>
/// What changed in a <see cref="BcrdtText"/> during one transaction, including formatting.
/// </summary>
/// <remarks>
/// Text deltas are computed separately from sequence deltas because rich-text marks are stored
/// inline as zero-width items. A naïve sequence walk would either report marks as content - shifting
/// every index a consumer sees - or ignore them entirely and lose formatting changes. The walk here
/// tracks the attribute state as it goes, so what a subscriber receives is expressed purely in
/// character positions with attributes attached to the steps that carry them.
/// </remarks>
public sealed class BcrdtTextEvent(BcrdtText target, BcrdtTransaction transaction) : BcrdtEvent(target, transaction)
{
    /// <summary>The text that changed.</summary>
    public new BcrdtText Target => (BcrdtText)base.Target;

    /// <summary>
    /// The change as a rich-text delta. <see cref="BcrdtEvent.Delta"/> returns the same thing, so a
    /// handler holding the event as a plain <see cref="BcrdtEvent"/> - a deep observer, say - sees
    /// text expressed in characters rather than as a list of single-character values.
    /// </summary>
    protected override IReadOnlyList<BcrdtDeltaOperation> ComputeDelta()
    {
        List<BcrdtDeltaOperation> delta = [];

        // Attributes in force at the cursor, and the attributes that were in force before the
        // transaction. The second is needed to tell "this mark changed" from "this mark was already
        // like that", which decides whether a retain needs to carry attributes at all.
        var currentAttributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        var previousAttributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        var pendingAttributes = new Dictionary<string, object?>(StringComparer.Ordinal);

        BcrdtDeltaKind? action = null;
        var insert = new StringBuilder();
        object? insertEmbed = null;
        int retain = 0;
        int deleteLength = 0;

        void Emit()
        {
            if (action is null) return;
            switch (action)
            {
                case BcrdtDeltaKind.Delete:
                    if (deleteLength > 0) delta.Add(BcrdtDeltaOperation.Delete(deleteLength));
                    deleteLength = 0;
                    break;

                case BcrdtDeltaKind.Insert:
                    if (insertEmbed is not null)
                    {
                        delta.Add(BcrdtDeltaOperation.InsertEmbed(insertEmbed, SnapshotAttributes(currentAttributes)));
                        insertEmbed = null;
                    }
                    else if (insert.Length > 0)
                    {
                        delta.Add(BcrdtDeltaOperation.InsertText(insert.ToString(), SnapshotAttributes(currentAttributes)));
                        insert.Clear();
                    }
                    break;

                case BcrdtDeltaKind.Retain:
                    if (retain > 0)
                    {
                        delta.Add(BcrdtDeltaOperation.Retain(
                            retain,
                            pendingAttributes.Count > 0
                                ? new Dictionary<string, object?>(pendingAttributes, StringComparer.Ordinal)
                                : null));
                    }
                    retain = 0;
                    break;
            }
            action = null;
        }

        for (BcrdtItem? item = Target.Start; item is not null; item = item.Right)
        {
            switch (item.Content)
            {
                case BcrdtContentType or BcrdtContentEmbed or BcrdtContentBinary:
                    if (WasAdded(item))
                    {
                        if (!WasDeleted(item))
                        {
                            Emit();
                            action = BcrdtDeltaKind.Insert;
                            insertEmbed = item.Content.GetContent()[0];
                            Emit();
                        }
                    }
                    else if (WasDeleted(item))
                    {
                        if (action != BcrdtDeltaKind.Delete) { Emit(); action = BcrdtDeltaKind.Delete; }
                        deleteLength += 1;
                    }
                    else if (!item.Deleted)
                    {
                        if (action != BcrdtDeltaKind.Retain) { Emit(); action = BcrdtDeltaKind.Retain; }
                        retain += 1;
                    }
                    break;

                case BcrdtContentString text:
                    if (WasAdded(item))
                    {
                        if (!WasDeleted(item))
                        {
                            if (action != BcrdtDeltaKind.Insert) { Emit(); action = BcrdtDeltaKind.Insert; }
                            insert.Append(text.Text);
                        }
                    }
                    else if (WasDeleted(item))
                    {
                        if (action != BcrdtDeltaKind.Delete) { Emit(); action = BcrdtDeltaKind.Delete; }
                        deleteLength += item.Length;
                    }
                    else if (!item.Deleted)
                    {
                        if (action != BcrdtDeltaKind.Retain) { Emit(); action = BcrdtDeltaKind.Retain; }
                        retain += item.Length;
                    }
                    break;

                case BcrdtContentFormat format:
                    HandleFormat(item, format);
                    break;
            }
        }

        Emit();

        // A run of trailing retains carrying no attributes says nothing, so trim it.
        while (delta.Count > 0 &&
               delta[^1].Kind == BcrdtDeltaKind.Retain &&
               delta[^1].Attributes is null)
        {
            delta.RemoveAt(delta.Count - 1);
        }

        return delta;

        void HandleFormat(BcrdtItem item, BcrdtContentFormat format)
        {
            if (WasAdded(item))
            {
                if (!WasDeleted(item))
                {
                    currentAttributes.TryGetValue(format.Key, out object? currentValue);
                    if (!BcrdtAttributeComparer.Equal(currentValue, format.Value))
                    {
                        if (action == BcrdtDeltaKind.Retain) Emit();
                        previousAttributes.TryGetValue(format.Key, out object? oldValue);
                        if (BcrdtAttributeComparer.Equal(format.Value, oldValue)) pendingAttributes.Remove(format.Key);
                        else pendingAttributes[format.Key] = format.Value;
                    }
                }
            }
            else if (WasDeleted(item))
            {
                previousAttributes[format.Key] = format.Value;
                currentAttributes.TryGetValue(format.Key, out object? currentValue);
                if (!BcrdtAttributeComparer.Equal(currentValue, format.Value))
                {
                    if (action == BcrdtDeltaKind.Retain) Emit();
                    pendingAttributes[format.Key] = currentValue;
                }
            }
            else if (!item.Deleted)
            {
                previousAttributes[format.Key] = format.Value;
                if (pendingAttributes.TryGetValue(format.Key, out object? pending) &&
                    !BcrdtAttributeComparer.Equal(pending, format.Value))
                {
                    if (action == BcrdtDeltaKind.Retain) Emit();
                    if (format.Value is null) pendingAttributes.Remove(format.Key);
                    else pendingAttributes[format.Key] = format.Value;
                }
            }

            if (!item.Deleted)
            {
                if (action == BcrdtDeltaKind.Insert) Emit();
                BcrdtAttributeComparer.Apply(currentAttributes, format);
            }
        }
    }

    private static IReadOnlyDictionary<string, object?>? SnapshotAttributes(Dictionary<string, object?> attributes)
    {
        Dictionary<string, object?>? result = null;
        foreach ((string key, object? value) in attributes)
        {
            if (value is null) continue;
            result ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            result[key] = value;
        }
        return result;
    }
}
