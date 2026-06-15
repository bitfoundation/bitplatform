using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection;
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
        // Async on purpose - see the note on the InvokeVoid timeout overload: the CTS timer
        // must outlive the call, which only happens if we await inside the using scope.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return await Invoke<TValue>(jsRuntime, identifier, cancellationToken, args);
    }

    internal static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        // Prerender/SSR: no runtime, so hand back default(TValue) rather than throwing. Callers
        // can't tell this apart from a real default - documented on the params-based overload.
        if (jsRuntime.IsJsRuntimeInvalid()) return default;

        // Always the safe async path - see the note on InvokeVoid. Callers whose JS function is
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
    /// True for exceptions that are safe to swallow while tearing down a wrapper (its
    /// <c>DisposeAsync</c> / teardown path). During teardown a JS interop call can surface as more
    /// than just a <see cref="JSDisconnectedException"/>:
    /// <list type="bullet">
    /// <item><see cref="JSDisconnectedException"/> - the circuit/runtime is already gone.</item>
    /// <item><see cref="OperationCanceledException"/> (including <see cref="TaskCanceledException"/>,
    /// which derives from it) - the default invoke path uses a timed
    /// <see cref="CancellationTokenSource"/>, so a teardown that races the timeout can cancel.</item>
    /// <item><see cref="ObjectDisposedException"/> - the runtime/circuit has already been disposed
    /// out from under the call.</item>
    /// </list>
    /// None of these are actionable while disposing, so callers can ignore them.
    /// </summary>
    internal static bool IsIgnorableDisposalException(this Exception exception)
        => exception is JSDisconnectedException
            or OperationCanceledException
            or ObjectDisposedException;

    /// <summary>
    /// Returns true when calling into JavaScript right now would either be impossible
    /// (no runtime / pre-render) or guaranteed to fail (circuit not yet initialized).
    /// </summary>
    /// <remarks>
    /// There are three runtimes that can reject a JS call even though they look like a normal
    /// <see cref="IJSRuntime"/>, and each needs a different signal:
    /// <list type="bullet">
    /// <item><c>UnsupportedJavaScriptRuntime</c> - injected during static SSR / pre-render; it
    /// throws on every call. Detected purely by type name, so the verdict is stable per type.</item>
    /// <item><c>RemoteJSRuntime</c> (Blazor Server) - throws an <see cref="InvalidOperationException"/>
    /// ("JavaScript interop calls cannot be issued at this time…") while the component is being
    /// prerendered, i.e. while <c>IsInitialized</c> is <c>false</c>. This is NOT a
    /// <see cref="JSDisconnectedException"/>, so we must guard it here; otherwise a read in
    /// <c>OnInitializedAsync</c> that used to yield <c>default</c> would now throw.</item>
    /// <item><c>WebViewJSRuntime</c> (Blazor Hybrid) - throws during the window between construction
    /// and the WebView attaching, i.e. while its <c>_ipcSender</c> is <c>null</c>.</item>
    /// </list>
    /// Because <c>IsInitialized</c>/<c>_ipcSender</c> flip from "not ready" to "ready" over the
    /// lifetime of a single instance, the verdict for those two runtimes can't be cached by type -
    /// only the classification (which runtime kind this is) is cached. The reflected member accessor
    /// is cached too, so the hot path is one dictionary lookup plus a single property/field read.
    /// <br/>
    /// We reflect over a public property (<c>IsInitialized</c>) and a private field
    /// (<c>_ipcSender</c>) whose internals have shifted across .NET releases. If either ever
    /// disappears we fail open (treat the runtime as ready) and let any genuine error surface at
    /// the call site, rather than throwing here or silently swallowing every call.
    /// </remarks>
    private enum JsRuntimeKind : byte
    {
        Operational,    // Blazor WASM, or any runtime we treat as always-ready
        Unsupported,    // static SSR / pre-render sentinel
        RemoteServer,   // Blazor Server: ready only once the circuit IsInitialized
        WebViewHybrid   // Blazor Hybrid: ready only once the IPC channel is attached
    }

    private static readonly ConcurrentDictionary<Type, JsRuntimeKind> RuntimeKindCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IsInitializedCache = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo?> IpcSenderCache = new();

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Reflected members belong to framework JS runtime types that are always present at runtime; we fail open if a member is trimmed/renamed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflected members belong to framework JS runtime types that are always present at runtime; we fail open if a member is trimmed/renamed.")]
    internal static bool IsJsRuntimeInvalid(this IJSRuntime? jsRuntime)
    {
        if (jsRuntime is null) return true;

        var type = jsRuntime.GetType();

        var kind = RuntimeKindCache.GetOrAdd(type, static t => t.Name switch
        {
            "UnsupportedJavaScriptRuntime" => JsRuntimeKind.Unsupported, // Prerendering
            "RemoteJSRuntime" => JsRuntimeKind.RemoteServer,             // Blazor Server
            "WebViewJSRuntime" => JsRuntimeKind.WebViewHybrid,           // Blazor Hybrid
            _ => JsRuntimeKind.Operational                              // Blazor WASM
        });

        switch (kind)
        {
            case JsRuntimeKind.Unsupported:
                return true;

            case JsRuntimeKind.RemoteServer:
                // Server circuit is unusable until IsInitialized becomes true (after prerender).
                var isInitialized = IsInitializedCache.GetOrAdd(type,
                    static t => t.GetProperty("IsInitialized", BindingFlags.Public | BindingFlags.Instance));
                return isInitialized?.GetValue(jsRuntime) is false;

            case JsRuntimeKind.WebViewHybrid:
                // Hybrid runtime is unusable until the WebView attaches its IPC sender.
                var ipcSender = IpcSenderCache.GetOrAdd(type,
                    static t => t.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance));
                return ipcSender is not null && ipcSender.GetValue(jsRuntime) is null;

            default:
                return false;
        }
    }
}
