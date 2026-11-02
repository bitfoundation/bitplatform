namespace Bit.Bcrdt;

/// <summary>Wire discriminators for block payloads.</summary>
/// <remarks>
/// These values are persisted. Never renumber an existing entry; new payload kinds take the next
/// free number, and readers reject numbers they do not recognise rather than guessing.
/// </remarks>
public enum BcrdtContentRef : byte
{
    /// <summary>A garbage-collected clock range. Not a content kind - a struct kind.</summary>
    GarbageCollected = 0,
    /// <summary>A deleted range whose payload has been dropped.</summary>
    Deleted = 1,
    /// <summary>An opaque byte array.</summary>
    Binary = 2,
    /// <summary>A run of text.</summary>
    String = 3,
    /// <summary>A non-text object embedded inside text.</summary>
    Embed = 4,
    /// <summary>A rich-text mark boundary. Occupies clock space but no index space.</summary>
    Format = 5,
    /// <summary>A nested collaborative type.</summary>
    Type = 6,
    /// <summary>One or more values from the collaborative value model.</summary>
    Any = 7,
    /// <summary>Reserved for nested sub-documents.</summary>
    Doc = 8,
    /// <summary>A deliberately omitted range. Not a content kind - a struct kind.</summary>
    Skip = 9,
}

/// <summary>
/// The payload carried by a block.
/// </summary>
/// <remarks>
/// <para>
/// Content is a strategy object so that new payload kinds can be introduced without touching
/// integration, encoding, garbage collection or undo. Everything the engine needs to know about a
/// payload - how long it is, whether it occupies an index, how to split it, whether it can absorb
/// its neighbour - is expressed here.
/// </para>
/// <para>
/// <see cref="Splice"/> and <see cref="TryMergeWith"/> are what make run-length encoding survive
/// editing. Typing a paragraph produces one block; inserting into its middle splits it in two;
/// deleting the insertion lets the halves merge back. Without both halves of that cycle, a document
/// would accumulate one block per keystroke forever.
/// </para>
/// </remarks>
public interface IBcrdtContent
{
    /// <summary>How many logical units this payload occupies in its parent's clock space.</summary>
    int Length { get; }

    /// <summary>
    /// Whether this payload occupies a position in the parent's index space.
    /// </summary>
    /// <remarks>
    /// Rich-text marks are the reason this is not simply <c>Length &gt; 0</c>: a mark is addressable
    /// by identifier and consumes clock space, but must not shift the index of the characters
    /// around it, or applying a mark would move every subsequent cursor.
    /// </remarks>
    bool Countable { get; }

    /// <summary>The wire discriminator for this payload.</summary>
    BcrdtContentRef Ref { get; }

    /// <summary>The individual values this payload represents, one per logical unit.</summary>
    IReadOnlyList<object?> GetContent();

    /// <summary>The value of one logical unit, without building the others.</summary>
    /// <remarks>
    /// What reading one element of an array, or a map entry, costs. <see cref="GetContent"/> may build
    /// every value of the payload to hand out one of them - a run of a million characters is a million
    /// strings - so reading such a run by index would cost time in the square of its length.
    /// </remarks>
    /// <returns>The unit's value, or <see langword="null"/> for a payload that carries none, such as a deleted range.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the payload.</exception>
    object? GetValue(int index)
    {
        IReadOnlyList<object?> content = GetContent();
        return content.Count == 0 ? null : content[index];
    }

    /// <summary>Creates an independent copy, used when undo re-inserts deleted content.</summary>
    IBcrdtContent Copy();

    /// <summary>
    /// Splits the payload at <paramref name="offset"/>, keeping the left part and returning the right.
    /// </summary>
    IBcrdtContent Splice(int offset);

    /// <summary>
    /// Whether <see cref="TryMergeWith"/> would absorb <paramref name="right"/>. Changes nothing.
    /// </summary>
    /// <remarks>
    /// Lets transaction cleanup find every block a run of them would fold into before folding any, so it
    /// can fold them from the left: each merge then appends one neighbour, rather than copying everything
    /// gathered so far into the next block over. The default - two payloads of the same kind - is the
    /// condition every payload that merges at all puts first; one that never merges says so.
    /// </remarks>
    bool CanMergeWith(IBcrdtContent right) => GetType() == right.GetType();

    /// <summary>
    /// Absorbs <paramref name="right"/> if the two payloads can be represented as one.
    /// </summary>
    /// <returns><see langword="true"/> if the merge happened and <paramref name="right"/> is now redundant.</returns>
    bool TryMergeWith(IBcrdtContent right);

    /// <summary>Called once the owning block has been placed in the document.</summary>
    void Integrate(BcrdtTransaction transaction, BcrdtItem item);

    /// <summary>Called when the owning block is deleted.</summary>
    void Delete(BcrdtTransaction transaction);

    /// <summary>Called when the owning block's payload is being reclaimed.</summary>
    void Collect(BcrdtStructStore store);

    /// <summary>Writes the payload, skipping the first <paramref name="offset"/> logical units.</summary>
    void Write(ref BcrdtBufferWriter writer, int offset);
}
