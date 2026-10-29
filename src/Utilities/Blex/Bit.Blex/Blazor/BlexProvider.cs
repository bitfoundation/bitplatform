using Bit.Blex.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Blex.Blazor;

/// <summary>
/// Root component that wires every registered <see cref="IBlexStore"/> into the <see cref="BlexManager"/>
/// manager and (on first render) connects the Redux DevTools bridge. Place it once near the root of
/// your app, wrapping your routes:
/// <code>&lt;BlexProvider&gt;&lt;Router ... /&gt;&lt;/BlexProvider&gt;</code>
/// </summary>
public sealed class BlexProvider : ComponentBase, IAsyncDisposable
{
    private BlexReduxDevToolsConnector? _connector;
    private BlexComponentStatePersistence? _componentState;
    private BlexQueryStatePersistence? _queryState;
    private BlexQueryFocusWatcher? _queryFocus;
    private bool _disposed;
    private Task? _durableStartTask;
    private bool _durableStateReady;

    [Inject] private BlexManager Manager { get; set; } = default!;
    [Inject] private IEnumerable<IBlexStore> Stores { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private BlexOptions Options { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// Enables the Redux DevTools connection. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// When enabled, the entire application state tree is serialized and exposed to the Redux
    /// DevTools browser extension on every action. This is intended for development only; set
    /// this to <c>false</c> in production (for example, bind it to your host environment) to
    /// avoid leaking application state to an installed extension.
    /// </remarks>
    [Parameter] public bool EnableDevTools { get; set; }

    /// <summary>
    /// Hands prerendered store state to the interactive render via Blazor's
    /// <see cref="PersistentComponentState"/>, avoiding the prerender "double render" flicker.
    /// Defaults to <c>true</c>; has no effect when the framework service is unavailable.
    /// </summary>
    [Parameter] public bool PersistComponentState { get; set; } = true;

    /// <summary>The application content rendered inside the provider.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        foreach (var store in Stores)
            Manager.Register(store);

        // Prerender -> interactive handoff (synchronous, before durable restore).
        if (PersistComponentState && IsStaticRender() is false)
        {
            var pcs = Services.GetService<PersistentComponentState>();
            if (pcs is not null)
            {
                _componentState = new BlexComponentStatePersistence(pcs, Stores, Manager, HandoffRenderMode());
                _componentState.TryRestore();

                // Before any descendant initializes and asks for a query the prerender already fetched.
                if (Services.GetService<BlexQueryClient>() is { } queries)
                {
                    _queryState = new BlexQueryStatePersistence(pcs, queries, Manager, HandoffRenderMode());
                    _queryState.TryRestore();
                }
            }
        }

        // Durable persistence + history. During Blazor Server prerendering, browser storage is
        // unreachable (JS interop is not available yet); in that case this is retried on first
        // render so the app still starts and hydrates as soon as the circuit is interactive.
        _durableStartTask = StartDurableStateAsync(rethrowInteropUnavailable: false);
        await _durableStartTask;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        // Blazor renders at the first await inside OnInitializedAsync, so this can run while the
        // initial start is still in flight; await it rather than starting a concurrent one.
        if (_durableStartTask is not null)
            await _durableStartTask;

        if (!_durableStateReady)
            await StartDurableStateAsync(rethrowInteropUnavailable: true);

        if (EnableDevTools)
        {
            _connector = new BlexReduxDevToolsConnector(Js);
            await _connector.ConnectAsync(Manager, Options.DevToolsName);
        }

        // Stale queries someone watches refetch when the tab is visible again or the network returns. Not once the
        // provider is gone: it was disposed while the awaits above ran, and nothing would dispose a watcher now.
        if (!_disposed && Services.GetService<BlexQueryClient>() is { } queries)
        {
            _queryFocus = new BlexQueryFocusWatcher(Js, queries, Manager);
            await _queryFocus.StartAsync(reportOffline: RequestsLeaveFromTheDevice());
        }
    }

    /// <summary>
    /// Whether the queries' requests leave from the device the browser runs on -- WebAssembly, or a BlazorWebView --
    /// so the browser going offline pauses them. On Blazor Server they leave from the server, which the browser's
    /// network says nothing about (and an "offline" event delivered late over a reconnecting circuit could only
    /// leave the cache thinking it is offline).
    /// </summary>
    private bool RequestsLeaveFromTheDevice()
    {
        return RendererInfo.Name is "WebAssembly" or "WebView";
    }

    private async Task StartDurableStateAsync(bool rethrowInteropUnavailable)
    {
        try
        {
            var persistor = Services.GetService<BlexStatePersistor>();
            if (persistor is not null)
                await persistor.StartAsync();
        }
        catch (InvalidOperationException) when (!rethrowInteropUnavailable)
        {
            // JS interop is unavailable during prerendering; retry after the first render.
            return;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
        {
            // Circuit torn down mid-start; JS interop surfaces any of these. Nothing to do.
            return;
        }

        _durableStateReady = true;

        // Start recording history only after rehydration so the baseline (the state Undo
        // ultimately returns to) is the hydrated state, not the pre-hydration defaults.
        Services.GetService<BlexHistory>()?.Start();
    }

    /// <summary>
    /// The render mode the prerendered state is handed to.
    /// </summary>
    /// <remarks>
    /// It has to be explicit: a Blazor Web App that registers more than one interactive render mode cannot
    /// infer it for the persistence callback, and fails the whole prerendered response when it has to. The
    /// provider knows its own mode (null in a host without render modes, BlazorWebView, which never
    /// prerenders).
    /// </remarks>
    private IComponentRenderMode? HandoffRenderMode()
    {
        return AssignedRenderMode;
    }

    /// <summary>
    /// True for a provider rendered with static server-side rendering: no interactive render follows it, so
    /// there is no prerendered state to restore and none worth handing over.
    /// </summary>
    private bool IsStaticRender()
    {
        return AssignedRenderMode is null && RendererInfo.IsInteractive is false;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Ahead of the content, so the renderer's dispatcher is bound before any descendant initializes:
        // from then on the manager reports a store changing outside it, and InvokeAsync marshals to it.
        builder.OpenComponent<BlexDispatcherBinding>(0);
        builder.AddComponentParameter(1, BlexDispatcherBinding.ManagerParameter, Manager);
        builder.CloseComponent();
        builder.AddContent(2, ChildContent);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _componentState?.Dispose();
        _queryState?.Dispose();

        if (_queryFocus is not null)
            await _queryFocus.DisposeAsync();

        if (_connector is not null)
        {
            Manager.DisconnectDevTools();
            await _connector.DisposeAsync();
        }
    }
}
