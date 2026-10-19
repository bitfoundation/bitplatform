using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// Maps <c>Database.BeginTransaction</c> onto the engine's single ambient transaction. Saves made
/// inside that scope by this context join it rather than opening a nested one; the transaction is
/// opened in this manager's name so a save from another context on the same store can tell it is
/// not one it may join.
/// </summary>
internal sealed class BlazeDbTransactionManager : IDbContextTransactionManager, IDisposable
{
    private readonly IBlazeDbTableCache _tables;
    private readonly ICurrentDbContext _currentContext;
    private BlazeDbEfTransaction? _current;
    private bool _disposing;

    public BlazeDbTransactionManager(IBlazeDbTableCache tables, ICurrentDbContext currentContext)
    {
        _tables = tables;
        _currentContext = currentContext;
    }

    public IDbContextTransaction? CurrentTransaction => _current;

    public IDbContextTransaction BeginTransaction()
    {
        if (_current is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress on this context.");
        }

        var transaction = _tables.Database.BeginTransaction(owner: this);
        _current = new BlazeDbEfTransaction(transaction, OnCompleted);
        return _current;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IDbContextTransaction>(cancellationToken);
        }
        return Task.FromResult(BeginTransaction());
    }

    public void CommitTransaction() => CurrentOrThrow().Commit();

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
        CurrentOrThrow().CommitAsync(cancellationToken);

    public void RollbackTransaction() => CurrentOrThrow().Rollback();

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) =>
        CurrentOrThrow().RollbackAsync(cancellationToken);

    public void ResetState()
    {
        _current?.Dispose();
        _current = null;
    }

    public Task ResetStateAsync(CancellationToken cancellationToken = default)
    {
        ResetState();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposing the context has to end a transaction it left open, as closing a connection would:
    /// the engine allows one transaction at a time, and an abandoned one would swallow every later
    /// write on the store into a batch nobody can commit. The service scope disposes this with the
    /// context; the change tracker is gone by then, so the detaching a rollback normally does is
    /// skipped.
    /// </summary>
    public void Dispose()
    {
        _disposing = true;
        _current?.Dispose();
        _current = null;
    }

    private void OnCompleted(bool rolledBack)
    {
        _current = null;
        if (rolledBack && !_disposing)
        {
            ForgetSavedEntities();
        }
    }

    /// <summary>
    /// After a rollback the tracked entities no longer describe the database. Queries hand back the
    /// table's own objects, so a save inside the transaction mutated those objects in place; rolling
    /// back restores the table's rows to their earlier values, but the objects the tracker holds
    /// keep the values that were undone - and, for an updated row, are no longer the instance the
    /// table holds at all, so the next query for that key would bring a second object with the same
    /// key into the tracker and fail. Detaching what the tracker believed was saved leaves the next
    /// query to load the rows as the database actually has them. Entities only added and never saved
    /// are kept: they describe no row and are still pending.
    /// </summary>
    private void ForgetSavedEntities()
    {
        var tracker = _currentContext.Context.ChangeTracker;
        foreach (var entry in tracker.Entries().ToList())
        {
            if (entry.State != EntityState.Added)
            {
                entry.State = EntityState.Detached;
            }
        }
    }

    private BlazeDbEfTransaction CurrentOrThrow() =>
        _current ?? throw new InvalidOperationException("No transaction is in progress on this context.");
}
