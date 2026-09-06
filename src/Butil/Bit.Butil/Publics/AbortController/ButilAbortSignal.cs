using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A live <see href="https://developer.mozilla.org/en-US/docs/Web/API/AbortSignal">AbortSignal</see>:
/// the half of an <see cref="AbortController"/> you hand out. Named <c>ButilAbortSignal</c> because
/// the shorter name would collide with nothing in the BCL but everything in a caller's head - this
/// is the browser's signal, not <see cref="CancellationToken"/>.
/// </summary>
/// <remarks>
/// A signal can be given to any number of operations at once; the one <see cref="AbortControllerHandle.Abort"/>
/// behind it cancels all of them. Signals from <see cref="AbortController.Timeout"/> and
/// <see cref="AbortController.Any"/> have no controller and abort on their own terms.
/// <br/>
/// Disposing releases the JS registry entry. It does not abort - a signal already handed to a
/// pending <see cref="Fetch"/> keeps guarding it.
/// </remarks>
public class ButilAbortSignal : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly AbortController _owner;
    private bool _released;

    internal ButilAbortSignal(IJSRuntime js, AbortController owner, Guid id)
    {
        _js = js;
        _owner = owner;
        Id = id;
    }

    /// <summary>
    /// The signal's id. This is what crosses the interop boundary when a signal is passed to another
    /// API - <see cref="FetchRequest.Signal"/>, for instance - so a released signal is just an id
    /// nothing answers to any more.
    /// </summary>
    public Guid Id { get; }

    /// <summary>Whether the signal has already aborted.</summary>
    /// <remarks>Returns <c>false</c> during prerender/SSR rather than throwing.</remarks>
    public ValueTask<bool> GetAborted() => _js.Invoke<bool>("BitButil.abortController.aborted", Id);

    /// <summary>
    /// Why the signal aborted, flattened to text: the <c>DOMException</c>'s message for
    /// <see cref="AbortControllerHandle.Abort"/> with no reason and for
    /// <see cref="AbortController.Timeout"/>, otherwise whatever reason was given.
    /// </summary>
    /// <returns>An empty string while the signal has not aborted.</returns>
    public ValueTask<string> GetReason() => _js.Invoke<string>("BitButil.abortController.reason", Id);

    /// <summary>
    /// Runs <paramref name="onAbort"/> when the signal aborts, with the reason as its argument.
    /// </summary>
    /// <returns>
    /// A subscription to dispose when you no longer want the callback, or null when the signal has
    /// already been released.
    /// </returns>
    /// <remarks>
    /// A signal aborts once and never again, so the listener fires at most once. Subscribing to a
    /// signal that has <em>already</em> aborted invokes the callback immediately rather than never -
    /// otherwise a race between the abort and the subscription would silently lose the event.
    /// </remarks>
    public ValueTask<ButilSubscription?> OnAbort(Action<string> onAbort) => _owner.AddAbortListener(Id, onAbort);

    /// <summary>
    /// Releases the signal's JS entry and detaches any listeners. Idempotent, and safe during
    /// teardown. Does not abort the signal.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_released) return;
        _released = true;

        try { await _js.InvokeVoid("BitButil.abortController.release", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            // In a finally because the .NET half is ours to clean up whether or not the JS half
            // could be reached: a released signal never fires, so its listeners are unreachable
            // either way, and leaving them would hold the callbacks - and whatever they close over.
            _owner.ForgetSignalListeners(Id);
        }

        GC.SuppressFinalize(this);
    }
}
