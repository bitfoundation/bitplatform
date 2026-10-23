namespace Bit.Bink;

/// <summary>When and how <see cref="BinkAutosave"/> saves.</summary>
public sealed record BinkAutosaveOptions
{
    /// <summary>Saves two seconds after the last change, and at least every thirty seconds while changes keep coming.</summary>
    public static readonly BinkAutosaveOptions Default = new();

    /// <summary>
    /// How long the document has to stay unchanged before it is saved. Saving only once editing pauses keeps a long
    /// drag or a burst of strokes from serializing the document many times over.
    /// </summary>
    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>The longest a change waits while editing never pauses, so a long session is never far from saved.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether changes made by collaborators, and documents loaded into the same instance, are saved too. On by
    /// default: the local copy is what would be recovered, and it should include them.
    /// </summary>
    public bool IncludeRemoteChanges { get; init; } = true;

    /// <summary>Whether disposing saves changes that are still waiting. On by default.</summary>
    public bool SaveOnDispose { get; init; } = true;

    /// <summary>How the document is written.</summary>
    public BinkSerializerOptions SerializerOptions { get; init; } = BinkSerializerOptions.Default;

    /// <summary>The clock the delays are measured on. Replaced by tests.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}

/// <summary>
/// Saves a document to an <see cref="IBinkDocumentStore"/> by itself, shortly after it changes, so a reload, a closed
/// tab or a crash loses at most a few seconds of work. <see cref="RestoreAsync"/> brings the saved copy back.
/// </summary>
/// <remarks>
/// <para>
/// Create it on the thread that owns the document, which in Blazor is the component's: it captures the current
/// <see cref="SynchronizationContext"/> and serializes the document there, never on a timer thread, because a
/// document is not thread-safe. Without a synchronization context (a console application, a test) saves run on the
/// timer's thread, which is only safe when nothing else edits the document at the same moment.
/// </para>
/// <para>
/// Autosaving is a recovery copy, separate from the application's own saving: it does not change
/// <see cref="BinkDocument.IsDirty"/>. An application that saves for real somewhere else calls
/// <see cref="DiscardAsync"/> afterwards, so the next visit is not offered a copy older than what it saved.
/// Nothing is sent anywhere the store does not send it; <see cref="BinkBrowserDocumentStore"/> stays on the device.
/// </para>
/// </remarks>
public sealed class BinkAutosave : IAsyncDisposable
{
    private readonly BinkDocument _document;
    private readonly IBinkDocumentStore _store;
    private readonly BinkAutosaveOptions _options;
    private readonly SynchronizationContext? _context;
    private readonly ITimer _timer;
    private readonly object _gate = new();
    private long _savedRevision;
    private DateTimeOffset? _firstUnsaved;
    private Task _saving = Task.CompletedTask;
    private bool _again;
    private bool _disposed;

    /// <summary>Starts saving a document to a store under a key.</summary>
    /// <param name="document">The document to save.</param>
    /// <param name="store">Where to save it.</param>
    /// <param name="key">The key it is saved under, such as the document's id or <c>"whiteboard"</c>.</param>
    /// <param name="options">When and how to save.</param>
    public BinkAutosave(BinkDocument document, IBinkDocumentStore store, string key, BinkAutosaveOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrEmpty(key);
        _document = document;
        _store = store;
        Key = key;
        _options = options ?? BinkAutosaveOptions.Default;
        _context = SynchronizationContext.Current;
        _savedRevision = document.Revision;
        _timer = _options.TimeProvider.CreateTimer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        document.Changed += OnDocumentChanged;
    }

    /// <summary>The key the document is saved under.</summary>
    public string Key { get; }

    /// <summary>True when the document changed since it was last saved here.</summary>
    public bool HasUnsavedChanges => _document.Revision != _savedRevision;

    /// <summary>When the document was last saved here, or null when it has not been yet.</summary>
    public DateTimeOffset? LastSaved { get; private set; }

    /// <summary>Why the last save failed, or null when it succeeded. Cleared by the next successful save.</summary>
    public Exception? LastError { get; private set; }

    /// <summary>Raised on the document's thread after each successful save.</summary>
    public event EventHandler? Saved;

    /// <summary>
    /// Raised on the document's thread when a save failed, for example because the browser's storage is full. The
    /// changes stay unsaved and the next change, or <see cref="BinkAutosaveOptions.MaxDelay"/>, tries again.
    /// </summary>
    public event EventHandler<Exception>? SaveFailed;

    private TimeSpan MaxDelay => _options.MaxDelay > TimeSpan.Zero ? _options.MaxDelay : _options.Delay;

