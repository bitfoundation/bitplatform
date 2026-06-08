using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

internal static class InternalJSRuntimeExtensions
{
    /// <summary>
    /// Invokes a void JavaScript function through the safe async path.
    /// </summary>
    /// <remarks>
    /// During static SSR / pre-render (when no real JS runtime is available) this is a no-op:
    /// it returns a completed <see cref="ValueTask"/> without calling into JS, so callers don't
    /// have to special-case prerender. See <see cref="IsJsRuntimeInvalid"/>.
    /// </remarks>
    internal static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return InvokeVoid(jsRuntime, identifier, CancellationToken.None, args);
    }

    internal static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        // This method must stay async: the CancellationTokenSource's internal timer is what
        // enforces the timeout, and it must remain alive (undisposed) until the JS call
        // completes. Returning the ValueTask from a non-async method would dispose the CTS
        // immediately, cancelling its timer and silently defeating the timeout.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        await InvokeVoid(jsRuntime, identifier, cancellationToken, args);
    }

    internal static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsJsRuntimeInvalid()) return default;

        // Always the safe async path. The synchronous in-process ("fast") path is only valid
        // for JS functions that are synchronous; using it for a Promise-returning function
        // either throws on deserialization or silently fires-and-forgets. Callers that know
        // their JS function is synchronous opt in via InvokeVoidFast.
        return jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Opt-in fast invoke for VOID calls. Honors <see cref="BitButil.FastInvokeEnabled"/> and,
    /// when running under an <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly), calls the
    /// JS function synchronously.
    /// <br/>
    /// IMPORTANT: only use this for JS functions that are genuinely synchronous (no Promise).
    /// Using it for an async JS function loses awaiting and error propagation.
    /// </summary>
    internal static ValueTask InvokeVoidFast(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return InvokeVoidFast(jsRuntime, identifier, CancellationToken.None, args);
    }

    internal static ValueTask InvokeVoidFast(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsJsRuntimeInvalid()) return default;

        return BitButil.FastInvokeEnabled
            ? jsRuntime.FastInvokeVoidAsync(identifier, cancellationToken, args)
            : jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }



    /// <summary>
    /// Invokes a value-returning JavaScript function through the safe async path.
    /// </summary>
    /// <returns>
    /// The deserialized result, or <c>default(<typeparamref name="TValue"/>)</c> during static SSR /
    /// pre-render when no JS runtime is available.
    /// </returns>
    /// <remarks>
    /// IMPORTANT: because prerender returns <c>default</c> (e.g. <c>null</c>, <c>false</c>, <c>0</c>)
    /// instead of throwing, a caller can't distinguish a genuine value from "the runtime wasn't
    /// available". Code that branches on the result should treat the prerender pass accordingly
    /// (for example, by deferring the read to <c>OnAfterRender</c>). See <see cref="IsJsRuntimeInvalid"/>.
    /// </remarks>
    internal static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return Invoke<TValue>(jsRuntime, identifier, CancellationToken.None, args);
    }

    internal static async ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        // Async on purpose — see the note on the InvokeVoid timeout overload: the CTS timer
        // must outlive the call, which only happens if we await inside the using scope.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return await Invoke<TValue>(jsRuntime, identifier, cancellationToken, args);
    }

    internal static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        // Prerender/SSR: no runtime, so hand back default(TValue) rather than throwing. Callers
        // can't tell this apart from a real default — documented on the params-based overload.
        if (jsRuntime.IsJsRuntimeInvalid()) return default;

        // Always the safe async path — see the note on InvokeVoid. Callers whose JS function is
        // synchronous opt in via InvokeFast.
        return jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Opt-in fast invoke for value-returning calls. Honors <see cref="BitButil.FastInvokeEnabled"/>
    /// and, when running under an <see cref="IJSInProcessRuntime"/> (Blazor WebAssembly), calls the
    /// JS function synchronously.
    /// <br/>
    /// IMPORTANT: only use this for JS functions that are genuinely synchronous (no Promise).
    /// Invoking a Promise-returning function this way throws when the result can't be deserialized
    /// to <typeparamref name="TValue"/>.
    /// </summary>
    internal static ValueTask<TValue> InvokeFast<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return InvokeFast<TValue>(jsRuntime, identifier, CancellationToken.None, args);
    }

    internal static ValueTask<TValue> InvokeFast<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
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
    /// <br/>
    /// This runs on every JS call, so the (type-based, therefore stable) verdict is cached per
    /// runtime <see cref="Type"/> to avoid repeating the <c>GetType().Name</c> comparison and its
    /// string allocation on the hot path.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, bool> UnsupportedRuntimeCache = new();

    internal static bool IsJsRuntimeInvalid(this IJSRuntime? jsRuntime)
    {
        if (jsRuntime is null) return true;

        return UnsupportedRuntimeCache.GetOrAdd(jsRuntime.GetType(),
            // During pre-rendering ASP.NET injects an UnsupportedJavaScriptRuntime that throws on
            // every call. We special-case it by type name (the documented sentinel) to keep
            // prerender silent.
            static type => type.Name == "UnsupportedJavaScriptRuntime");
    }
}
