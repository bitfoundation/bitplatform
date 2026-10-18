namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Holds live-query refreshes back while the context's <c>SaveChanges</c> is in progress.
/// </summary>
/// <remarks>
/// The connection raises <c>TablesChanged</c> as soon as the transport has run the write, which is
/// before EF has consumed the insert result and before it has accepted the tracked changes. On the
/// browser main thread that ordering is harmless: the refresh is queued behind the rest of the save.
/// On a thread-pool host - the in-process transport in a test, or Blazor Server - the refresh runs
/// on another thread, and re-executing the query materialises the just-inserted row into a change
/// tracker that is still assigning it its store-generated key. That surfaced as
/// "another instance with the same key value is already being tracked" thrown out of the
/// <em>write</em>. Waiting for <see cref="DbContext.SavedChanges"/> makes the read follow the write
/// on every host.
/// </remarks>
internal sealed class BsqliteSaveChangesGate : IDisposable
{
    private readonly DbContext _context;
    private readonly Lock _lock = new();
    private TaskCompletionSource? _saving;
    private bool _disposed;

    public BsqliteSaveChangesGate(DbContext context)
    {
        _context = context;
        _context.SavingChanges += OnSavingChanges;
        _context.SavedChanges += OnSaveFinished;
        _context.SaveChangesFailed += OnSaveFinished;
    }

    /// <summary>Completes once no <c>SaveChanges</c> is running on the context.</summary>
    public Task WaitForIdleAsync(CancellationToken cancellationToken)
    {
        Task? pending;
        lock (_lock)
        {
            pending = _saving?.Task;
        }

        return pending is null ? Task.CompletedTask : pending.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        TaskCompletionSource? saving;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            saving = _saving;
            _saving = null;
        }

        _context.SavingChanges -= OnSavingChanges;
        _context.SavedChanges -= OnSaveFinished;
        _context.SaveChangesFailed -= OnSaveFinished;

        // Nothing will ever signal a refresh that is still waiting; let it run rather than hang.
        saving?.TrySetResult();
    }

    private void OnSavingChanges(object? sender, SavingChangesEventArgs e)
    {
        lock (_lock)
        {
            _saving ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    private void OnSaveFinished(object? sender, EventArgs e)
    {
        TaskCompletionSource? saving;
        lock (_lock)
        {
            saving = _saving;
            _saving = null;
        }

        saving?.TrySetResult();
    }
}
