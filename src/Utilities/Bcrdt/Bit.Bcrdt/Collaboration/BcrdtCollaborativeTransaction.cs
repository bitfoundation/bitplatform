namespace Bit.Bcrdt;

/// <summary>
/// A batch of changes that becomes one update when committed.
/// </summary>
/// <remarks>
/// <para>
/// There is no rollback. Every change made inside the batch is already part of the document's
/// history by the time the batch closes; what the batch controls is when the change is
/// <i>published</i> - as one update and one round of events rather than many.
/// </para>
/// <para>
/// Keep a batch synchronous: anything that runs while it is open joins it, including updates that
/// arrive from peers during an <c>await</c>. See <see cref="BcrdtCollaborativeDocument.BeginTransaction"/>.
/// </para>
/// </remarks>
public sealed class BcrdtCollaborativeTransaction : IDisposable, IAsyncDisposable
{
    private readonly BcrdtCollaborativeDocument _document;
    private readonly IDisposable _scope;
    private readonly bool _nested;
    private bool _committed;

    internal BcrdtCollaborativeTransaction(BcrdtCollaborativeDocument document, IDisposable scope, bool openedInsideAnother)
    {
        _document = document;
        _scope = scope;
        _nested = openedInsideAnother;
    }

    /// <summary>Whether this batch joined a transaction that was already open, and so publishes with it.</summary>
    public bool IsNested => _nested;

    /// <summary>Publishes the batch and waits for it to reach storage.</summary>
    /// <remarks>
    /// For a nested batch nothing is published until the enclosing transaction commits, so there is
    /// nothing yet to wait for; this then returns once anything already queued has been stored.
    /// </remarks>
    public async ValueTask CommitAsync()
    {
        Commit();
        await _document.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>Publishes the batch without waiting for storage.</summary>
    public void Commit()
    {
        if (_committed) return;
        _committed = true;
        _scope.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose() => Commit();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => CommitAsync();
}
