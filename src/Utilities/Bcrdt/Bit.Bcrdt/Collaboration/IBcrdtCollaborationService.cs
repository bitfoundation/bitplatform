namespace Bit.Bcrdt;

/// <summary>Opens and tracks collaborative documents.</summary>
public interface IBcrdtCollaborationService : IAsyncDisposable
{
    /// <summary>Opens a document, or returns the already-open instance.</summary>
    /// <remarks>
    /// Every successful call takes a reference to the document; pair it with one call to
    /// <see cref="CloseAsync"/>. The document stays open while any reference is held. A call that
    /// fails or is cancelled holds no reference. Options apply only to the call that actually opens
    /// the document.
    /// </remarks>
    ValueTask<BcrdtCollaborativeDocument> OpenAsync(
        string documentId,
        BcrdtOpenDocumentOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a reference taken by <see cref="OpenAsync"/>. The document is closed, and everything
    /// it owns released, when the last reference goes.
    /// </summary>
    /// <remarks>
    /// References are counted per identifier, not per caller: a close without a matching open releases
    /// somebody else's reference, and can close the document under them. Pair them exactly - or let
    /// <c>BcrdtCollaborationScope</c> do it.
    /// </remarks>
    ValueTask CloseAsync(string documentId);

    /// <summary>The documents currently open.</summary>
    IReadOnlyCollection<BcrdtCollaborativeDocument> OpenDocuments { get; }
}
