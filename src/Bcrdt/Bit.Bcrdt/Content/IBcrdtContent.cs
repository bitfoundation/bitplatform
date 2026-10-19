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

    /// <summary>Creates an independent copy, used when undo re-inserts deleted content.</summary>
    IBcrdtContent Copy();

    /// <summary>
    /// Splits the payload at <paramref name="offset"/>, keeping the left part and returning the right.
    /// </summary>
    IBcrdtContent Splice(int offset);

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
