namespace Bit.Bcrdt;

/// <summary>
/// Ties a WebAssembly application's open documents to the browser's page and network lifecycle, through
/// Bit.Butil.
/// </summary>
/// <remarks>
/// <para>
/// It reconnects documents the moment the browser is back online instead of when their backoff happens to
/// expire, waits for their changes to reach storage when the page is hidden, withdraws presence while the page is
/// unloaded or cached, and can ask for persistent storage. Each is a setting of
/// <see cref="BcrdtBrowserLifecycleOptions"/>.
/// </para>
/// <para>
/// It acts on <see cref="IBcrdtCollaborationService.OpenDocuments"/> as they are when the browser reports
/// something, so a document opened after <see cref="StartAsync"/> is covered too. Start it once the application
/// runs in the browser - in a WebAssembly <c>Program.cs</c>, before <c>RunAsync</c>. Anywhere else - prerendering,
/// Blazor Server, Hybrid, a test host - <see cref="StartAsync"/> does nothing: the documents there live in a .NET
/// process a browser tab's lifecycle does not reach.
/// </para>
/// <para>
/// Bit.Butil reaches the browser through its script, so the page needs <c>_content/Bit.Butil/bit-butil.js</c> (or
/// Butil's lazy scripts), as for any Butil service.
/// </para>
/// </remarks>
public sealed class BcrdtBrowserLifecycle : IAsyncDisposable
{
    private readonly IBcrdtCollaborationService _collaboration;
    private readonly IBcrdtBrowser _browser;
    private readonly BcrdtBrowserLifecycleOptions _options;
    private readonly object _gate = new();
    private IAsyncDisposable? _subscription;
    private Task _flush = Task.CompletedTask;
    private Task? _queuedFlush;
    private bool _starting;
    private bool _disposed;

    /// <summary>Creates a lifecycle for the documents of a collaboration service.</summary>
    /// <param name="collaboration">The service whose open documents are kept in step with the page.</param>
    /// <param name="window">Butil's window, for the network and page events.</param>
    /// <param name="document">Butil's document, for visibility.</param>
    /// <param name="navigator">Butil's navigator, for <c>navigator.onLine</c>.</param>
    /// <param name="storageManager">Butil's storage manager, for persistent storage.</param>
    /// <param name="options">What to do; the defaults when omitted.</param>
    public BcrdtBrowserLifecycle(
        IBcrdtCollaborationService collaboration,
        Bit.Butil.Window window,
        Bit.Butil.Document document,
        Bit.Butil.Navigator navigator,
        Bit.Butil.StorageManager storageManager,
        BcrdtBrowserLifecycleOptions? options = null)
        : this(
            collaboration,
            new BcrdtButilBrowser(
                window ?? throw new ArgumentNullException(nameof(window)),
                document ?? throw new ArgumentNullException(nameof(document)),
                navigator ?? throw new ArgumentNullException(nameof(navigator)),
                storageManager ?? throw new ArgumentNullException(nameof(storageManager))),
            options)
    {
    }

    internal BcrdtBrowserLifecycle(IBcrdtCollaborationService collaboration, IBcrdtBrowser browser, BcrdtBrowserLifecycleOptions? options)
    {
        ArgumentNullException.ThrowIfNull(collaboration);

        _collaboration = collaboration;
        _browser = browser;
        _options = options ?? new BcrdtBrowserLifecycleOptions();
    }

    /// <summary>Whether the lifecycle is listening to the browser.</summary>
    public bool IsStarted { get; private set; }

    /// <summary>
    /// Whether the browser believes it has a network connection: <c>navigator.onLine</c>, kept current by the
    /// <c>online</c> and <c>offline</c> events.
    /// </summary>
    /// <remarks>
    /// The browser only knows about the local link, so <see langword="true"/> does not promise that the server is
    /// reachable - <see cref="BcrdtCollaborativeDocument.ConnectionStatus"/> says that - but <see langword="false"/>
    /// is reliable, and the moment to tell the user their edits are being kept for later.
    /// <see langword="true"/> until the lifecycle has started and been told otherwise.
    /// </remarks>
    public bool IsOnline { get; private set; } = true;

    /// <summary>
    /// What the browser answered when asked for persistent storage, or <see langword="null"/> if it was not asked -
    /// see <see cref="BcrdtBrowserLifecycleOptions.RequestPersistentStorage"/>.
    /// </summary>
    public bool? IsStoragePersistent { get; private set; }

    /// <summary>Raised when <see cref="IsOnline"/> changes, with its new value.</summary>
    public event Action<bool>? OnlineChanged;

    /// <summary>
    /// Raised when something the lifecycle did on the browser's behalf failed: a change that could not be stored,
    /// a failing <see cref="BcrdtBrowserLifecycleOptions.AfterFlush"/>, a reconnection that threw.
    /// </summary>
    /// <remarks>
    /// The browser's events arrive with nobody waiting on them, so their failures are reported here rather than
    /// thrown.
    /// </remarks>
    public event Action<Exception>? Error;

