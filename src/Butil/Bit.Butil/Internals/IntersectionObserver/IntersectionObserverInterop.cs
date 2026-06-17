using System;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Per-subscription host for a single <c>IntersectionObserver</c>. The callback is reached through
/// a per-instance <see cref="DotNetObjectReference{T}"/> (no static state), and the reference is
/// released when the subscription is disposed.
/// </summary>
internal sealed class IntersectionObserverInterop(Action<IntersectionObserverEntry[]> handler) : IDisposable
{
    internal const string InvokeMethodName = nameof(InvokeIntersection);

    private DotNetObjectReference<IntersectionObserverInterop>? _dotNetRef;
    internal DotNetObjectReference<IntersectionObserverInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    [JSInvokable(InvokeMethodName)]
    public void InvokeIntersection(Guid id, IntersectionObserverEntry[] entries) => handler(entries);

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
