namespace Bit.Bcrdt.Butil;

/// <summary>What <see cref="BcrdtBrowserLifecycle"/> does when the browser reports a change.</summary>
public sealed class BcrdtBrowserLifecycleOptions
{
    /// <summary>
    /// Whether documents waiting out a reconnection backoff reconnect at once when the browser comes back online,
    /// the page is shown again, or a frozen page resumes.
    /// </summary>
    /// <remarks>
    /// The sync provider backs off exponentially, up to 30 seconds by default, because it cannot tell an outage
    /// that is over from one that is not. The browser can: a laptop waking up or a phone leaving a tunnel reports
    /// <c>online</c>, and a tab brought back to the front reports itself visible. Only a document in
    /// <see cref="BcrdtConnectionStatus.Reconnecting"/> is touched - never one the application disconnected on
    /// purpose, nor one that was refused, which stays <see cref="BcrdtConnectionStatus.Faulted"/>.
    /// </remarks>
    public bool ReconnectWhenOnline { get; set; } = true;

    /// <summary>
    /// Whether every open document's pending changes are flushed to its store when the page is hidden, unloaded or
    /// frozen - the last moments a browser reliably gives a page to save its state.
    /// </summary>
    /// <remarks>
    /// The persistence layer writes in the background as changes happen, so this does not start writes that would
    /// otherwise wait; it waits for them, then runs <see cref="AfterFlush"/>, and reports a change that could not be
    /// stored through <see cref="BcrdtBrowserLifecycle.Error"/>.
    /// </remarks>
    public bool FlushWhenHidden { get; set; } = true;

    /// <summary>
    /// Runs once every open document's changes have reached its store, when the page is hidden - to make the
    /// store itself durable, when its writes are buffered.
    /// </summary>
    /// <remarks>
    /// For example a <c>BcrdtBlazeDbDocumentStore</c> with <c>WaitForDurability</c> off, whose writes reach the
    /// database's memory at once and its storage on the next background flush:
    /// <c>options.AfterFlush = cancellationToken => database.FlushAsync(cancellationToken);</c>
    /// </remarks>
    public Func<CancellationToken, ValueTask>? AfterFlush { get; set; }

    /// <summary>
    /// Whether the local participant is withdrawn from every open document's presence when the page is unloaded or
    /// put in the back/forward cache, and restored when it is shown again.
    /// </summary>
    /// <remarks>
    /// Without it a closed tab stays listed until the presence timeout - 30 seconds by default - because nothing
    /// runs to say goodbye. The withdrawal is sent while the page is being torn down, so it reaches the others when
    /// the browser still lets the connection send - a navigation or a closed tab, as a rule - and the timeout
    /// covers the rest.
    /// </remarks>
    public bool SuspendPresenceOnPageHide { get; set; } = true;

    /// <summary>
    /// Whether to ask the browser, when the lifecycle starts, to make this site's storage persistent - so documents
    /// kept for offline use are not evicted when the device runs short of space.
    /// </summary>
    /// <remarks>
    /// Off by default, because some browsers ask the user rather than deciding by themselves, and a prompt at
    /// startup that the application did not ask for is a surprise. Chromium decides from how the site is used,
    /// and grants it at once to an installed app. <see cref="BcrdtBrowserLifecycle.IsStoragePersistent"/> says
    /// what the browser answered.
    /// </remarks>
    public bool RequestPersistentStorage { get; set; }
}

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
            _subscription = await _browser.SubscribeAsync(OnSignalAsync, cancellationToken).ConfigureAwait(false);
            SetOnline(await _browser.IsOnlineAsync(cancellationToken).ConfigureAwait(false));

            if (_options.RequestPersistentStorage)
            {
                IsStoragePersistent = await _browser.RequestPersistentStorageAsync(cancellationToken).ConfigureAwait(false);
            }

            IsStarted = true;
        }
        catch
        {
            IAsyncDisposable? subscription = _subscription;
            _subscription = null;
            if (subscription is not null) await subscription.DisposeAsync().ConfigureAwait(false);

            lock (_gate) _starting = false;
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
    /// meanwhile must not stack up more of them.
    /// </remarks>
    private Task FlushAsync()
    {
        if (!_options.FlushWhenHidden) return Task.CompletedTask;

        lock (_gate)
        {
            if (!_flush.IsCompleted) return _flush;
            return _flush = FlushOpenDocumentsAsync();
        }
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
        if (_disposed) return;
        _disposed = true;
        IsStarted = false;

        IAsyncDisposable? subscription = _subscription;
        _subscription = null;
        if (subscription is not null) await subscription.DisposeAsync().ConfigureAwait(false);
    }
}
