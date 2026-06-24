using System;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Per-subscription host for a single <c>MutationObserver</c>. The callback is reached through a
/// per-instance <see cref="DotNetObjectReference{T}"/> (no static state), and the reference is
/// released when the subscription is disposed.
/// </summary>
internal sealed class MutationObserverInterop(Action<MutationRecord[]> handler) : IDisposable
{
    internal const string InvokeMethodName = nameof(InvokeMutation);

    private DotNetObjectReference<MutationObserverInterop>? _dotNetRef;
    internal DotNetObjectReference<MutationObserverInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    [JSInvokable(InvokeMethodName)]
    public void InvokeMutation(Guid id, MutationRecord[] records) => handler(records);

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
