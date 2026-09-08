using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/CloseWatcher">CloseWatcher API</see>:
/// one event for every way a user asks to close something - the Escape key, the Android back
/// gesture, and whatever else the platform offers.
/// </summary>
/// <remarks>
/// This is what makes a custom dialog, drawer or menu behave like a native <c>&lt;dialog&gt;</c>.
/// Without it, handling Escape means a keydown listener that knows nothing about the back gesture,
/// and nesting two of them means deciding by hand which one closes first. The browser keeps a close
/// stack, so the innermost watcher closes first, and only one closes per gesture.
/// <br/>
/// <b>User activation:</b> a page may keep one "free" watcher at a time. Creating a second while the
/// first is active spends a user activation; without one the browser groups them, so a single Escape
/// closes both. Create watchers in response to the interaction that opened the thing, not ahead of
/// time.
/// <br/>
/// Chromium only for now. Where <see cref="IsSupported"/> is false, fall back to your own key
/// handler - <see cref="Create"/> returns null rather than throwing.
/// </remarks>
[ButilService(typeof(CloseWatcher))]
public class CloseWatcher(IJSRuntime js) : IAsyncDisposable
{
    internal const string CloseMethodName = nameof(InvokeClose);
    internal const string CancelMethodName = nameof(InvokeCancel);

    private readonly ConcurrentDictionary<Guid, (Action OnClose, Action? OnCancel)> _handlers = new();

    // Per-instance callback reference (see Keyboard): watchers are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<CloseWatcher>? _dotNetRef;
    private DotNetObjectReference<CloseWatcher> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>CloseWatcher</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.closeWatcher.isSupported");

    /// <summary>
    /// Invoked from JS when the watcher closes. Public + <see cref="JSInvokableAttribute"/> so it
    /// can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(CloseMethodName)]
    public void InvokeClose(Guid id)
    {
        if (_handlers.TryRemove(id, out var handlers)) handlers.OnClose.Invoke();
    }

    /// <summary>
    /// Invoked from JS when a close is requested and the caller asked to be able to refuse it.
    /// Public + <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(CancelMethodName)]
    public void InvokeCancel(Guid id)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnCancel?.Invoke();
    }

    /// <summary>
    /// Starts watching for a close request.
    /// </summary>
    /// <param name="onClose">
    /// Called when the user asked to close and nothing refused - hide the dialog here.
    /// </param>
    /// <param name="onCancel">
    /// Optional. When given, a close request is intercepted first and this runs instead, so you can
    /// ask "discard your changes?" and call <see cref="CloseWatcherHandle.Close"/> yourself if the
    /// answer is yes. The browser only offers this while there is a user activation to spend, so a
    /// close can still arrive without a cancel before it.
    /// </param>
    /// <returns>
    /// The watcher, or null when the runtime has no <c>CloseWatcher</c> or refused to create another
    /// one. <b>Dispose it</b> when the thing it guards is gone.
    /// </returns>
    [DynamicDependency(nameof(InvokeClose), typeof(CloseWatcher))]
    [DynamicDependency(nameof(InvokeCancel), typeof(CloseWatcher))]
    public async ValueTask<CloseWatcherHandle?> Create(Action onClose, Action? onCancel = null)
    {
        ArgumentNullException.ThrowIfNull(onClose);

        var id = Guid.NewGuid();
        _handlers[id] = (onClose, onCancel);

        var created = await js.Invoke<bool>("BitButil.closeWatcher.create",
            DotNetRef, id, CloseMethodName, CancelMethodName, onCancel is not null);

        if (created is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new CloseWatcherHandle(js, id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.closeWatcher.destroy", id);
        });
    }

    /// <summary>Destroys every watcher created through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.closeWatcher.destroy", id);
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
