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
