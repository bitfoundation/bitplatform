namespace Bit.Bcrdt;

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
    public bool CanMergeWith(IBcrdtContent right) => false;

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
