using Microsoft.AspNetCore.Components;

namespace Bit.Bcrdt.Blazor;

/// <summary>
/// A component base that keeps itself rendered against changing collaborative state.
/// </summary>
/// <remarks>
/// <para>
/// Subscribing a Blazor component to a collaborative document by hand goes wrong in three specific
/// ways, and this class exists to make all three impossible.
/// </para>
/// <para>
/// The first is threading. A notification can be raised on whatever thread changed the state, and
/// calling <c>StateHasChanged</c> from there is undefined behaviour in Blazor Server. Every render
/// requested here is marshalled through <see cref="ComponentBase.InvokeAsync(Func{Task})"/> first. (A
/// document opened through <see cref="BcrdtCollaborationService"/> already applies network updates on the
/// renderer's context; this is the second line of defence, and the one that covers state you change
/// from your own background work.)
/// </para>
/// <para>
/// The second is render storms. A collaborative document under active editing raises events far
/// faster than a browser can paint - a fast typist plus five collaborators can produce hundreds of
/// events a second, and rendering each one makes the UI slower the more people are working. Renders
/// are coalesced onto a frame budget, so bursts collapse into one render.
/// </para>
/// <para>
/// The third is leaks. Subscriptions registered through this class are tracked and disposed with the
/// component. A component that subscribes to a long-lived document and forgets to unsubscribe keeps
/// itself, its render tree and everything they capture alive for the lifetime of the document. Each
/// <c>Track</c> also returns its subscription, for a component that switches between documents and
/// needs to drop the old one's without waiting for its own disposal.
/// </para>
/// </remarks>
public abstract class BcrdtComponentBase : ComponentBase, IAsyncDisposable
{
    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<Func<ValueTask>> _asyncCleanup = [];
    private int _renderPending;
    private bool _disposed;

    /// <summary>The collaboration service for this scope.</summary>
    [Inject]
    protected IBcrdtCollaborationService Collaboration { get; set; } = default!;

    /// <summary>
    /// How long to coalesce renders for. Roughly one frame by default.
    /// </summary>
    /// <remarks>
    /// Set to <see cref="TimeSpan.Zero"/> to render on the next dispatcher turn instead: changes made
    /// within the same turn still collapse into one render, but nothing waits for a frame. That is
    /// appropriate for components showing infrequent changes and for tests that assert on render
    /// counts. A render is never run synchronously inside the change notification that requested it.
    /// </remarks>
    protected TimeSpan RenderInterval { get; set; } = TimeSpan.FromMilliseconds(16);

    /// <summary>Re-renders when a collaborative type changes.</summary>
    /// <returns>The subscription; disposing it stops the tracking early.</returns>
    protected IDisposable Track(BcrdtAbstractType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Register(type.Observe((_, _) => RequestRender()));
    }

    /// <summary>Re-renders when a collaborative type or anything nested inside it changes.</summary>
    /// <returns>The subscription; disposing it stops the tracking early.</returns>
    protected IDisposable TrackDeep(BcrdtAbstractType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Register(type.ObserveDeep((_, _) => RequestRender()));
    }

    /// <summary>Re-renders on any change to a document.</summary>
    /// <returns>The subscription; disposing it stops the tracking early.</returns>
    protected IDisposable Track(BcrdtCollaborativeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        void Handler(BcrdtUpdateEventArgs _) => RequestRender();
        document.Changed += Handler;
        return Register(new Unsubscriber(() => document.Changed -= Handler));
    }

    /// <summary>Re-renders when participants join, leave or move their cursors.</summary>
    /// <returns>The subscription; disposing it stops the tracking early.</returns>
    protected IDisposable Track(BcrdtPresenceService presence)
    {
        ArgumentNullException.ThrowIfNull(presence);

        void Handler() => RequestRender();
        presence.Changed += Handler;
        return Register(new Unsubscriber(() => presence.Changed -= Handler));
    }

    /// <summary>Re-renders when the connection state changes.</summary>
    /// <returns>The subscription; disposing it stops the tracking early.</returns>
    protected IDisposable Track(BcrdtSyncProvider sync)
    {
        ArgumentNullException.ThrowIfNull(sync);

        void Handler(BcrdtConnectionStatus _) => RequestRender();
        sync.StatusChanged += Handler;
        return Register(new Unsubscriber(() => sync.StatusChanged -= Handler));
    }

    /// <summary>Registers a resource to dispose with the component.</summary>
    /// <returns>A handle; disposing it disposes the resource early.</returns>
    protected IDisposable TrackDisposable(IDisposable resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return Register(resource);
    }

    /// <summary>Registers an asynchronous resource to dispose with the component.</summary>
    protected void TrackDisposable(IAsyncDisposable resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        _asyncCleanup.Add(resource.DisposeAsync);
    }

    /// <summary>
    /// Asks for a render, coalescing with any already pending.
    /// </summary>
    /// <remarks>
    /// Safe to call from any thread and at any rate. The flag is claimed before the work is
    /// scheduled, so a burst of a thousand calls schedules exactly one render.
    /// </remarks>
    protected void RequestRender()
    {
        if (_disposed) return;
        if (Interlocked.Exchange(ref _renderPending, 1) == 1) return;
        _ = RenderSoonAsync();
    }

    private async Task RenderSoonAsync()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                // Always leave the current turn, even with no interval: InvokeAsync runs inline when
                // already on the dispatcher, and a render must not run inside the notification that
                // asked for it.
                if (RenderInterval > TimeSpan.Zero) await Task.Delay(RenderInterval).ConfigureAwait(true);
                else await Task.Yield();

                Interlocked.Exchange(ref _renderPending, 0);
                if (_disposed) return;
                StateHasChanged();
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
            // The circuit or component went away while the render was queued. Leave the flag clear so
            // a later request is not swallowed if the component is in fact still alive.
            Interlocked.Exchange(ref _renderPending, 0);
        }
    }

    /// <summary>Releases every tracked subscription and resource.</summary>
    /// <remarks>Every resource is released even if an earlier one throws; the failures are rethrown together.</remarks>
    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        List<Exception>? failures = null;

        foreach (IDisposable subscription in _subscriptions.ToArray())
        {
            try
            {
                subscription.Dispose();
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }
        _subscriptions.Clear();

        foreach (Func<ValueTask> cleanup in _asyncCleanup)
        {
            try
            {
                await cleanup().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }
        _asyncCleanup.Clear();

        GC.SuppressFinalize(this);

        if (failures is not null) throw new AggregateException("One or more tracked resources failed to dispose.", failures);
    }

    private IDisposable Register(IDisposable inner)
    {
        var handle = new TrackedSubscription(this, inner);
        _subscriptions.Add(handle);
        return handle;
    }

    private sealed class TrackedSubscription(BcrdtComponentBase owner, IDisposable inner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            owner._subscriptions.Remove(this);
            inner.Dispose();
        }
    }

    private sealed class Unsubscriber(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}
