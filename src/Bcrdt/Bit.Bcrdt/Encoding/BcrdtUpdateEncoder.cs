namespace Bit.Bcrdt;

/// <summary>Turns document state and transactions into wire updates.</summary>
public static class BcrdtUpdateEncoder
{
    /// <summary>
    /// Encodes everything a document holds that a peer does not.
    /// </summary>
    /// <param name="doc">The document to encode.</param>
    /// <param name="remoteState">
    /// What the peer already has. Pass <see langword="null"/> for the whole document.
    /// </param>
    /// <remarks>
    /// This is the workhorse of synchronization. Given a peer's state vector - a few dozen bytes -
    /// it produces exactly the blocks that peer is missing and nothing else, so reconciling after a
    /// week offline costs one message proportional to what actually changed rather than to the size
    /// of the document.
    /// </remarks>
    public static byte[] EncodeStateAsUpdate(BcrdtDocument doc, BcrdtStateVector? remoteState = null)
    {
        ArgumentNullException.ThrowIfNull(doc);

        var writer = new BcrdtBufferWriter(1024);
        try
        {
            BcrdtUpdateCodec.WriteClientBlocks(ref writer, doc.Store, remoteState ?? new BcrdtStateVector());

            // The delete set is always sent whole - deletions are idempotent and the set is small
            // relative to the content - but deriving it is proportional to the document, so it comes
            // from the document's cache rather than being rebuilt for every peer that asks.
            doc.GetDeleteSet().Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Encodes exactly what one transaction changed.
    /// </summary>
    /// <remarks>
    /// The transaction's opening state vector doubles as "what everyone already had", so the delta
    /// falls out of the same code path that computes a peer delta. A single keystroke produces a
    /// message of a few bytes.
    /// </remarks>
    public static byte[] EncodeTransaction(BcrdtTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var writer = new BcrdtBufferWriter(256);
        try
        {
            BcrdtUpdateCodec.WriteClientBlocks(ref writer, transaction.Doc.Store, transaction.BeforeState);
            transaction.DeleteSet.Write(ref writer);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>An update carrying nothing, useful as a neutral element.</summary>
    public static byte[] Empty { get; } = CreateEmpty();

    private static byte[] CreateEmpty()
    {
        var writer = new BcrdtBufferWriter(4);
        try
        {
            writer.WriteVarUInt(0);
            writer.WriteVarUInt(0);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }
}
