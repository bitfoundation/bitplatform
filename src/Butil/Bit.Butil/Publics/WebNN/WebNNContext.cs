using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// A WebNN execution context, created by <see cref="WebNN.CreateContext"/>. Dispose it when you are
/// done - a context can hold a real accelerator backend open.
/// </summary>
public sealed class WebNNContext : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private bool _disposed;

    internal WebNNContext(IJSRuntime js, Guid id, WebNNContextInfo info)
    {
        _js = js;
        Id = id;
        Info = info;
    }

    /// <summary>The internal context id.</summary>
    public Guid Id { get; }

    /// <summary>Which backend this context actually got.</summary>
    public WebNNContextInfo Info { get; }

    /// <summary>
    /// Which operators this backend implements, and within what limits.
    /// </summary>
    /// <returns>The entries, or an empty array on a build that doesn't report them.</returns>
    /// <remarks>
    /// The point of asking: the same model can run on one backend and be rejected by another, so a
    /// page that offers a choice of device wants to know before it builds a graph.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebNNOpSupport))]
    public ValueTask<WebNNOpSupport[]> GetOpSupportLimits()
        => _js.Invoke<WebNNOpSupport[]>("BitButil.webNN.opSupportLimits", Id);

    /// <summary>Releases the context. Calling it again does nothing.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try { await _js.InvokeVoid("BitButil.webNN.destroy", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
