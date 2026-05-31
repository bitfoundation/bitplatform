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
        if (jsRuntime.IsJsRuntimeInvalid()) return default;

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
        if (jsRuntime.IsJsRuntimeInvalid()) return default;

        return BitButil.FastInvokeEnabled
            ? jsRuntime.FastInvokeAsync<TValue>(identifier, cancellationToken, args)
            : jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }


    /// <summary>
    /// Returns true when calling into JavaScript right now would either be impossible
    /// (no runtime / pre-render) or guaranteed to fail (disposed circuit).
    /// </summary>
    /// <remarks>
    /// We deliberately avoid reflecting over private fields of <c>RemoteJSRuntime</c>
    /// or <c>WebViewJSRuntime</c>; those internals have changed across .NET releases.
    /// Instead we rely on the only documented sentinel — the
    /// <c>UnsupportedJavaScriptRuntime</c> type used during static SSR / pre-render —
    /// and let actual disconnect surface as <see cref="JSDisconnectedException"/> at
    /// the call site, which callers already catch.
    /// </remarks>
    internal static bool IsJsRuntimeInvalid(this IJSRuntime? jsRuntime)
    {
        if (jsRuntime is null) return true;

        // During pre-rendering ASP.NET injects an UnsupportedJavaScriptRuntime that
        // throws on every call. We special-case it to keep prerender silent.
        return jsRuntime.GetType().Name == "UnsupportedJavaScriptRuntime";
    }
}
