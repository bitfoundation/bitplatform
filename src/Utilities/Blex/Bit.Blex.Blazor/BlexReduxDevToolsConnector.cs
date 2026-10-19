using System.Text.Json.Nodes;
using Microsoft.JSInterop;

namespace Bit.Blex.Blazor;

/// <summary>
/// Bridges a <see cref="BlexManager"/> to the Redux DevTools browser extension, enabling
/// live action inspection and time-travel. Created and managed by <see cref="BlexProvider"/>.
/// </summary>
public sealed class BlexReduxDevToolsConnector : IBlexDevTools, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<BlexReduxDevToolsConnector>? _selfRef;
    private BlexManager? _manager;

    /// <summary>
    /// Handle identifying this connector's connection inside the shared JS module (0 = none).
    /// The module is cached per URL, so every connector on the page shares it; without a handle a
    /// re-created provider's disconnect would tear down whichever connection happened to be last.
    /// </summary>
    private int _handle;
    private bool _ready;
    private bool _disposed;

    /// <summary>Creates a connector over the supplied JS runtime.</summary>
    public BlexReduxDevToolsConnector(IJSRuntime js) => _js = js;

    /// <summary>
    /// Loads the JS bridge, connects to the extension and wires the manager. Safe to call when the
    /// extension is absent or the bridge fails to load - it simply becomes a no-op.
    /// </summary>
    public async Task ConnectAsync(BlexManager manager, string name)
    {
        _manager = manager;
        try
        {
            var module = await _js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Bit.Blex.Blazor/bit-blex-devtools.js");
            if (_disposed)
            {
                // Disposed while the import was in flight (fast navigation); don't connect.
                await module.DisposeAsync();
                return;
            }

            _module = module;
            _selfRef = DotNetObjectReference.Create(this);

            var handle = await _module.InvokeAsync<int>("connect", _selfRef, name);
            if (handle > 0 && !_disposed)
            {
                _handle = handle;
                _ready = true;
                manager.ConnectDevTools(this);
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone before the bridge loaded.
        }
        catch (JSException ex)
        {
            // A missing static asset or an extension quirk must not break app startup -- but it
            // should be visible. Routed through the manager so it reaches the app's OnError like
            // every other non-fatal failure, instead of only a console line nobody reads.
            Report(ex, "connecting the DevTools bridge");
        }
    }

    private void Report(Exception exception, string detail)
    {
        if (_manager is { } manager)
            manager.ReportError("devtools", exception, detail);
        else
            Console.Error.WriteLine($"[Blex] {detail}: {exception.Message}");
    }

    /// <inheritdoc />
    public void Init(JsonObject globalState)
    {
        if (_ready && _module is not null)
            FireAndForget(_module.InvokeVoidAsync("init", _handle, globalState.ToJsonString()));
    }

    /// <inheritdoc />
    public void Send(string actionName, JsonObject globalState)
        => Send(actionName, globalState, null);

    /// <inheritdoc />
    public void Send(string actionName, JsonObject globalState, JsonObject? payload)
    {
        if (_ready && _module is not null)
            FireAndForget(_module.InvokeVoidAsync("send", _handle, actionName, globalState.ToJsonString(), payload?.ToJsonString()));
    }

    // DevTools is a non-critical sink, so interop is dispatched without blocking the dispatch
    // pipeline. We still observe the task so a transient interop failure can't surface as an
    // unobserved task exception.
    private void FireAndForget(ValueTask task)
    {
        if (task.IsCompletedSuccessfully)
            return;

        _ = Awaited(this, task);

        static async Task Awaited(BlexReduxDevToolsConnector self, ValueTask t)
        {
            try
            {
                await t;
            }
            catch (JSDisconnectedException)
            {
                // Circuit gone; nothing to report.
            }
            catch (Exception ex)
            {
                self.Report(ex, "sending to DevTools");
            }
        }
    }

    /// <summary>Invoked from JS for every message the extension sends back (drives time-travel).</summary>
    [JSInvokable]
    public Task HandleMessage(string messageJson)
    {
        _manager?.HandleDevToolsMessage(messageJson);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _ready = false;
        try
        {
            if (_module is not null)
            {
                if (_handle > 0)
                    await _module.InvokeVoidAsync("disconnect", _handle);
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or TaskCanceledException or ObjectDisposedException)
        {
            // Circuit teardown can surface any of these from JS interop, and a reloaded BlazorWebView page
            // answers for the old page's module with a JSException; nothing to clean up either way.
        }

        _selfRef?.Dispose();
    }
}
