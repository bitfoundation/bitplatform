using System.Collections;

namespace Bit.Bcrdt;

/// <summary>
/// A collaborative ordered sequence.
/// </summary>
/// <remarks>
/// <para>
/// Concurrent insertions at the same position are ordered deterministically, and - unlike a naïve
/// timestamp-ordered sequence - a run inserted by one client stays contiguous rather than
/// interleaving with a run inserted by another. Concurrent deletion of the same element is
/// idempotent: it disappears once, not twice, and never resurrects.
/// </para>
/// <para>
/// Indices are positions in the <i>visible</i> sequence. Deleted elements leave no index behind,
/// so an index means the same thing to an application as it would for a <see cref="List{T}"/>.
/// </para>
/// </remarks>
public sealed class BcrdtArray : BcrdtAbstractType, IEnumerable<object?>
{
    private List<object?>? _pending;

    /// <summary>Creates an empty array, not yet attached to a document.</summary>
    public BcrdtArray() => _pending = [];

    /// <inheritdoc/>
    public override BcrdtTypeKind Kind => BcrdtTypeKind.Array;

    /// <summary>The number of visible elements.</summary>
    public int Count => _pending?.Count ?? Length;

    /// <summary>Reads the element at an index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the sequence.</exception>
    public object? this[int index]
    {
        get
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _pending is not null ? _pending[index] : BcrdtTypeOperations.ListGet(this, index);
        }
    }

    protected internal override BcrdtAbstractType CreateInstance() => new BcrdtArray();

    protected internal override BcrdtEvent CreateEvent(BcrdtTransaction transaction, HashSet<string?> changedKeys) =>
        new BcrdtArrayEvent(this, transaction);

    protected internal override void Integrate(BcrdtDocument doc, BcrdtItem? item)
    {
        base.Integrate(doc, item);

        if (_pending is null) return;
        List<object?> buffered = _pending;
        _pending = null;
        if (buffered.Count > 0) Insert(0, buffered);
    }

    /// <summary>Inserts values at an index.</summary>
    /// <param name="index">Where to insert. Must be between zero and <see cref="Count"/> inclusive.</param>
    /// <param name="values">The values to insert, in order.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the sequence.</exception>
    /// <exception cref="NotSupportedException">A value is outside the collaborative value model.</exception>
    /// <exception cref="InvalidOperationException">A nested type already belongs somewhere else.</exception>
    public void Insert(int index, IReadOnlyList<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ValidateIndex(index, Count, inclusive: true);
        if (values.Count == 0) return;

        if (_pending is not null)
        {
            _pending.InsertRange(index, BcrdtTypeOperations.PreparePendingValues(values, this));
            return;
        }

        Doc.Transact(transaction => BcrdtTypeOperations.ListInsert(transaction, this, index, values));
    }

    /// <summary>Inserts a single value at an index.</summary>
    public void Insert(int index, object? value) => Insert(index, new[] { value });

    /// <summary>Appends values.</summary>
    /// <remarks>A bare <see langword="null"/> argument appends a single null element.</remarks>
    public void Push(params object?[]? values) => Insert(Count, values ?? [null]);

    /// <summary>Appends a sequence of values.</summary>
    public void PushRange(IEnumerable<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Insert(Count, [.. values]);
    }

    /// <summary>Prepends values.</summary>
    /// <remarks>A bare <see langword="null"/> argument prepends a single null element.</remarks>
    public void Unshift(params object?[]? values) => Insert(0, values ?? [null]);

    /// <summary>Removes a run of elements.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The run extends outside the sequence.</exception>
    public void Delete(int index, int length = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        int count = Count;
        ValidateIndex(index, count, inclusive: true);
        if (length > count - index)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), $"Cannot delete {length} element(s) at index {index} of a sequence of length {count}.");
        }
        if (length == 0) return;

        if (_pending is not null)
        {
            for (int i = index; i < index + length; i++) BcrdtTypeOperations.ReleasePending(_pending[i], this);
            _pending.RemoveRange(index, length);
            return;
        }

        Doc.Transact(transaction => BcrdtTypeOperations.ListDelete(transaction, this, index, length));
    }

    /// <summary>Removes every element, as a single transaction.</summary>
    public void Clear() => Delete(0, Count);

    /// <summary>
    /// Replaces the element at an index.
    /// </summary>
    /// <remarks>
    /// Delete-then-insert, in one transaction. Note that this is genuinely a replacement and not an
    /// edit: a concurrent change to the old element is lost, because the element it applied to no
    /// longer exists. For mergeable updates, store a nested type and mutate it in place.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the sequence.</exception>
    public void Replace(int index, object? value)
    {
        ValidateIndex(index, Count, inclusive: false);

        if (_pending is not null)
        {
            object? replacement = BcrdtTypeOperations.PreparePendingValues([value], this)[0];
            BcrdtTypeOperations.ReleasePending(_pending[index], this);
            _pending[index] = replacement;
            return;
        }

        // Validated before the old element is removed, so a value that cannot be stored leaves the
        // sequence untouched instead of one element shorter.
        BcrdtTypeOperations.PrepareValues([value], this);

        Doc.Transact(transaction =>
        {
            BcrdtTypeOperations.ListDelete(transaction, this, index, 1);
            BcrdtTypeOperations.ListInsert(transaction, this, index, new[] { value });
        });
    }

    /// <summary>Moves an element to a different position, as a single transaction.</summary>
    /// <param name="fromIndex">The element to move.</param>
    /// <param name="toIndex">
    /// Where to put it, as a position in the sequence <i>before</i> the element is lifted out: the
    /// element ends up just before whatever is currently at <paramref name="toIndex"/>, or at the
    /// end when it equals <see cref="Count"/>.
    /// </param>
    /// <remarks>
    /// Implemented as remove-then-insert, which means two clients moving the same element
    /// concurrently can produce two copies of it. A move that must be conflict-free needs a
    /// dedicated move operation; this method is the pragmatic choice for lists where that race is
    /// acceptable, such as a single user reordering their own items. A nested type is moved by
    /// inserting a <see cref="BcrdtAbstractType.Clone"/> of it, because removing the original removes
    /// everything inside it.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either index is outside the sequence.</exception>
    public void Move(int fromIndex, int toIndex)
    {
        int count = Count;
        ValidateIndex(fromIndex, count, inclusive: false);
        ValidateIndex(toIndex, count, inclusive: true);

        // Both of these name the position the element already occupies.
        if (toIndex == fromIndex || toIndex == fromIndex + 1) return;
        int target = toIndex > fromIndex ? toIndex - 1 : toIndex;

        if (_pending is not null)
        {
            object? pendingValue = _pending[fromIndex];
            _pending.RemoveAt(fromIndex);
            _pending.Insert(target, pendingValue);
            return;
        }

        Doc.Transact(transaction =>
        {
            object? value = BcrdtTypeOperations.ListGet(this, fromIndex);
            if (value is BcrdtAbstractType nested) value = nested.Clone();

            BcrdtTypeOperations.ListDelete(transaction, this, fromIndex, 1);
            BcrdtTypeOperations.ListInsert(transaction, this, target, new[] { value });
        });
    }

    /// <inheritdoc/>
    public override BcrdtArray Clone()
    {
        var clone = new BcrdtArray();
        clone.Insert(0, ToArray().Select(CloneValue).ToList());
        return clone;
    }

    private static void ValidateIndex(int index, int count, bool inclusive)
    {
        if (index < 0 || index > count || (!inclusive && index == count))
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), $"Index {index} is outside a sequence of length {count}.");
        }
    }

    /// <summary>A snapshot of the sequence as a list.</summary>
    public List<object?> ToArray() => _pending is not null ? [.. _pending] : BcrdtTypeOperations.ListToArray(this);

    /// <summary>A snapshot of the sequence, projected to <typeparamref name="T"/>.</summary>
    public List<T?> ToArray<T>() => [.. ToArray().Select(value => value is T typed ? typed : BcrdtAnyCodec.ToObject<T>(value))];

    /// <summary>Returns the element at an index as a nested map, if it is one.</summary>
    public BcrdtMap? GetMap(int index) => this[index] as BcrdtMap;

    /// <summary>Returns the element at an index as a nested array, if it is one.</summary>
    public BcrdtArray? GetArray(int index) => this[index] as BcrdtArray;

    /// <summary>Returns the element at an index as nested text, if it is one.</summary>
    public BcrdtText? GetText(int index) => this[index] as BcrdtText;

    /// <inheritdoc/>
    public override object ToJson() =>
        ToArray().Select(value => value is BcrdtAbstractType nested ? nested.ToJson() : value).ToList();

    /// <inheritdoc/>
    public IEnumerator<object?> GetEnumerator() =>
        (_pending ?? (IEnumerable<object?>)BcrdtTypeOperations.ListEnumerate(this)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
