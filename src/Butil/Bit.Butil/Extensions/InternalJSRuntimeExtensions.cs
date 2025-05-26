using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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


    [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
    internal static bool IsJsRuntimeInvalid(this IJSRuntime jsRuntime)
    {
        if (jsRuntime is null) return false;

        var type = jsRuntime.GetType();

        return type.Name switch
        {
            "UnsupportedJavaScriptRuntime" => true, // Prerendering
            "RemoteJSRuntime" => (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)! is false, // Blazor server
            "WebViewJSRuntime" => type.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(jsRuntime) is null, // Blazor Hybrid
            _ => false // Blazor WASM
        };
    }
}
