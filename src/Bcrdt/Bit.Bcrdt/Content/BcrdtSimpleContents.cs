namespace Bit.Bcrdt;

/// <summary>
/// A range whose payload has been reclaimed but whose identity must be preserved.
/// </summary>
/// <remarks>
/// When garbage collection reclaims deleted content it cannot simply remove the block: concurrent
/// operations still in flight may reference its identifier as an origin. This payload keeps the
/// clock range alive at a cost of a few bytes while releasing the actual data.
/// </remarks>
/// <param name="length">The number of logical units the reclaimed range covered.</param>
public sealed class BcrdtContentDeleted(int length) : IBcrdtContent
{
    /// <inheritdoc/>
    public int Length { get; private set; } = length;

    /// <inheritdoc/>
    public bool Countable => false;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Deleted;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => [];

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentDeleted(Length);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset)
    {
        var right = new BcrdtContentDeleted(Length - offset);
        Length = offset;
        return right;
    }

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right)
    {
        if (right is not BcrdtContentDeleted other) return false;
        Length += other.Length;
        return true;
    }

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item)
    {
        transaction.DeleteSet.Add(item.Id.Client, item.Id.Clock, Length);
        item.MarkDeleted();
    }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset) => writer.WriteVarUInt((ulong)(Length - offset));

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentDeleted Read(ref BcrdtBufferReader reader)
    {
        int length = reader.ReadVarUInt32();
        if (length <= 0) throw new BcrdtFormatException("Deleted range must have positive length.");
        return new BcrdtContentDeleted(length);
    }
}

/// <summary>An opaque byte array stored as a single collaborative unit.</summary>
/// <param name="data">The bytes to store. Treated as immutable.</param>
public sealed class BcrdtContentBinary(byte[] data) : IBcrdtContent
{
    /// <summary>The stored bytes.</summary>
    public byte[] Data { get; } = data;

    /// <inheritdoc/>
    public int Length => 1;

    /// <inheritdoc/>
    public bool Countable => true;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Binary;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => [Data];

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentBinary(Data);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset) => throw new InvalidOperationException("Binary content is indivisible.");

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right) => false;

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) { }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset) => writer.WriteBytes(Data);

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentBinary Read(ref BcrdtBufferReader reader) => new(reader.ReadBytes());
}

/// <summary>
/// A non-text object embedded in a text sequence - an image, a mention, a formula.
/// </summary>
/// <remarks>
/// <para>
/// An embed occupies exactly one index position, so a cursor can sit either side of it and a
/// selection can include it, but its interior is opaque to the text algorithms.
/// </para>
/// <para>
/// An embed always holds a value. <see cref="BcrdtText.InsertEmbed"/> refuses <see langword="null"/>,
/// and so does the decoder: a null embed would occupy a position with nothing for a delta to insert,
/// so every projection of the text would count one position fewer than the document holds.
/// </para>
/// </remarks>
public sealed class BcrdtContentEmbed : IBcrdtContent
{
    /// <summary>Creates an embed.</summary>
    /// <param name="value">The embedded value, from the collaborative value model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The value is outside the collaborative value model.</exception>
    public BcrdtContentEmbed(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = BcrdtAnyCodec.Canonicalize(value)!;
    }

    private BcrdtContentEmbed(object canonicalValue, bool _) => Value = canonicalValue;

    /// <summary>The embedded value.</summary>
    public object Value { get; }

    /// <inheritdoc/>
    public int Length => 1;

    /// <inheritdoc/>
    public bool Countable => true;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Embed;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => [Value];

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentEmbed(Value, true);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset) => throw new InvalidOperationException("Embedded content is indivisible.");

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right) => false;

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) { }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset) => BcrdtAnyCodec.Write(ref writer, Value);

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    /// <exception cref="BcrdtFormatException">The payload is malformed, or embeds <see langword="null"/>.</exception>
    public static BcrdtContentEmbed Read(ref BcrdtBufferReader reader) =>
        new(BcrdtAnyCodec.Read(ref reader) ?? throw new BcrdtFormatException("An embed carries no value."), true);
}

/// <summary>
/// A rich-text mark boundary: "from here on, this attribute has this value".
/// </summary>
/// <remarks>
/// <para>
/// Marks are stored inline in the text sequence rather than as ranges over character positions.
/// This is what makes formatting survive concurrent editing: a mark is anchored between two
/// characters by the same YATA rules as the characters themselves, so text inserted at the boundary
/// lands on the intended side of the mark instead of drifting as indices shift.
/// </para>
/// <para>
/// A mark occupies one unit of clock space - it needs an identifier like anything else - but is not
/// countable, so applying formatting never shifts a single character index or cursor position.
/// A <see langword="null"/> <see cref="Value"/> removes the attribute.
/// </para>
/// </remarks>
/// <param name="key">The attribute name, for example <c>bold</c>.</param>
/// <param name="value">The attribute value, or <see langword="null"/> to clear it.</param>
public sealed class BcrdtContentFormat(string key, object? value) : IBcrdtContent
{
    /// <summary>The attribute name.</summary>
    public string Key { get; } = key;

    /// <summary>The attribute value, or <see langword="null"/> to clear the attribute.</summary>
    public object? Value { get; } = BcrdtAnyCodec.Canonicalize(value);

    /// <inheritdoc/>
    public int Length => 1;

    /// <inheritdoc/>
    public bool Countable => false;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.Format;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent() => [];

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentFormat(Key, Value);

    /// <inheritdoc/>
    public IBcrdtContent Splice(int offset) => throw new InvalidOperationException("A format marker is indivisible.");

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right) => false;

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item)
    {
        if (item.Parent is BcrdtAbstractType parent)
        {
            // Search markers cache index positions, and a mark changes what a scan from a marker
            // will encounter. Also flag the type so the text reader knows to look for attributes at
            // all, which keeps plain-text documents on the fast path.
            parent.InvalidateSearchMarkers();
            parent.HasFormatting = true;
        }
    }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset)
    {
        writer.WriteString(Key);
        BcrdtAnyCodec.Write(ref writer, Value);
    }

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentFormat Read(ref BcrdtBufferReader reader)
    {
        string key = reader.ReadString();
        object? value = BcrdtAnyCodec.Read(ref reader);
        return new BcrdtContentFormat(key, value);
    }
}
