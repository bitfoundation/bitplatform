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
    private readonly List<object?> _values;

    /// <summary>Creates a payload holding a single value.</summary>
    /// <exception cref="NotSupportedException">The value is outside the collaborative value model.</exception>
    /// <remarks>
    /// The value is stored in canonical form - see <see cref="BcrdtAnyCodec.Canonicalize(object?)"/> - so this
    /// replica holds exactly what its peers will decode, and later changes to the caller's object
    /// cannot alter the document behind the engine's back.
    /// </remarks>
    public BcrdtContentAny(object? value) => _values = [BcrdtAnyCodec.Canonicalize(value)];

    /// <summary>Creates a payload holding a run of values.</summary>
    /// <exception cref="NotSupportedException">A value is outside the collaborative value model.</exception>
    public BcrdtContentAny(IEnumerable<object?> values) => _values = [.. values.Select(BcrdtAnyCodec.Canonicalize)];

    private BcrdtContentAny(List<object?> values, bool _) => _values = values;

    /// <summary>Wraps values that have already been through <see cref="BcrdtAnyCodec.Canonicalize(object?)"/>.</summary>
    internal static BcrdtContentAny FromCanonical(List<object?> values) => new(values, true);

    /// <inheritdoc/>
    public int Length => _values.Count;

    /// <inheritdoc/>
    public bool Countable => true;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Any;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => _values;

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentAny([.. _values], true);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset)
    {
        var right = new BcrdtContentAny(_values.GetRange(offset, _values.Count - offset), true);
        _values.RemoveRange(offset, _values.Count - offset);
        return right;
    }

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right)
    {
        if (right is not BcrdtContentAny other) return false;
        _values.AddRange(other._values);
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
        int count = _values.Count - offset;
        writer.WriteVarUInt((ulong)count);
        for (int i = offset; i < _values.Count; i++)
        {
            BcrdtAnyCodec.Write(ref writer, _values[i]);
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
}
