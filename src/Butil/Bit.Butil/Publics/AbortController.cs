using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/AbortController">AbortController</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/AbortSignal">AbortSignal</see>:
/// a cancellation token the browser understands, which you create once and hand to as many
/// operations as you like.
/// </summary>
/// <remarks>
/// The point of a standalone controller is sharing. A <see cref="ButilAbortSignal"/> can be passed
/// to several APIs at once, and one <see cref="AbortControllerHandle.Abort"/> cancels all of them -
/// which the per-API abort handles (<see cref="AbortableFetch"/> and friends) cannot do, since each
/// only reaches its own call.
/// <br/>
/// <see cref="Timeout"/> and <see cref="Any"/> produce signals nobody can abort by hand: the first
/// fires itself after a delay, the second when any of its sources fires. Composing them - a request
/// deadline that a Cancel button can also trip - is what they are for.
/// </remarks>
[ButilService(typeof(AbortController))]
public class AbortController(IJSRuntime js) : IAsyncDisposable
{
    internal const string AbortMethodName = nameof(InvokeAbort);

    private readonly ConcurrentDictionary<Guid, Action<string>> _handlers = new();

    // Per-instance callback reference: signals are isolated per circuit / WASM app and released on
    // disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<AbortController>? _dotNetRef;
    private DotNetObjectReference<AbortController> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>AbortController</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (<c>false</c>) rather than
    /// throwing, so the result can't be distinguished from a genuine value. If you branch on it,
    /// defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.abortController.isSupported");

    /// <summary>True when the runtime exposes <c>AbortSignal.timeout</c>.</summary>
    public ValueTask<bool> IsTimeoutSupported() => js.Invoke<bool>("BitButil.abortController.isTimeoutSupported");

    /// <summary>
    /// True when the runtime exposes <c>AbortSignal.any</c>. <see cref="Any"/> still works when this
    /// is false - it falls back to a hand-wired controller with the same behaviour.
    /// </summary>
    public ValueTask<bool> IsAnySupported() => js.Invoke<bool>("BitButil.abortController.isAnySupported");

    /// <summary>
    /// Invoked from JS when a signal aborts. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(AbortMethodName)]
    public void InvokeAbort(Guid listenerId, string reason)
    {
        if (_handlers.TryGetValue(listenerId, out var handler)) handler(reason);
    }

    /// <summary>
    /// Creates a controller and its signal.
    /// </summary>
    /// <returns>A handle, or null when the runtime has no <c>AbortController</c>.</returns>
    /// <remarks>
    /// Dispose the handle when you're done to release the signal's JS entry. Disposing does not
    /// abort it - releasing a handle is not the same as cancelling what it guards.
    /// </remarks>
    public async ValueTask<AbortControllerHandle?> Create()
    {
        var id = Guid.NewGuid();
        var created = await js.Invoke<bool>("BitButil.abortController.create", id);

        return created ? new AbortControllerHandle(js, this, id) : null;
    }

    /// <summary>
    /// Creates a signal that aborts itself after <paramref name="delay"/> with a
    /// <c>TimeoutError</c> reason. Nothing can abort it early.
    /// </summary>
    /// <param name="delay">How long to wait. Rounded to whole milliseconds.</param>
    /// <returns>A signal, or null when the runtime has no <c>AbortSignal.timeout</c>.</returns>
    /// <remarks>
    /// The timer starts immediately, not when the signal is first used. Combine it with a
    /// controller's own signal through <see cref="Any"/> to get a deadline that a user can also
    /// cancel by hand.
    /// </remarks>
    public async ValueTask<ButilAbortSignal?> Timeout(TimeSpan delay)
    {
        var id = Guid.NewGuid();
        var created = await js.Invoke<bool>("BitButil.abortController.timeout", id, (long)delay.TotalMilliseconds);

        return created ? new ButilAbortSignal(js, this, id) : null;
    }

    /// <summary>
    /// Creates a signal that aborts as soon as any of <paramref name="signals"/> does, carrying that
    /// source's reason.
    /// </summary>
    /// <returns>
    /// A signal, or null when any source has already been released or the runtime has neither
    /// <c>AbortSignal.any</c> nor <c>AbortController</c>.
    /// </returns>
    /// <remarks>
    /// The composite does not keep its sources alive: release one and the composite is left watching
    /// a signal that can no longer fire, so dispose the composite first. Where <c>AbortSignal.any</c>
    /// is missing (Safari before 17.4) this wires a controller to the sources by hand, which behaves
    /// identically.
    /// </remarks>
    public async ValueTask<ButilAbortSignal?> Any(params ButilAbortSignal[] signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        var id = Guid.NewGuid();
        var created = await js.Invoke<bool>("BitButil.abortController.any", id, Array.ConvertAll(signals, s => s.Id));

        return created ? new ButilAbortSignal(js, this, id) : null;
    }

    internal async ValueTask<ButilSubscription?> AddAbortListener(Guid signalId, Action<string> onAbort)
    {
        ArgumentNullException.ThrowIfNull(onAbort);

        var listenerId = Guid.NewGuid();
        _handlers[listenerId] = onAbort;

        var added = await js.Invoke<bool>("BitButil.abortController.addListener", DotNetRef, signalId, listenerId);

        if (added is false)
        {
            _handlers.TryRemove(listenerId, out _);
            return null;
        }

        return new ButilSubscription(listenerId, async () =>
        {
            _handlers.TryRemove(listenerId, out _);
            await js.InvokeVoid("BitButil.abortController.removeListener", signalId, listenerId);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, releases every signal whose handle was never disposed, along with
    /// the interop reference their callbacks were dispatched through.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.abortController.releaseAll");
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
