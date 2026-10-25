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

    // Savepoints still valid, oldest first. Rolling back to one forgets those taken after it, and
    // any rollback below a savepoint's position forgets that savepoint too.
    private readonly List<BlazeDbSavepoint> _savepoints = [];

    // Where each open atomic unit (RunInTransaction, an EF Core save) began, outermost first. Unlike a
    // savepoint, a unit's mark is never forgotten: a rollback below it inside the unit lowers it to the
    // point the rollback reached, because every write recorded above that point was made inside the
    // unit and has to go if the unit fails.
    private readonly List<int> _unitMarks = [];
    private long _nextSavepointId;
    private bool _completed;
    private bool _endedByDispose;

    internal BlazeDbTransaction(BlazeDbDatabase db) => _db = db;

    internal IReadOnlyList<IBlazeDbTxnOp> Ops => _ops;

    internal void Record(IBlazeDbTxnOp op) => _ops.Add(op);

    /// <summary>
    /// Opens an atomic unit inside this transaction and returns its handle: <see cref="RollbackUnit"/>
    /// undoes every write made since, <see cref="EndUnit"/> keeps them. Units nest strictly - each is ended
    /// or rolled back before the one around it, which their one caller - <see cref="BlazeDbDatabase.RunInTransactionCore"/>,
    /// behind <see cref="BlazeDbDatabase.RunInTransaction(Action)"/> and the EF Core provider's saves - ensures by
    /// holding the write lock for a unit's whole life. A unit that fails part-way leaves none of its writes behind.
    /// </summary>
    internal int BeginUnit()
    {
        EnsureActive();
        _unitMarks.Add(_ops.Count);
        return _unitMarks.Count - 1;
    }

    /// <summary>Closes the unit, keeping its writes as part of the transaction.</summary>
    internal void EndUnit(int unit) => _unitMarks.RemoveRange(unit, _unitMarks.Count - unit);

    /// <summary>Undoes the unit's writes and closes it; the transaction stays open.</summary>
    internal void RollbackUnit(int unit)
    {
        EnsureActive();
        var position = _unitMarks[unit];
        EndUnit(unit);
        _db.RollbackTransactionTo(this, position);
    }

    // The two below roll back for a unit whose body threw, and keep that exception the one its caller sees.
    // Should undoing a write fail as well, it is not lost: the database has recorded it - with storage it
    // refuses writes from then on and names it as the cause - and a transaction a unit could not be taken out
    // of refuses to commit (see UndoFailure). Reporting it in place of the body's exception would only hide
    // what went wrong first: a validation error turned into a NullReferenceException from an index selector.

    /// <summary>Undoes a failed unit's writes, keeping the body's exception the one that propagates.</summary>
    internal void RollbackUnitAfterFailure(int unit)
    {
        try
        {
            RollbackUnit(unit);
        }
        catch
        {
            // Recorded by the database; the body's exception is the one to report.
        }
    }

    /// <summary>Rolls the transaction back after its body threw, keeping that exception the one that propagates.</summary>
    internal void RollbackAfterFailure()
    {
        try
        {
            Dispose();
        }
        catch
        {
            // Recorded by the database; the body's exception is the one to report.
        }
    }

    /// <summary>
    /// Set when undoing part of the transaction - a unit or a savepoint - failed. It then still holds writes
    /// that were meant to be gone, so it can only be rolled back: a commit is refused and rolls it back.
    /// </summary>
    internal Exception? UndoFailure { get; private set; }

    internal void MarkUndoFailed(Exception cause) => UndoFailure ??= cause;

    /// <summary>Forgets the writes from <paramref name="position"/> on, once they have been reverted.</summary>
    internal void Truncate(int position)
    {
        _ops.RemoveRange(position, _ops.Count - position);
        while (_savepoints.Count > 0 && _savepoints[^1].Position > position)
        {
            _savepoints.RemoveAt(_savepoints.Count - 1);
        }
        for (var i = 0; i < _unitMarks.Count; i++)
        {
            _unitMarks[i] = Math.Min(_unitMarks[i], position);
        }
    }

    /// <summary>
    /// Marks the current point in the transaction, so <see cref="RollbackTo(BlazeDbSavepoint)"/> can
    /// later undo the writes made after it while keeping the ones before - the way SQL's
    /// <c>SAVEPOINT</c> nests a unit of work inside a transaction. Savepoints cost nothing until
    /// they are rolled back to, and need no releasing.
    /// </summary>
    public BlazeDbSavepoint CreateSavepoint()
    {
        EnsureActive();
        var savepoint = new BlazeDbSavepoint(this, ++_nextSavepointId, _ops.Count);
        _savepoints.Add(savepoint);
        return savepoint;
    }

    /// <summary>
    /// Undoes every write made since <paramref name="savepoint"/> was created, rows and index entries
    /// alike; the transaction stays open, and so does the savepoint, which can be rolled back to
    /// again. Savepoints created after it are discarded.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The savepoint belongs to another transaction, or an earlier rollback went past it.
    /// </exception>
    public void RollbackTo(BlazeDbSavepoint savepoint)
    {
        EnsureActive();
        if (!ReferenceEquals(savepoint.Transaction, this))
        {
            throw new InvalidOperationException("The savepoint was created by a different transaction.");
        }
        var index = _savepoints.FindIndex(s => s.Id == savepoint.Id);
        if (index < 0)
        {
            throw new InvalidOperationException(
                "The savepoint is no longer valid: a rollback to an earlier point already undid the writes it marked.");
        }
        _savepoints.RemoveRange(index + 1, _savepoints.Count - index - 1);
        _db.RollbackTransactionTo(this, savepoint.Position);
    }

    /// <summary>
    /// Commits the batch. If the commit cannot be journaled - or undoing part of the transaction earlier
    /// (back to a savepoint, or a failed unit) could not take a write back, so the batch still holds writes
    /// that were meant to be gone - every write is undone and the exception propagates; either way the
    /// transaction is over, so disposing it afterwards does nothing.
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
        _db.DeliverChanges();
    }

    public void Rollback()
    {
        EnsureActive();
        try
        {
            _db.RollbackTransaction(this);
        }
        finally
        {
            _completed = true;
        }
    }

    public void Dispose()
    {
        if (!_completed)
        {
            Rollback();
        }
    }

    /// <summary>The database rolled this transaction back because it was disposed with it open.</summary>
    internal void MarkCompleted()
    {
        _completed = true;
        _endedByDispose = true;
    }

    private void EnsureActive()
    {
        if (_endedByDispose)
        {
            throw new ObjectDisposedException(nameof(BlazeDbDatabase),
                "The database was disposed while this transaction was open, so the transaction was rolled back.");
        }
        if (_completed)
        {
            throw new InvalidOperationException("BlazeDbTransaction has already been committed or rolled back.");
        }
    }
}