    /// <summary>Starts listening to the browser. Calling it again does nothing.</summary>
    /// <remarks>
    /// Does nothing outside a browser, so code shared with a prerendering or Blazor Server host may call it
    /// unconditionally. With <see cref="BcrdtBrowserLifecycleOptions.RequestPersistentStorage"/> on, this is where
    /// the browser is asked.
    /// </remarks>
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_browser.IsAvailable) return;

        lock (_gate)
        {
            if (_starting) return;
            _starting = true;
        }

        try
        {
            // Listening first, so a change between reading the state and subscribing is not missed.
            IAsyncDisposable subscription = await _browser.SubscribeAsync(OnSignalAsync, cancellationToken).ConfigureAwait(false);

            // Disposed while the subscription was being made: nobody else will ever stop it.
            bool disposed;
            lock (_gate)
            {
                disposed = _disposed;
                if (!disposed) _subscription = subscription;
            }
            if (disposed)
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
                return;
            }

            SetOnline(await _browser.IsOnlineAsync(cancellationToken).ConfigureAwait(false));

            if (_options.RequestPersistentStorage)
            {
                IsStoragePersistent = await _browser.RequestPersistentStorageAsync(cancellationToken).ConfigureAwait(false);
            }

            lock (_gate) IsStarted = !_disposed;
        }
        catch
        {
            IAsyncDisposable? subscription;
            lock (_gate)
            {
                subscription = _subscription;
                _subscription = null;
                _starting = false;
            }
            if (subscription is not null) await subscription.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Acts on one thing the browser reported. Never throws: failures go to <see cref="Error"/>.</summary>
    internal async Task OnSignalAsync(BcrdtBrowserSignal signal)
    {
        if (_disposed) return;

        try
        {
            switch (signal)
            {
                case BcrdtBrowserSignal.Online:
                    SetOnline(true);
                    await ReconnectAsync().ConfigureAwait(false);
                    break;

                case BcrdtBrowserSignal.Offline:
                    SetOnline(false);
                    break;

                case BcrdtBrowserSignal.Visible or BcrdtBrowserSignal.Resume:
                    await ReconnectAsync().ConfigureAwait(false);
                    break;

                case BcrdtBrowserSignal.PageShow:
                    if (_options.SuspendPresenceOnPageHide)
                    {
                        foreach (BcrdtCollaborativeDocument document in _collaboration.OpenDocuments) document.Presence.Resume();
                    }
                    await ReconnectAsync().ConfigureAwait(false);
                    break;

                case BcrdtBrowserSignal.PageHide:
                    if (_options.SuspendPresenceOnPageHide)
                    {
                        foreach (BcrdtCollaborativeDocument document in _collaboration.OpenDocuments) document.Presence.Suspend();
                    }
                    await FlushAsync().ConfigureAwait(false);
                    break;

                case BcrdtBrowserSignal.Hidden or BcrdtBrowserSignal.Freeze:
                    await FlushAsync().ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void SetOnline(bool online)
    {
        if (IsOnline == online) return;
        IsOnline = online;
        OnlineChanged?.Invoke(online);
    }

    /// <summary>Connects every document that is waiting out a reconnection backoff, now.</summary>
    /// <remarks>
    /// A fresh connect takes over from the pending attempt and starts the backoff over: if it fails, the provider
    /// carries on with the attempt it already had scheduled rather than starting a second loop. A browser that
    /// reports itself offline is not asked to try.
    /// </remarks>
    private async Task ReconnectAsync()
    {
        if (!_options.ReconnectWhenOnline || !IsOnline) return;

        BcrdtCollaborativeDocument[] waiting = [.. _collaboration.OpenDocuments
            .Where(document => document.ConnectionStatus == BcrdtConnectionStatus.Reconnecting)];

        await Task.WhenAll(waiting.Select(async document =>
        {
            try
            {
                await document.ConnectAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                ReportError(ex);
            }
            catch (ObjectDisposedException)
            {
                // Closed meanwhile.
            }
        })).ConfigureAwait(false);
    }

    /// <summary>Waits for every open document's changes to reach its store, then runs the store's own flush.</summary>
    /// <remarks>
    /// One at a time: a store that keeps failing keeps a flush waiting, and a page hidden and shown repeatedly
    /// meanwhile must not stack up more of them. But a flush already running covers only what was written before
    /// it began, so a signal arriving meanwhile gets one more flush after it - shared by every signal until it
    /// starts, which is later than all of them.
    /// </remarks>
    private Task FlushAsync()
    {
        if (!_options.FlushWhenHidden) return Task.CompletedTask;

        lock (_gate)
        {
            if (_flush.IsCompleted) return _flush = FlushOpenDocumentsAsync();
            return _queuedFlush ??= FlushAfterAsync(_flush);
        }
    }

    private async Task FlushAfterAsync(Task running)
    {
        // Out from under the caller's lock before anything else, so the queued flush is recorded before it
        // can be taken off the queue.
        await Task.Yield();
        await running.ConfigureAwait(false);

        Task next;
        lock (_gate)
        {
            _queuedFlush = null;
            next = _flush = FlushOpenDocumentsAsync();
        }
        await next.ConfigureAwait(false);
    }

    private async Task FlushOpenDocumentsAsync()
    {
        await Task.WhenAll(_collaboration.OpenDocuments.Select(async document =>
        {
            try
            {
                await document.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        })).ConfigureAwait(false);

        if (_options.AfterFlush is { } afterFlush)
        {
            try
            {
                await afterFlush(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }
    }

    private void ReportError(Exception exception)
    {
        try
        {
            Error?.Invoke(exception);
        }
        catch (Exception)
        {
            // A subscriber that throws must not take the browser's event handler down with it.
        }
    }

    /// <inheritdoc/>
    /// <remarks>Stops listening. The documents are the collaboration service's, and are left as they are.</remarks>
    public async ValueTask DisposeAsync()
    {
        IAsyncDisposable? subscription;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            IsStarted = false;

            // A start still waiting on its subscription sees this and stops the subscription itself.
            subscription = _subscription;
            _subscription = null;
        }

        if (subscription is not null) await subscription.DisposeAsync().ConfigureAwait(false);
    }
}
