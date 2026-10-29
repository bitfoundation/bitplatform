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

    private BcrdtContentAny(List<object?> values, bool _) : this(values, 0, values.Count) { }

    private BcrdtContentAny(List<object?> values, int start, int count)
    {
        _values = values;
        _start = start;
        _count = count;
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
    public IReadOnlyList<object?> GetContent() =>
        _start == 0 && _count == _values.Count ? _values : new Slice(_values, _start, _count);

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentAny(_values.GetRange(_start, _count), true);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset)
    {
        var right = new BcrdtContentAny(_values, _start + offset, _count - offset);
        _count = offset;
        Detach();
        right.Detach();
        return right;
    }

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right)
    {
        if (right is not BcrdtContentAny other) return false;

        if (ReferenceEquals(_values, other._values) && _start + _count == other._start)
        {
            _count += other._count;
            return true;
        }

        if (_start + _count != _values.Count)
        {
            _values = _values.GetRange(_start, _count);
            _start = 0;
        }

        for (int i = 0; i < other._count; i++) _values.Add(other._values[other._start + i]);
        _count += other._count;
        return true;
    }

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) { }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

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
        if (_count < _values.Count / RetentionDivisor)
        {
            _values = _values.GetRange(_start, _count);
            _start = 0;
        }
    }

    private sealed class Slice(List<object?> values, int start, int count) : IReadOnlyList<object?>
    {
        public object? this[int index] =>
            (uint)index < (uint)count ? values[start + index] : throw new ArgumentOutOfRangeException(nameof(index));

        public int Count => count;

        public IEnumerator<object?> GetEnumerator()
        {
            for (int i = 0; i < count; i++) yield return values[start + i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
