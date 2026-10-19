namespace Bit.BlazeDb;

/// <summary>
/// A single-writer transaction. Writes are applied to memory immediately (so reads inside the
/// transaction see them) and recorded for undo. <see cref="Commit"/> makes the batch atomic in
/// the WAL; <see cref="Rollback"/> (or disposing without committing) reverts all writes.
/// While a transaction is active, all table writes on the database - from any call site -
/// become part of it (ambient transaction semantics).
/// </summary>
public sealed class BlazeDbTransaction : IDisposable
{
    private readonly BlazeDbDatabase _db;
    private readonly List<IBlazeDbTxnOp> _ops = [];
    private bool _completed;

    internal BlazeDbTransaction(BlazeDbDatabase db) => _db = db;

    internal IReadOnlyList<IBlazeDbTxnOp> Ops => _ops;

    internal void Record(IBlazeDbTxnOp op) => _ops.Add(op);

    /// <summary>
    /// A point the transaction can later be rolled back to with <see cref="RollbackTo"/>, undoing
    /// only the writes recorded after it. The EF Core provider takes one around each save that joins
    /// an open transaction, so a save that fails part-way leaves none of its writes behind.
    /// </summary>
    internal int Savepoint()
    {
        EnsureActive();
        return _ops.Count;
    }

    /// <summary>Undoes the writes recorded since <paramref name="savepoint"/>; the transaction stays open.</summary>
    internal void RollbackTo(int savepoint)
    {
        EnsureActive();
        _db.RollbackTransactionTo(this, savepoint);
    }

    /// <summary>Forgets the writes from <paramref name="savepoint"/> on, once they have been reverted.</summary>
    internal void Truncate(int savepoint) => _ops.RemoveRange(savepoint, _ops.Count - savepoint);

    /// <summary>
    /// Commits the batch. If the commit cannot be journaled, every write is undone and the exception
    /// propagates; either way the transaction is over, so disposing it afterwards does nothing.
    /// </summary>
    public void Commit()
    {
        EnsureActive();
        try
        {
            _db.CommitTransaction(this);
        }
        finally
        {
            _completed = true;
        }
    }

    public void Rollback()
    {
        EnsureActive();
        _db.RollbackTransaction(this);
        _completed = true;
    }

    public void Dispose()
    {
        if (!_completed)
        {
            Rollback();
        }
    }

    private void EnsureActive()
    {
        if (_completed)
        {
            throw new InvalidOperationException("BlazeDbTransaction has already been committed or rolled back.");
        }
    }
}
