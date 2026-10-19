using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A run of text, length-counted in UTF-16 code units.
/// </summary>
/// <remarks>
/// <para>
/// This is the payload that makes collaborative text affordable. A paragraph typed in one sitting
/// is one block regardless of length; only concurrent edits inside it cause splits. A document with
/// a million characters typically holds a few thousand blocks rather than a million.
/// </para>
/// <para>
/// Lengths are UTF-16 code units because that is what .NET strings and browser selection APIs both
/// index by. Surrogate pairs are handled explicitly during splitting - see <see cref="Splice"/>.
/// </para>
/// </remarks>
public sealed class BcrdtContentString : IBcrdtContent
{
    private StringBuilder _text;

    /// <summary>Creates a text payload.</summary>
    public BcrdtContentString(string text) => _text = new StringBuilder(text);

    private BcrdtContentString(StringBuilder text) => _text = text;

    /// <summary>The text this block carries.</summary>
    public string Text => _text.ToString();

    /// <inheritdoc/>
    public int Length => _text.Length;

    /// <inheritdoc/>
    public bool Countable => true;

    /// <inheritdoc/>
    public BcrdtContentRef Ref => BcrdtContentRef.String;

    /// <inheritdoc/>
    public IReadOnlyList<object?> GetContent()
    {
        string text = Text;
        var result = new List<object?>(text.Length);
        foreach (char c in text) result.Add(c.ToString());
        return result;
    }

    /// <inheritdoc/>
    public IBcrdtContent Copy() => new BcrdtContentString(new StringBuilder(_text.ToString()));

    /// <inheritdoc/>
    /// <summary>
    /// Splits at an exact code-unit boundary, even between the halves of a surrogate pair.
    /// </summary>
    /// <remarks>
    /// The boundary cannot be nudged to keep a pair intact. An identifier addresses a logical unit,
    /// so a block of length n must split into blocks of exactly <c>offset</c> and
    /// <c>n - offset</c> units, or every clock beyond this point would mean something different from
    /// what its author intended.
    /// <para>
    /// Splitting a pair is therefore allowed, and each half is left holding a lone surrogate. That is
    /// safe because it is lossless: the halves recombine into the original character when the text is
    /// read, and the wire format encodes lone surrogates faithfully. Crucially, this also means the
    /// visible text does not depend on <i>whether</i> a particular replica happened to split a
    /// particular block - which it otherwise would, and which would break convergence outright.
    /// </para>
    /// </remarks>
    public IBcrdtContent Splice(int offset)
    {
        string full = _text.ToString();
        _text = new StringBuilder(full[..offset]);
        return new BcrdtContentString(new StringBuilder(full[offset..]));
    }

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right)
    {
        if (right is not BcrdtContentString other) return false;
        _text.Append(other._text);
        return true;
    }

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) { }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    public void Write(ref BcrdtBufferWriter writer, int offset) =>
        writer.WriteString(offset == 0 ? _text.ToString() : _text.ToString(offset, _text.Length - offset));

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentString Read(ref BcrdtBufferReader reader) => new(reader.ReadString());
}
