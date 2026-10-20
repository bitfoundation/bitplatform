using System.Collections;

namespace Bit.Bcrdt;

/// <summary>
/// A collaborative add-wins observed-remove set.
/// </summary>
/// <remarks>
/// <para>
/// Membership is keyed by the canonical encoding of each element, so two replicas that add equal
/// values converge on one member rather than two. The semantics are <i>add-wins</i>: if one client
/// removes an element while another concurrently adds it, the element remains. That falls out of
/// the underlying model rather than being bolted on - a removal deletes the specific block it
/// observed, and a concurrent addition creates a different block that the removal never saw. An
/// addition only creates a block when the element is absent, though: adding an element this replica
/// already holds writes nothing, so a concurrent removal of that element stands.
/// </para>
/// <para>
/// Add-wins is the right default because removal is usually the destructive operation, and
/// resurrecting an element a user re-added is less surprising than silently discarding it. A
/// remove-wins set would need a different type; the underlying engine supports building one.
/// </para>
/// </remarks>
public sealed class BcrdtSet : BcrdtAbstractType, IEnumerable<object?>
{
    private List<object?>? _pending;

    /// <summary>Creates an empty set, not yet attached to a document.</summary>
    public BcrdtSet() => _pending = [];

    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Set;

    /// <summary>The number of elements.</summary>
    public int Count
    {
        get
        {
            if (_pending is not null) return _pending.Count;
            int count = 0;
            foreach (BcrdtItem item in KeyIndex.Values)
            {
                if (!item.Deleted) count++;
            }
            return count;
        }
    }

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtSet();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtSetEvent(this, transaction);

    protected internal override void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        base.Integrate(doc, item);

