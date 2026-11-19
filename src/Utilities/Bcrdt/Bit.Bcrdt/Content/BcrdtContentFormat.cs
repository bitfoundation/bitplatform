namespace Bit.Bcrdt;

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
    public bool CanMergeWith(IBcrdtContent right) => false;

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
