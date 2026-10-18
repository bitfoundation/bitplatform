using System.Data;
using System.Data.Common;
using System.Globalization;

namespace Bit.Bsqlite.Data;

/// <summary>A transaction driven by explicit BEGIN/COMMIT/ROLLBACK statements on the transport.</summary>
/// <remarks>
/// <para>
/// <see cref="IsolationLevel.Serializable"/> (the default) begins with <c>BEGIN IMMEDIATE</c>, which
/// takes the write lock up front so a transaction that later writes cannot fail part-way with
/// <c>SQLITE_BUSY</c> - the same choice Microsoft.Data.Sqlite makes. Any other level begins
/// deferred. SQLite has no further isolation levels; the enum value is recorded, not interpreted.
/// </para>
/// <para>
/// Disposing without committing rolls back, as every ADO.NET provider does. The asynchronous
/// dispose does so on the transport; the synchronous one cannot wait, so it issues the rollback
/// and returns - the transport serializes requests, so it still lands before the next command.
/// </para>
/// <para>
/// A cancellation token is honoured only before a statement is sent. Once <c>BEGIN</c>,
/// <c>COMMIT</c> or <c>ROLLBACK</c> is on its way, the worker runs it whatever .NET stops waiting
/// for, so abandoning the wait would leave the engine in a transaction no object here owns - holding
/// the write lock against every other tab, and swallowing every later write into it.
/// </para>
/// </remarks>
public sealed class BsqliteTransaction : DbTransaction
{
    private readonly BsqliteConnection _connection;
    private int _savepointCounter;
    private bool _completed;

    internal BsqliteTransaction(BsqliteConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        IsolationLevel = isolationLevel == IsolationLevel.Unspecified
            ? IsolationLevel.Serializable
            : isolationLevel;
    }

    public override IsolationLevel IsolationLevel { get; }

    protected override DbConnection DbConnection => _connection;

    /// <summary>Savepoints are supported, so EF Core nests <c>SaveChanges</c> inside this safely.</summary>
    public override bool SupportsSavepoints => true;

    /// <summary>Whether the transaction has been committed or rolled back.</summary>
    public bool IsCompleted => _completed;

    internal async Task BeginAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Not cancellable once sent; see the remarks.
        await _connection.ExecuteInternalAsync(
            IsolationLevel == IsolationLevel.Serializable ? "BEGIN IMMEDIATE" : "BEGIN",
            CancellationToken.None).ConfigureAwait(false);
        _connection.DiscardPendingWrites();
    }

    public override void Commit()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(nameof(Commit), nameof(CommitAsync));

    public override void Rollback()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(nameof(Rollback), nameof(RollbackAsync));

    public override Task CommitAsync(CancellationToken cancellationToken = default)
        => CompleteAsync("COMMIT", cancellationToken);

    public override Task RollbackAsync(CancellationToken cancellationToken = default)
        => CompleteAsync("ROLLBACK", cancellationToken);

    public override void Save(string savepointName)
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(nameof(Save), nameof(SaveAsync));

    public override void Release(string savepointName)
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(nameof(Release), nameof(ReleaseAsync));

    public override void Rollback(string savepointName)
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(nameof(Rollback), nameof(RollbackAsync));

    public override Task SaveAsync(string savepointName, CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        return _connection.ExecuteInternalAsync($"SAVEPOINT {QuoteSavepoint(savepointName)}", cancellationToken);
    }

    public override Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        return _connection.ExecuteInternalAsync($"RELEASE SAVEPOINT {QuoteSavepoint(savepointName)}", cancellationToken);
    }

    public override Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default)
    {
        ThrowIfCompleted();
        return _connection.ExecuteInternalAsync($"ROLLBACK TO SAVEPOINT {QuoteSavepoint(savepointName)}", cancellationToken);
    }

    /// <summary>Creates a savepoint with a generated name and returns that name.</summary>
    public async Task<string> SaveAsync(CancellationToken cancellationToken = default)
    {
        var name = "bit_bsqlite_sp" + (++_savepointCounter).ToString(CultureInfo.InvariantCulture);
        await SaveAsync(name, cancellationToken).ConfigureAwait(false);
        return name;
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            try
            {
                await RollbackAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The transaction may already be gone - a failed COMMIT rolls back on the engine's
                // side - and disposal is already the unwinding path: the original reason for
                // leaving the block is the more useful error, so this one is not thrown over it.
                Detach();
                _connection.DiscardPendingWrites();
            }
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_completed)
        {
            // Cannot wait here; see the remarks. The request is queued in order behind whatever is
            // in flight, and the connection is detached immediately so a new BEGIN is allowed.
            Detach();
            _connection.DiscardPendingWrites();
            _ = RollbackQuietlyAsync();
        }

        base.Dispose(disposing);
    }

    private async Task RollbackQuietlyAsync()
    {
        try
        {
            await _connection.ExecuteInternalAsync("ROLLBACK", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Nothing can observe this failure; the engine rolls back on close regardless.
        }
    }

    private async Task CompleteAsync(string statement, CancellationToken cancellationToken)
    {
        ThrowIfCompleted();

        // Before anything changes, so a cancelled commit leaves a transaction the caller can still
        // roll back or dispose; after this point the statement is sent and runs, as the remarks say.
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _connection.ExecuteInternalAsync(statement, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Whether COMMIT succeeded or SQLite rolled it back on failure, this object is finished,
            // and nothing it wrote is reported as committed.
            Detach();
            _connection.DiscardPendingWrites();
            throw;
        }

        Detach();

        // Live queries hear about the transaction's writes only now, and only if it committed.
        if (statement == "COMMIT")
        {
            _connection.CommitPendingWrites();
        }
        else
        {
            _connection.DiscardPendingWrites();
        }
    }

    private void Detach()
    {
        _completed = true;
        if (ReferenceEquals(_connection.CurrentTransaction, this))
        {
            _connection.CurrentTransaction = null;
        }
    }

    private void ThrowIfCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException("The transaction has already been committed or rolled back.");
        }
    }

    private static string QuoteSavepoint(string savepointName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(savepointName);
        return "\"" + savepointName.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
