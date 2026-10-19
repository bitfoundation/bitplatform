using System.Collections;

namespace Bit.Bcrdt;

/// <summary>
/// A collaborative string-keyed map.
/// </summary>
/// <remarks>
/// <para>
/// Keys are independent: two clients writing different keys never conflict, and neither write is
/// lost. Two clients writing the <i>same</i> key concurrently resolve last-writer-wins, where the
/// winner is decided by the engine's deterministic ordering rather than by wall-clock time - so
/// every replica agrees on the winner, but which one wins is arbitrary from the application's point
/// of view.
/// </para>
/// <para>
/// When losing a concurrent write is unacceptable, store a nested type under the key instead of a
/// scalar. A nested <see cref="BcrdtMap"/>, <see cref="BcrdtArray"/> or <see cref="BcrdtText"/> merges
/// concurrent changes rather than discarding one side.
/// </para>
/// </remarks>
public sealed class BcrdtMap : BcrdtAbstractType, IEnumerable<KeyValuePair<string, object?>>
{
    private Dictionary<string, object?>? _pending;

    /// <summary>Creates an empty map, not yet attached to a document.</summary>
    /// <remarks>
    /// A detached map can be populated before it is inserted anywhere; the buffered contents are
    /// replayed as real collaborative operations at the moment of insertion. This is what allows a
    /// nested structure to be built up in one expression instead of insert-then-populate.
    /// </remarks>
    public BcrdtMap() => _pending = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Map;

    /// <summary>The number of keys that currently have values.</summary>
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

    /// <summary>The keys that currently have values.</summary>
    public IEnumerable<string> Keys => _pending?.Keys ?? BcrdtTypeOperations.MapKeys(this);

    /// <summary>The values currently stored.</summary>
    public IEnumerable<object?> Values => this.Select(pair => pair.Value);

    /// <summary>Reads or writes a key.</summary>
    public object? this[string key]
    {
        get => Get(key);
        set => Set(key, value);
    }

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtMap();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtMapEvent(this, transaction);

    protected internal override void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        base.Integrate(doc, item);

