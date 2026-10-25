using System.Runtime.InteropServices;
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
/// <para>
/// The text is held as an immutable slice of a string - shared with the blocks it was split from -
/// followed by whatever has been appended since, which is how typing at the end of a block grows it.
/// That makes both of the operations editing does constantly cheap: appending is amortized constant
/// time, and splitting a block is constant time rather than a copy of it. Copying on every split made
/// deleting every other character of a long block cost time in the square of its length, which a peer
/// could provoke with one modest update.
/// </para>
/// </remarks>
public sealed class BcrdtContentString : IBcrdtContent
{
    /// <summary>
    /// A slice shorter than this fraction of the string it points into is copied out, so that a few
    /// characters left of a large paste do not keep the whole paste alive.
    /// </summary>
    private const int RetentionDivisor = 4;

    // An immutable slice, possibly of a string other blocks slice as well.
    private ReadOnlyMemory<char> _head;

    // What was appended since the block was last split; null when nothing was.
    private StringBuilder? _tail;

    /// <summary>Creates a text payload.</summary>
    public BcrdtContentString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _head = text.AsMemory();
    }

    private BcrdtContentString(ReadOnlyMemory<char> head) => _head = head;

    /// <summary>The text this block carries.</summary>
    /// <remarks>A fresh string on every read; to collect a run of blocks, use <see cref="AppendTo"/>.</remarks>
    public string Text
    {
        get
        {
            if (_tail is null) return _head.ToString();

            var builder = new StringBuilder(Length);
            AppendTo(builder);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Appends the text this block carries, copying straight from the block rather than through an
    /// intermediate string - reading a whole document then costs one copy of it, not two.
    /// </summary>
    internal void AppendTo(StringBuilder builder)
    {
        builder.Append(_head);
        if (_tail is not null) builder.Append(_tail);
    }

    /// <inheritdoc/>
    public int Length => _head.Length + (_tail?.Length ?? 0);

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
    public IBcrdtContent Copy() => new BcrdtContentString(Text);

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
    /// <para>
    /// Both halves are slices of the same string, so a split copies nothing - except text appended
    /// since the last split, which is settled into one string first, and a half small enough that
    /// keeping its whole string alive for it would be wasteful.
    /// </para>
    /// </remarks>
    public IBcrdtContent Splice(int offset)
    {
        Settle();
        ReadOnlyMemory<char> whole = _head;
        _head = Detach(whole[..offset]);
        return new BcrdtContentString(Detach(whole[offset..]));
    }

    /// <inheritdoc/>
    public bool TryMergeWith(IBcrdtContent right)
    {
        if (right is not BcrdtContentString other) return false;

        // The two halves of an earlier split, rejoined, are still one slice of one string.
        if (_tail is null && other._tail is null && TryJoin(_head, other._head, out ReadOnlyMemory<char> joined))
        {
            _head = joined;
            return true;
        }

        _tail ??= new StringBuilder();
        _tail.Append(other._head);
        if (other._tail is not null) _tail.Append(other._tail);
        return true;
    }

    /// <inheritdoc/>
    public void Integrate(BcrdtTransaction transaction, BcrdtItem item) { }

    /// <inheritdoc/>
    public void Delete(BcrdtTransaction transaction) { }

    /// <inheritdoc/>
    public void Collect(BcrdtStructStore store) { }

    /// <inheritdoc/>
    /// <remarks>
    /// Only the units from <paramref name="offset"/> on are copied. A keystroke at the end of a long block
    /// is merged into it before the transaction's update is encoded, and the update then writes just that
    /// keystroke from the block - copying the whole block to do so would make typing cost time in the
    /// square of the block's length.
    /// </remarks>
    public void Write(ref BcrdtBufferWriter writer, int offset)
    {
        if (_tail is null)
        {
            writer.WriteString(_head[offset..].ToString());
            return;
        }

        if (offset >= _head.Length)
        {
            int start = offset - _head.Length;
            writer.WriteString(_tail.ToString(start, _tail.Length - start));
            return;
        }

        writer.WriteString(string.Concat(_head.Span[offset..], _tail.ToString()));
    }

    /// <summary>Reads a payload written by <see cref="Write"/>.</summary>
    public static BcrdtContentString Read(ref BcrdtBufferReader reader) => new(reader.ReadString());

    /// <summary>Folds appended text into the immutable slice, so the block can be sliced.</summary>
    private void Settle()
    {
        if (_tail is null) return;
        _head = Text.AsMemory();
        _tail = null;
    }

    /// <summary>A slice that no longer pins a string much larger than itself.</summary>
    private static ReadOnlyMemory<char> Detach(ReadOnlyMemory<char> slice)
    {
        if (MemoryMarshal.TryGetString(slice, out string? backing, out _, out _) &&
            slice.Length < backing.Length / RetentionDivisor)
        {
            return slice.ToString().AsMemory();
        }
        return slice;
    }

    /// <summary>Joins two slices that sit side by side in the same string.</summary>
    private static bool TryJoin(ReadOnlyMemory<char> left, ReadOnlyMemory<char> right, out ReadOnlyMemory<char> joined)
    {
        if (MemoryMarshal.TryGetString(left, out string? leftString, out int leftStart, out int leftLength) &&
            MemoryMarshal.TryGetString(right, out string? rightString, out int rightStart, out _) &&
            ReferenceEquals(leftString, rightString) &&
            leftStart + leftLength == rightStart)
        {
            joined = leftString.AsMemory(leftStart, leftLength + right.Length);
            return true;
        }

        joined = default;
        return false;
    }
}
