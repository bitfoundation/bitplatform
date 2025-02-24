using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

internal static class InternalJSRuntimeExtensions
{
    internal static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return InvokeVoid(jsRuntime, identifier, CancellationToken.None, args);
    }

    internal static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return InvokeVoid(jsRuntime, identifier, cancellationToken, args);
    }

    internal static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        return BitButil.FastInvokeEnabled
            ? jsRuntime.FastInvokeVoidAsync(identifier, cancellationToken, args)
            : jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }



    internal static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return Invoke<TValue>(jsRuntime, identifier, CancellationToken.None, args);
    }

    internal static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return Invoke<TValue>(jsRuntime, identifier, cancellationToken, args);
    }

    internal static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        return BitButil.FastInvokeEnabled
            ? jsRuntime.FastInvokeAsync<TValue>(identifier, cancellationToken, args)
            : jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }
}
