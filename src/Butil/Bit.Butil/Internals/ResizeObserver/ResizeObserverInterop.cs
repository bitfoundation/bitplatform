using System;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Per-subscription host for a single <c>ResizeObserver</c>. The callback is reached through a
/// per-instance <see cref="DotNetObjectReference{T}"/> (no static state), and the reference is
/// released when the subscription is disposed.
/// </summary>
internal sealed class ResizeObserverInterop(Action<ResizeObserverEntry[]> handler) : IDisposable
{
    internal const string InvokeMethodName = nameof(InvokeResize);

    private DotNetObjectReference<ResizeObserverInterop>? _dotNetRef;
    internal DotNetObjectReference<ResizeObserverInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    [JSInvokable(InvokeMethodName)]
    public void InvokeResize(Guid id, ResizeObserverEntry[] entries) => handler(entries);

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
