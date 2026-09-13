using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/Worker">Worker</see> and
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/SharedWorker">SharedWorker</see>:
/// JavaScript running on a thread of its own, so long work stops freezing the page.
/// </summary>
/// <remarks>
/// <b>A worker runs a script you supply.</b> There is no second .NET runtime behind this: the URL
/// you pass is fetched and run as JavaScript, and the conversation with it is messages, not method
/// calls. That is the honest shape of the browser API, and it is why the payload types here are
/// <see cref="ButilMessage"/> rather than anything typed.
/// <br/>
/// A <b>dedicated</b> worker belongs to the page that created it and dies with it - or when
/// <see cref="WorkerHandle.Terminate"/> says so. A <b>shared</b> worker is one instance for every
/// page of the origin naming the same script and name, reached through a
/// <see cref="MessagePortHandle"/>; no page can terminate it for the others, and it ends when the
/// last port closes.
/// <br/>
/// The script must be same-origin (or a <c>blob:</c> URL the page created). A cross-origin script
/// URL fails in the constructor, which is why <see cref="Create"/> can answer with null.
/// </remarks>
[ButilService(typeof(Worker))]
public class Worker(IJSRuntime js, MessageChannel messageChannel) : IAsyncDisposable
{
    internal const string MessageMethodName = nameof(InvokeWorkerMessage);
    internal const string ErrorMethodName = nameof(InvokeWorkerError);

    private readonly ConcurrentDictionary<Guid, WorkerHandlers> _handlers = new();

    // Per-instance callback reference (see Keyboard): workers are isolated per circuit / WASM app
    // and terminated on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Worker>? _dotNetRef;
    private DotNetObjectReference<Worker> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    private sealed record WorkerHandlers(Action<ButilMessage> OnMessage, Action<WorkerError>? OnError);

    /// <summary>True when the runtime exposes <c>Worker</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value. If you branch on it,
    /// defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.worker.isSupported");

    /// <summary>
    /// True when the runtime exposes <c>SharedWorker</c>. Not universal - shipping engines have
    /// dropped and re-added it, so check before relying on it.
    /// </summary>
    public ValueTask<bool> IsSharedSupported() => js.Invoke<bool>("BitButil.worker.isSharedSupported");

    /// <summary>
    /// Invoked from JS for each message a dedicated worker posts. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(MessageMethodName)]
    public void InvokeWorkerMessage(Guid id, bool isBinary, string? json, byte[]? data)
    {
        if (_handlers.TryGetValue(id, out var handlers))
            handlers.OnMessage.Invoke(new ButilMessage(isBinary, json, data));
    }

    /// <summary>
    /// Invoked from JS when a worker fails. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ErrorMethodName)]
    public void InvokeWorkerError(Guid id, string message, string fileName, int lineNumber, int columnNumber)
    {
        if (_handlers.TryGetValue(id, out var handlers))
            handlers.OnError?.Invoke(new WorkerError(message, fileName, lineNumber, columnNumber));
    }

    /// <summary>
    /// Starts a dedicated worker.
    /// </summary>
    /// <param name="scriptUrl">
    /// A same-origin URL to a JavaScript file - or a <c>blob:</c> URL this page made, which is how a
    /// worker is built from a string. Cross-origin URLs are refused by the constructor.
    /// </param>
    /// <param name="onMessage">Called for everything the worker posts back.</param>
    /// <param name="options">Name, module type and credentials. Defaults to a classic, unnamed worker.</param>
    /// <param name="onError">
    /// Called when the worker throws where nothing caught it, and when it posts something this page
    /// cannot deserialize. A worker that has errored is still alive - the error does not terminate
    /// it.
    /// </param>
    /// <returns>A handle, or null when the runtime has no <c>Worker</c> or the URL was refused.</returns>
    /// <remarks>
    /// A script that 404s, or throws while loading, does <em>not</em> fail here - it surfaces through
    /// <paramref name="onError"/> instead, because the constructor returns before the script has
    /// been fetched.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilMessage))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WorkerError))]
    public async ValueTask<WorkerHandle?> Create(string scriptUrl,
                                                 Action<ButilMessage> onMessage,
                                                 WorkerOptions? options = null,
                                                 Action<WorkerError>? onError = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptUrl);
        ArgumentNullException.ThrowIfNull(onMessage);

        var id = Guid.NewGuid();
        _handlers[id] = new WorkerHandlers(onMessage, onError);

        var created = await js.Invoke<bool>("BitButil.worker.create",
            DotNetRef, id, scriptUrl, options?.Name, options?.Module ?? false, options?.Credentials);

        if (created is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new WorkerHandle(js, id, () => _handlers.TryRemove(id, out _));
    }

    /// <summary>
    /// Connects to a shared worker, starting it if this is the first page to ask.
    /// </summary>
    /// <param name="scriptUrl">A same-origin URL to a JavaScript file.</param>
    /// <param name="options">
    /// Name, module type and credentials. The <see cref="WorkerOptions.Name"/> matters more here
    /// than for a dedicated worker: script URL <em>and</em> name together decide whether two pages
    /// get the same worker or two different ones.
    /// </param>
    /// <returns>A handle, or null when the runtime has no <c>SharedWorker</c> or the URL was refused.</returns>
    /// <remarks>
    /// The conversation happens over <see cref="SharedWorkerHandle.Port"/>, which has to be started
    /// before anything arrives. The worker's script sees each page as a <c>connect</c> event
    /// carrying the other end of that same port.
    /// <br/>
    /// There is no error callback: a shared worker's failures are reported to whichever context is
    /// looking, not reliably to every connected page.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilMessage))]
    public async ValueTask<SharedWorkerHandle?> CreateShared(string scriptUrl, WorkerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptUrl);

        var id = Guid.NewGuid();
        var portId = Guid.NewGuid();

        var created = await js.Invoke<bool>("BitButil.worker.createShared",
            id, portId, scriptUrl, options?.Name, options?.Module ?? false, options?.Credentials);

        return created ? new SharedWorkerHandle(js, messageChannel, id, portId) : null;
    }

    /// <summary>
    /// On scope/circuit teardown, terminates any dedicated worker whose handle was never disposed
    /// and drops this page's connection to any shared one.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.worker.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
