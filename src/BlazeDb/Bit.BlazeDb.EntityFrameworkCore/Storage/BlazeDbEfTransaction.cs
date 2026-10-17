using Microsoft.EntityFrameworkCore.Storage;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

internal sealed class BlazeDbEfTransaction : IDbContextTransaction
{
    private readonly BlazeDbTransaction _transaction;
    private readonly Action<bool> _onCompleted;
    private bool _completed;
    private bool _rolledBack;
    private bool _disposed;

    // onCompleted is called once, when the transaction is disposed, with whether it was rolled back.
    public BlazeDbEfTransaction(BlazeDbTransaction transaction, Action<bool> onCompleted)
    {
        _transaction = transaction;
        _onCompleted = onCompleted;
        TransactionId = Guid.NewGuid();
    }

    public Guid TransactionId { get; }

    public void Commit()
    {
        _transaction.Commit();
        _completed = true;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        Commit();
        return Task.CompletedTask;
    }

    public void Rollback()
    {
        _transaction.Rollback();
        _completed = true;
        _rolledBack = true;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        Rollback();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (!_completed)
        {
            // Disposing without committing is a rollback, as with every other provider.
            _transaction.Dispose();
            _completed = true;
            _rolledBack = true;
        }
        _onCompleted(_rolledBack);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
