using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Handle to an in-flight <see cref="Fetch.Send"/>. Disposing aborts the request unless it has
/// already completed.
/// </summary>
public sealed class AbortableFetch : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _completed;

    internal AbortableFetch(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>The internal request id.</summary>
    public Guid Id => _id;

    /// <summary>Aborts the request immediately if it's still in flight.</summary>
    public ValueTask Abort()
    {
        _completed = true;
        return _js.InvokeVoid("BitButil.fetch.abort", _id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_completed) return;
        _completed = true;
        try { await _js.InvokeVoid("BitButil.fetch.abort", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