        if (_pending is null) return;
        Dictionary<string, object?> buffered = _pending;
        _pending = null;
        foreach ((string key, object? value) in buffered) Set(key, value);
    }

    /// <summary>Writes a value to a key.</summary>
    /// <param name="key">The key to write.</param>
    /// <param name="value">
    /// A value from the collaborative value model, a <see cref="byte"/> array, or a nested
    /// collaborative type.
    /// </param>
    /// <exception cref="NotSupportedException">The value is outside the collaborative value model.</exception>
    /// <exception cref="InvalidOperationException">A nested type already belongs somewhere else.</exception>
    public void Set(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_pending is not null)
        {
            bool hasPrevious = _pending.TryGetValue(key, out object? previous);
            if (hasPrevious && value is BcrdtAbstractType && ReferenceEquals(previous, value)) return;

            object? prepared = BcrdtTypeOperations.PreparePendingValues([value], this)[0];
            if (hasPrevious) BcrdtTypeOperations.ReleasePending(previous, this);
            _pending[key] = prepared;
            return;
        }

        Doc.Transact(transaction => BcrdtTypeOperations.MapSet(transaction, this, key, value));
    }

    /// <summary>Writes a value projected onto the collaborative value model.</summary>
    /// <remarks>
    /// Use this for application types that are not part of the value model; it flattens the object
    /// through JSON on the way in and <see cref="Get{T}"/> rehydrates it on the way out.
    /// </remarks>
    public void SetObject<T>(string key, T value) => Set(key, BcrdtAnyCodec.FromObject(value));

    /// <summary>Reads a key's value, or <see langword="null"/> if it has none.</summary>
    public object? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_pending is not null) return _pending.GetValueOrDefault(key);
        return BcrdtTypeOperations.MapGet(this, key);
    }

    /// <summary>Reads a key's value as <typeparamref name="T"/>.</summary>
    public T? Get<T>(string key)
    {
        object? value = Get(key);
        if (value is T typed) return typed;
        if (value is null) return default;
        return BcrdtAnyCodec.ToObject<T>(value);
    }

    /// <summary>Reads a key's value if it has one.</summary>
    public bool TryGetValue(string key, out object? value)
    {
        if (!ContainsKey(key))
        {
            value = null;
            return false;
        }
        value = Get(key);
        return true;
    }

    /// <summary>Whether a key currently has a value.</summary>
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_pending is not null) return _pending.ContainsKey(key);
        return BcrdtTypeOperations.MapHas(this, key);
    }

    /// <summary>Removes a key.</summary>
    public void Delete(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_pending is not null)
        {
            if (_pending.Remove(key, out object? removed)) BcrdtTypeOperations.ReleasePending(removed, this);
            return;
        }
        Doc.Transact(transaction => BcrdtTypeOperations.MapDelete(transaction, this, key));
    }

    /// <summary>Removes every key, as a single transaction.</summary>
    public void Clear()
    {
        if (_pending is not null)
        {
            foreach (object? removed in _pending.Values) BcrdtTypeOperations.ReleasePending(removed, this);
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

    /// <summary>Reads a nested map, or <see langword="null"/> if the key holds something else.</summary>
    public BcrdtMap? GetMap(string key) => Get(key) as BcrdtMap;

    /// <summary>Reads a nested array, or <see langword="null"/> if the key holds something else.</summary>
    public BcrdtArray? GetArray(string key) => Get(key) as BcrdtArray;

    /// <summary>Reads nested text, or <see langword="null"/> if the key holds something else.</summary>
    public BcrdtText? GetText(string key) => Get(key) as BcrdtText;

    /// <summary>Reads a nested set, or <see langword="null"/> if the key holds something else.</summary>
    public BcrdtSet? GetSet(string key) => Get(key) as BcrdtSet;

    /// <summary>Returns the nested map at a key, creating it if the key is empty.</summary>
    /// <remarks>
    /// The create-if-absent pattern is safe here in a way it is not in a database: if two clients
    /// call this concurrently, one of the two maps wins deterministically and the loser's contents
    /// are discarded. To merge rather than discard, have both clients write into a map that already
    /// exists.
    /// </remarks>
    public BcrdtMap EnsureMap(string key) => Ensure(key, static () => new BcrdtMap());

    /// <summary>Returns the nested array at a key, creating it if the key is empty.</summary>
    public BcrdtArray EnsureArray(string key) => Ensure(key, static () => new BcrdtArray());

    /// <summary>Returns the nested text at a key, creating it if the key is empty.</summary>
    public BcrdtText EnsureText(string key) => Ensure(key, static () => new BcrdtText());

    /// <summary>Returns the nested set at a key, creating it if the key is empty.</summary>
    public BcrdtSet EnsureSet(string key) => Ensure(key, static () => new BcrdtSet());

    private T Ensure<T>(string key, Func<T> factory) where T : BcrdtAbstractType
    {
        if (Get(key) is T existing) return existing;
        T created = factory();
        Set(key, created);
        return created;
    }

    /// <inheritdoc/>
    public override BcrdtMap Clone()
    {
        var clone = new BcrdtMap();
        foreach ((string key, object? value) in this) clone.Set(key, CloneValue(value));
        return clone;
    }

    /// <summary>
    /// A plain-object projection, with keys in a deterministic order.
    /// </summary>
    /// <remarks>
    /// Ordering is sorted rather than insertion-ordered on purpose. Two replicas that have converged
    /// hold the same keys but generally inserted them in different orders, so an insertion-ordered
    /// projection would render them differently and make every snapshot comparison a false negative.
    /// </remarks>
    public override object ToJson()
    {
        var result = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string key, object? value) in this)
        {
            result[key] = value is BcrdtAbstractType nested ? nested.ToJson() : value;
        }
        return result;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
        (_pending ?? (IEnumerable<KeyValuePair<string, object?>>)BcrdtTypeOperations.MapEntries(this)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
