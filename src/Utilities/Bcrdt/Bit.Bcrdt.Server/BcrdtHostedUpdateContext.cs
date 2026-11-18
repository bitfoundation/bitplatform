namespace Bit.Bcrdt;

/// <summary>A change a client is about to make to a hosted document, offered for validation.</summary>
/// <remarks>Valid only for the duration of the callback it is passed to.</remarks>
public sealed class BcrdtHostedUpdateContext
{
    internal BcrdtHostedUpdateContext(string documentId, IBcrdtHostConnection connection, BcrdtDocument document, ReadOnlyMemory<byte> update)
    {
        DocumentId = documentId;
        Connection = connection;
        Document = document;
        Update = update;
    }

    /// <summary>The document the change is for.</summary>
    public string DocumentId { get; }

    /// <summary>The connection that sent it.</summary>
    public IBcrdtHostConnection Connection { get; }

    /// <summary>
    /// The hosted replica as it stands before the change. Read it; do not change it - the callback runs
    /// under the document's lock and a change made here would be broadcast as the server's own.
    /// </summary>
    public BcrdtDocument Document { get; }

    /// <summary>
    /// The update, in the engine's wire format. <see cref="BcrdtUpdateOperations"/> decodes it without a
    /// document - its state vector, blocks and deletions - and applying it to a copy of
    /// <see cref="Document"/> shows what it would do.
    /// </summary>
    public ReadOnlyMemory<byte> Update { get; }
}
