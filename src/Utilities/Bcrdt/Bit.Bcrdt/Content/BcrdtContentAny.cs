namespace Bit.Bcrdt;

/// <summary>
/// A run of values from the collaborative value model.
/// </summary>
/// <remarks>
/// Values are packed into a single block so that inserting a hundred array elements in one
/// transaction costs one block rather than a hundred. Each value still has its own identifier -
/// the block spans a clock range - so concurrent edits can address an individual element.
/// </remarks>
public sealed class BcrdtContentAny : IBcrdtContent
{
    private const int RetentionDivisor = 4;

    private List<object?> _values;
    private int _start;
    private int _count;

    // The runs that read from _values since it was split, or null if no split ever shared it.
    private Readers? _readers;

    /// <summary>Creates a payload holding a single value.</summary>
    /// <exception cref="NotSupportedException">The value is outside the collaborative value model.</exception>
    /// <remarks>
    /// The value is stored in canonical form - see <see cref="BcrdtAnyCodec.Canonicalize(object?)"/> - so this
    /// replica holds exactly what its peers will decode, and later changes to the caller's object
    /// cannot alter the document behind the engine's back.
    /// </remarks>
    public BcrdtContentAny(object? value) : this([BcrdtAnyCodec.Canonicalize(value)], true) { }

    /// <summary>Creates a payload holding a run of values.</summary>
    /// <exception cref="NotSupportedException">A value is outside the collaborative value model.</exception>
    public BcrdtContentAny(IEnumerable<object?> values) : this([.. values.Select(BcrdtAnyCodec.Canonicalize)], true) { }

    private BcrdtContentAny(List<object?> values, bool _) : this(values, 0, values.Count, null) { }

    private BcrdtContentAny(List<object?> values, int start, int count, Readers? readers)
    {
        _values = values;
        _start = start;
        _count = count;
        _readers = readers;
    }

    /// <summary>Wraps values that have already been through <see cref="BcrdtAnyCodec.Canonicalize(object?)"/>.</summary>
    internal static BcrdtContentAny FromCanonical(List<object?> values) => new(values, true);

    /// <inheritdoc/>
    public int Length => _count;

    /// <inheritdoc/>
    public bool Countable => true;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Any;

    /// <inheritdoc/>
    /// <remarks>
    /// Always a view of the run as it is now, never the list behind it: that list may still grow when the
    /// run absorbs a neighbour, and a caller enumerating it then - pushing to an array while walking it,
    /// say - would have the enumeration fail under it.
    /// </remarks>
    public IReadOnlyList<object?> GetContent() => new Slice(_values, _start, _count);

    /// <inheritdoc/>
    public object? GetValue(int index) => ValueAt(_values, _start, _count, index);

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentAny(_values.GetRange(_start, _count), true);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, _count);

        _readers ??= new Readers();
        _readers.Count++;
        var right = new BcrdtContentAny(_values, _start + offset, _count - offset, _readers);
        _count = offset;
        Detach();
        right.Detach();
        return right;
    }

    /// <inheritdoc/>
    public bool CanMergeWith(IBcrdtContent right) => right is BcrdtContentAny;

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right)
    {
        if (right is not BcrdtContentAny other) return false;

        if (ReferenceEquals(_values, other._values) && _start + _count == other._start)
        {
            _count += other._count;
            other.Release();
            return true;
        }

        // Only the list's one reader may grow it, and only at its end. A run that shares its list with
        // another it was split from would keep everything it gains alive for as long as that neighbour
        // lives - long after the run itself has been deleted and collected - so it moves to a list of its
        // own first, sized for what it is about to absorb.
        if (_readers is { Count: > 1 } || _start + _count != _values.Count)
        {
            var values = new List<object?>(_count + other._count);
            for (int i = 0; i < _count; i++) values.Add(_values[_start + i]);
            MoveTo(values);
        }

        for (int i = 0; i < other._count; i++) _values.Add(other._values[other._start + i]);
        _count += other._count;
        other.Release();
        return true;
    }

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) { }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) => Release();

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset)
    {
        int count = _count - offset;
        writer.WriteVarUInt((ulong)count);
        for (int i = offset; i < _count; i++)
        {
            BcrdtAnyCodec.Write(ref writer, _values[_start + i]);
        }
    }

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentAny Read(ref BcrdtBufferReader reader)
    {
        int count = reader.ValidateCount(reader.ReadVarUInt32(), "value");
        var values = new List<object?>(BcrdtBufferReader.InitialCapacity(count));
        for (int i = 0; i < count; i++) values.Add(BcrdtAnyCodec.Read(ref reader));
        return new BcrdtContentAny(values, true);
    }

    private void Detach()
    {
        if (_count < _values.Count / RetentionDivisor) MoveTo(_values.GetRange(_start, _count));
    }

    /// <summary>Moves the run to a list of its own, leaving the one it read from to its other readers.</summary>
    private void MoveTo(List<object?> values)
    {
        Release();
        _values = values;
        _start = 0;
        _readers = null;
    }

    /// <summary>Stops counting this run among the readers of its list, once it has been absorbed or reclaimed.</summary>
    /// <remarks>
    /// A run dropped without this - the part of an update a replica already had, say - only leaves its
    /// list looking shared, and the cost of that is one copy when a reader next grows.
    /// </remarks>
    private void Release()
    {
        if (_readers is not null) _readers.Count--;
    }

    private static object? ValueAt(List<object?> values, int start, int count, int index) =>
        (uint)index < (uint)count ? values[start + index] : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>How many runs read from one list.</summary>
    /// <remarks>
    /// Kept so that a run left alone on its list - its neighbour moved to a list of its own on the split -
    /// can go on growing it in place. Without the count, every value appended to such a run would copy it.
    /// </remarks>
    private sealed class Readers
    {
        public int Count = 1;
    }

    private sealed class Slice(List<object?> values, int start, int count) : IReadOnlyList<object?>
    {
        public object? this[int index] => ValueAt(values, start, count, index);

        public int Count => count;

        public IEnumerator<object?> GetEnumerator()
        {
            for (int i = 0; i < count; i++) yield return values[start + i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