    /// <summary>
    /// Reads a document saved under a key, or returns null when there is none, or when what is there is not a
    /// readable document (in which case <paramref name="onError"/> hears why).
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The key.</param>
    /// <param name="options">How to read it; stored data is treated as untrusted like any other document.</param>
    /// <param name="onError">Told why a stored copy could not be read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<BinkDocument?> RestoreAsync(IBinkDocumentStore store, string key, BinkDeserializerOptions? options = null, Action<string>? onError = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrEmpty(key);
        byte[]? bytes = await store.LoadAsync(key, cancellationToken).ConfigureAwait(true);
        if (bytes is null || bytes.Length == 0) return null;
        if (BinkSerializer.TryDeserialize(bytes, out BinkDocument? document, out string? error, options)) return document;
        onError?.Invoke(error ?? "The saved drawing could not be read.");
        return null;
    }

    /// <summary>Saves now, whether or not anything changed, and waits for the store to finish.</summary>
    public async Task SaveNowAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        Task saving;
        lock (_gate)
        {
            // A save under way may have serialized the document before the latest change, so this one follows it
            // rather than standing in for it.
            _saving = saving = SaveAfterAsync(_saving, cancellationToken);
        }
        await saving.ConfigureAwait(true);
    }

    private async Task SaveAfterAsync(Task previous, CancellationToken cancellationToken)
    {
        try
        {
            await previous.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Another caller's cancelled save is no concern of this one.
        }
        await SaveCoreAsync(force: true, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Removes the saved copy and forgets the changes waiting to be saved. Call it once the application has saved
    /// the document for real, or the user chose to start over. Later changes are saved again as usual.
    /// </summary>
    public async Task DiscardAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        // A save already writing would put the copy back after the delete.
        await WaitForSaveAsync().ConfigureAwait(true);
        lock (_gate)
        {
            _savedRevision = _document.Revision;
            _firstUnsaved = null;
            _again = false;
        }
        await _store.DeleteAsync(Key, cancellationToken).ConfigureAwait(true);
    }

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        if (_disposed) return;
        if (!_options.IncludeRemoteChanges && e.Origin is BinkChangeOrigin.Remote or BinkChangeOrigin.Load) return;
        DateTimeOffset now = _options.TimeProvider.GetUtcNow();
        TimeSpan due;
        lock (_gate)
        {
            _firstUnsaved ??= now;
            // Saved after a quiet spell, but never later than the longest a change may wait.
            DateTimeOffset at = now + _options.Delay;
            DateTimeOffset latest = _firstUnsaved.Value + MaxDelay;
            if (at > latest) at = latest;
            due = at - now;
        }
        _timer.Change(due < TimeSpan.Zero ? TimeSpan.Zero : due, Timeout.InfiniteTimeSpan);
    }

    private void OnTimer(object? state)
    {
        if (_disposed) return;
        // The document is serialized on the thread that owns it.
        if (_context is null) _ = SaveAsync(force: false, CancellationToken.None);
        else _context.Post(static self => _ = ((BinkAutosave)self!).SaveAsync(force: false, CancellationToken.None), this);
    }

    private Task SaveAsync(bool force, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_saving.IsCompleted)
            {
                // One write at a time; the one under way is followed by another for what changed meanwhile.
                _again = true;
                return _saving;
            }
            _saving = SaveCoreAsync(force, cancellationToken);
            return _saving;
        }
    }

    private async Task SaveCoreAsync(bool force, CancellationToken cancellationToken)
    {
        // Yields first, so the work runs after SaveAsync has stored the task and released the lock.
        await Task.Yield();
        long revision = _document.Revision;
        try
        {
            // A timer save posted just before disposing can arrive after it, when nothing may be written any more.
            if (_disposed || (!force && revision == _savedRevision)) return;
            byte[] bytes = BinkSerializer.SerializeToUtf8Bytes(_document, _options.SerializerOptions);
            await _store.SaveAsync(Key, bytes, cancellationToken).ConfigureAwait(true);
            lock (_gate)
            {
                _savedRevision = revision;
                // What changed while the bytes were written is still waiting, from now on.
                _firstUnsaved = _document.Revision == revision ? null : _options.TimeProvider.GetUtcNow();
            }
            LastSaved = _options.TimeProvider.GetUtcNow();
            LastError = null;
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // A failing store must not take the application down; it is reported and retried.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LastError = ex;
            SaveFailed?.Invoke(this, ex);
            // Tried again after the longest wait, or sooner when the next change comes.
            if (!_disposed) _timer.Change(MaxDelay, Timeout.InfiniteTimeSpan);
            return;
        }
        finally
        {
            bool again;
            lock (_gate)
            {
                again = _again;
                _again = false;
            }
            if (again && !_disposed) _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Waits for a save under way. One a caller cancelled through <see cref="SaveNowAsync"/> is no concern of the waiter.</summary>
    private async Task WaitForSaveAsync()
    {
        try
        {
            await _saving.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>Stops saving; first saves what is still waiting when <see cref="BinkAutosaveOptions.SaveOnDispose"/> is on.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _document.Changed -= OnDocumentChanged;
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        try
        {
            await WaitForSaveAsync().ConfigureAwait(true);
            if (_options.SaveOnDispose && HasUnsavedChanges) await SaveAsync(force: false, CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            _disposed = true;
            _timer.Dispose();
        }
    }
}
