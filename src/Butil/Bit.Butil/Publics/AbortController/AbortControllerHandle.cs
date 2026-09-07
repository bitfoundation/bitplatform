using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A controller returned by <see cref="AbortController.Create"/>: the abort button for the
/// <see cref="Signal"/> it owns.
/// </summary>
/// <remarks>
/// Keep the handle, hand out the <see cref="Signal"/>. Everything that took the signal is cancelled
/// by a single <see cref="Abort"/>, which is the whole reason to create a controller rather than
/// using an API's own abort handle.
/// </remarks>
public sealed class AbortControllerHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;

    internal AbortControllerHandle(IJSRuntime js, AbortController owner, Guid id)
    {
        _js = js;
        Signal = new ButilAbortSignal(js, owner, id);
    }

    /// <summary>The signal this controller aborts. Pass it to anything that accepts one.</summary>
    public ButilAbortSignal Signal { get; }

    /// <summary>
    /// Aborts the signal, running every listener attached to it.
    /// </summary>
    /// <param name="reason">
    /// Why, readable by anything watching the signal. Left null, the browser's own
    /// <c>AbortError</c> message is used - which is what a caller with nothing to add wants, since
    /// an empty reason would replace a meaningful message with nothing.
    /// </param>
    /// <remarks>
    /// Aborting twice does nothing the second time: a signal keeps the first reason it was given.
    /// </remarks>
    public ValueTask Abort(string? reason = null)
        => _js.InvokeVoid("BitButil.abortController.abort", Signal.Id, reason);

    /// <summary>
    /// Releases the controller and its signal. Idempotent, and safe during teardown.
    /// </summary>
    /// <remarks>
    /// Disposing does <em>not</em> abort. Call <see cref="Abort"/> first if that is what you meant -
    /// letting a handle go out of scope cancels nothing, exactly as an unaborted
    /// <c>AbortController</c> in JavaScript cancels nothing.
    /// </remarks>
    public ValueTask DisposeAsync() => Signal.DisposeAsync();
}