        if (_pending is null) return;
        List<object?> buffered = _pending;
        _pending = null;
        foreach (object? value in buffered) Add(value);
    }

    /// <summary>Adds an element. Adding an element that is already present is a no-op.</summary>
    /// <exception cref="NotSupportedException">
    /// The value is outside the collaborative value model, or is a nested collaborative type - which
    /// has no value-based identity a set could deduplicate on.
    /// </exception>
    public void Add(object? value)
    {
        (string key, object? member) = Prepare(value);

        if (_pending is not null)
        {
            // Membership is decided by the same canonical key the attached set uses, so a set built
            // up before it is inserted holds exactly what it will hold afterwards.
            if (!_pending.Any(existing => BcrdtAnyCodec.CanonicalKey(existing) == key)) _pending.Add(member);
            return;
        }

        if (BcrdtTypeOperations.MapHas(this, key)) return;
        Doc.Transact(transaction => BcrdtTypeOperations.MapSet(transaction, this, key, member));
    }

    /// <summary>Adds several elements, as a single transaction.</summary>
    /// <exception cref="NotSupportedException">An element cannot be stored; nothing is added.</exception>
    public void AddRange(IEnumerable<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        // Every element is validated before any is added.
        List<(string Key, object? Member)> prepared = [.. values.Select(Prepare)];

        if (_pending is not null)
        {
            foreach ((_, object? member) in prepared) Add(member);
            return;
        }

        Doc.Transact(transaction =>
        {
            foreach ((string key, object? member) in prepared)
            {
                if (!BcrdtTypeOperations.MapHas(this, key)) BcrdtTypeOperations.MapSet(transaction, this, key, member);
            }
        });
    }

    /// <summary>Removes an element.</summary>
    public bool Remove(object? value)
    {
        string key = BcrdtAnyCodec.CanonicalKey(value);
        if (_pending is not null) return _pending.RemoveAll(existing => BcrdtAnyCodec.CanonicalKey(existing) == key) > 0;

        if (!BcrdtTypeOperations.MapHas(this, key)) return false;
        Doc.Transact(transaction => BcrdtTypeOperations.MapDelete(transaction, this, key));
        return true;
    }

    /// <summary>Whether an element is present.</summary>
    public bool Contains(object? value)
    {
        string key = BcrdtAnyCodec.CanonicalKey(value);
        if (_pending is not null) return _pending.Any(existing => BcrdtAnyCodec.CanonicalKey(existing) == key);
        return BcrdtTypeOperations.MapHas(this, key);
    }

    /// <inheritdoc/>
    public override BcrdtSet Clone()
    {
        var clone = new BcrdtSet();
        clone.AddRange(ToArray());
        return clone;
    }

    private static (string Key, object? Member) Prepare(object? value)
    {
        if (value is BcrdtAbstractType)
        {
            throw new NotSupportedException(
                "A set holds values, and a nested collaborative type has no value-based identity to deduplicate on. " +
                "Store nested types in a map or an array instead.");
        }

        object? member = value is byte[] ? value : BcrdtAnyCodec.Canonicalize(value);
        return (BcrdtAnyCodec.CanonicalKey(member), member);
    }

    /// <summary>Removes every element, as a single transaction.</summary>
    public void Clear()
    {
        if (_pending is not null)
        {
            _pending.Clear();
            return;
        }

        Doc.Transact(transaction =>
        {
            foreach (string key in BcrdtTypeOperations.MapKeys(this).ToArray())
            {
                BcrdtTypeOperations.MapDelete(transaction, this, key);
            }
        });
    }

    /// <summary>A snapshot of the elements.</summary>
    public List<object?> ToArray() => [.. this];

    /// <summary>
    /// A plain-object projection, with elements in a deterministic order.
    /// </summary>
    /// <remarks>
    /// A set has no inherent order, so the projection sorts by each element's canonical encoding.
    /// Without that, two converged replicas would render their members in whatever order they
    /// happened to add them, and no snapshot comparison would ever hold.
    /// </remarks>
    public override object ToJson() =>
        ToArray()
            .OrderBy(BcrdtAnyCodec.CanonicalKey, StringComparer.Ordinal)
            .Select(value => value is BcrdtAbstractType nested ? nested.ToJson() : value)
            .ToList();

    /// <inheritdoc/>
    public IEnumerator<object?> GetEnumerator()
    {
        if (_pending is not null) return _pending.GetEnumerator();
        return BcrdtTypeOperations.MapEntries(this).Select(pair => pair.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// A root type whose kind has not been declared yet.
/// </summary>
/// <remarks>
/// A document can receive content for a root type before the application ever asks for it - remote
/// updates arrive by name, and the name alone does not say whether it is a map or an array. Content
/// lands here until the application calls for the root by a specific kind, at which point the
/// accumulated blocks are re-parented onto a properly typed instance. Nested types never need this,
/// because their kind travels with them on the wire.
/// </remarks>
public sealed class BcrdtUntypedType : BcrdtAbstractType
{
    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Untyped;

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtUntypedType();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtGenericEvent(this, transaction);

    /// <summary>
    /// Projects the accumulated content, inferring its shape from what it actually contains.
    /// </summary>
    /// <remarks>
    /// The kind is unknown by definition here, so it is inferred rather than declared: keyed content
    /// is a map, character content is text, anything else is a sequence. This keeps a document
    /// inspectable and comparable even for roots the application has not asked for yet, which is
    /// exactly the situation a replica is in right after receiving its first update.
    /// </remarks>
    public override object ToJson()
    {
        if (KeyIndex.Count > 0)
        {
            var map = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach ((string key, object? value) in BcrdtTypeOperations.MapEntries(this))
            {
                map[key] = value is BcrdtAbstractType nested ? nested.ToJson() : value;
            }
            return map;
        }

        if (Start is null) return new SortedDictionary<string, object?>(StringComparer.Ordinal);

        bool looksLikeText = false;
        for (BcrdtItem? item = Start; item is not null; item = item.Right)
        {
            if (item.Deleted) continue;
            if (item.Content is BcrdtContentString or BcrdtContentFormat)
            {
                looksLikeText = true;
                break;
            }
        }

        if (looksLikeText)
        {
            var builder = new System.Text.StringBuilder();
            for (BcrdtItem? item = Start; item is not null; item = item.Right)
            {
                if (!item.Deleted && item.Content is BcrdtContentString text) builder.Append(text.Text);
            }
            return builder.ToString();
        }

        return BcrdtTypeOperations.ListToArray(this)
            .Select(value => value is BcrdtAbstractType nested ? nested.ToJson() : value)
            .ToList();
    }
}
