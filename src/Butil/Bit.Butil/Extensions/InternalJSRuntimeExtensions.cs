using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        if (BitButil.LazyScriptsEnabled) return InvokeVoidLazy(jsRuntime, identifier, cancellationToken, args);

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

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The fast path forwards to FastInvokeVoidAsync (annotated [RequiresUnreferencedCode]) but only ever passes trim-safe primitives from the opted-in synchronous APIs. The real protection - the attribute - stays on the public FastInvoke* surface so a trimming/AOT consumer still gets the warning at their call site; this suppresses only the redundant internal propagation.")]
    internal static ValueTask InvokeVoidFast(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsJsRuntimeInvalid()) return default;
        if (BitButil.LazyScriptsEnabled) return InvokeVoidFastLazy(jsRuntime, identifier, cancellationToken, args);

        return BitButil.FastInvokeEnabled
            ? jsRuntime.FastInvokeVoidAsync(identifier, cancellationToken, args)
            : jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }



    /// <summary>
    /// Invokes a value-returning JavaScript function through the safe async path.
    /// </summary>
    /// <returns>
    /// The deserialized result, or a safe default during static SSR / pre-render when no JS runtime
    /// is available. The safe default is an empty string for <see cref="string"/>, an empty array for
    /// array types, and <c>default(<typeparamref name="TValue"/>)</c> for everything else.
    /// </returns>
    /// <remarks>
    /// IMPORTANT: because prerender returns a safe default (e.g. <c>""</c>, <c>[]</c>, <c>false</c>,
    /// <c>0</c>) instead of throwing, a caller can't distinguish a genuine value from "the runtime
    /// wasn't available". Code that branches on the result should treat the prerender pass accordingly
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
        // Prerender/SSR: no runtime, so hand back a safe default rather than throwing. For string
        // and array return types that means an empty string / empty array (never null) so callers
        // that read in OnInitializedAsync don't hit a NullReferenceException; everything else gets
        // default(TValue). Callers still can't tell this apart from a real empty/default value -
        // documented on the params-based overload.
        if (jsRuntime.IsJsRuntimeInvalid()) return SafeDefault<TValue>();

        // Always the safe async path - see the note on InvokeVoid. Callers whose JS function is
        // synchronous opt in via InvokeFast.
        if (BitButil.LazyScriptsEnabled) return InvokeLazy<TValue>(jsRuntime, identifier, cancellationToken, args);

        return jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    /// <summary>
    /// The value handed back when the runtime is invalid (prerender/SSR). Reference types that are
    /// routinely dereferenced - <see cref="string"/> and arrays - become empty instead of
    /// <c>null</c> to avoid surprise <see cref="NullReferenceException"/>s; all other types fall
    /// back to <c>default(TValue)</c>. The computed value is cached per <typeparamref name="TValue"/>
    /// so repeated prerender calls don't re-run the type inspection / array allocation.
    /// </summary>
    private static ValueTask<TValue> SafeDefault<TValue>() => new(SafeDefaultHolder<TValue>.Value);

    private static class SafeDefaultHolder<TValue>
    {
        internal static readonly TValue Value = Create();

        [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Array.CreateInstance with a concrete element type is AOT-safe; no members are reflected over.")]
        private static TValue Create()
        {
            var type = typeof(TValue);

            if (type == typeof(string))
                return (TValue)(object)string.Empty;

            if (type.IsArray)
                return (TValue)(object)Array.CreateInstance(type.GetElementType()!, 0);

            return default!;
        }
    }

    /// <summary>
    /// Invokes a JS listener-registration function - one that reports whether it actually attached
    /// by returning a boolean - and tells the caller whether the registration took.
    /// </summary>
    /// <remarks>
    /// Plain <see cref="Invoke{TValue}(IJSRuntime, string, object?[])"/> can't be used for this:
    /// during prerender/SSR it hands back <c>default(bool)</c>, which is indistinguishable from the
    /// JS side refusing to attach. Nothing is registered during prerender and nothing failed either
    /// - the caller's subscription is simply inert until the app is interactive - so this reports
    /// success there, and only a real <c>false</c> from JS means "the listener is not attached".
    /// </remarks>
    internal static async ValueTask<bool> InvokeRegister(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsJsRuntimeInvalid()) return true;

        return await jsRuntime.Invoke<bool>(identifier, args);
    }

    /// <summary>
    /// <see cref="InvokeRegister"/> for a registration function that reports failure by returning
    /// the reason as a string, and null when it attached. The reason travels back with the call
    /// rather than through the error callback, so the caller raises it exactly once and can put the
    /// real message on the exception it throws.
    /// </summary>
    /// <remarks>Prerender/SSR reports success, for the reason given on <see cref="InvokeRegister"/>.</remarks>
    internal static async ValueTask<string?> InvokeRegisterOrError(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsJsRuntimeInvalid()) return null;

        return await jsRuntime.Invoke<string?>(identifier, args);
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

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The fast path forwards to FastInvokeAsync (annotated [RequiresUnreferencedCode]) but only ever passes trim-safe primitives from the opted-in synchronous APIs. The real protection - the attribute - stays on the public FastInvoke* surface so a trimming/AOT consumer still gets the warning at their call site; this suppresses only the redundant internal propagation.")]
    internal static ValueTask<TValue> InvokeFast<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsJsRuntimeInvalid()) return SafeDefault<TValue>();
        if (BitButil.LazyScriptsEnabled) return InvokeFastLazy<TValue>(jsRuntime, identifier, cancellationToken, args);

        return BitButil.FastInvokeEnabled
            ? jsRuntime.FastInvokeAsync<TValue>(identifier, cancellationToken, args)
            : jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }


    // The lazy-scripts variants of the four entry points above: the same calls, preceded by making sure the
    // module behind the identifier has been imported (see ButilScriptLoader). Separate async methods so the
    // default path stays free of a state machine and lazy mode pays for it only until the module is loaded
    // (EnsureLoaded completes synchronously from then on).

    private static async ValueTask InvokeVoidLazy(IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        await ButilScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken);
        await jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Same forwarding as InvokeVoidFast: only trim-safe primitives from the opted-in synchronous APIs reach the fast path.")]
    private static async ValueTask InvokeVoidFastLazy(IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        await ButilScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken);

        if (BitButil.FastInvokeEnabled) await jsRuntime.FastInvokeVoidAsync(identifier, cancellationToken, args);
        else await jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }

    private static async ValueTask<TValue> InvokeLazy<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        await ButilScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken);
        return await jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Same forwarding as InvokeFast: only trim-safe primitives from the opted-in synchronous APIs reach the fast path.")]
    private static async ValueTask<TValue> InvokeFastLazy<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        await ButilScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken);

        return BitButil.FastInvokeEnabled
            ? await jsRuntime.FastInvokeAsync<TValue>(identifier, cancellationToken, args)
            : await jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }


    /// <summary>
    /// Bridges a <see cref="CancellationToken"/> to a JavaScript-side abort: when the token fires, the
    /// given interop function is invoked (fire-and-forget) with the given arguments. Returns the
    /// registration to dispose once the call it guards has settled - or <c>default</c> when the token
    /// can never be cancelled, so a <see cref="CancellationToken.None"/> caller pays nothing.
    /// </summary>
    /// <remarks>
    /// One implementation for every cancellable wrapper (<see cref="Fetch"/>, <see cref="WebOtp"/>,
    /// <see cref="DigitalCredentials"/>), because the callback runs in whatever context cancels the
    /// token - a component's <c>Dispose</c>, a timer thread - and has nothing to hand an exception to.
    /// A circuit that is already gone makes the interop call throw or fault; both are swallowed here,
    /// the way any teardown-time interop failure is, rather than surfacing out of the caller's
    /// <c>CancellationTokenSource.Cancel()</c> or as an unobserved task exception. An already-cancelled
    /// token runs the callback synchronously inside this method, which is what dispatches the abort
    /// <em>before</em> the call it belongs to - the JavaScript side holds such an abort against the
    /// call's handle (see <c>abortable.ts</c>).
    /// </remarks>
    internal static CancellationTokenRegistration RegisterJsAbort(this IJSRuntime jsRuntime, CancellationToken cancellationToken, string identifier, params object?[]? args)
    {
        if (cancellationToken.CanBeCanceled is false) return default;

        return cancellationToken.Register(static state =>
        {
            var (js, id, arguments) = ((IJSRuntime, string, object?[]?))state!;
            try
            {
                var pending = js.InvokeVoid(id, arguments);
                if (pending.IsCompletedSuccessfully is false) _ = Observe(pending);
            }
            catch (Exception exception) when (exception.IsIgnorableDisposalException()) { }
        }, (jsRuntime, identifier, args));

        // Awaited only to observe a failure: the abort itself is the whole of the work, and there is no
        // caller left to report it to.
        static async Task Observe(ValueTask pending)
        {
            try { await pending; }
            catch (Exception exception) when (exception.IsIgnorableDisposalException() || exception is JSException) { }
        }
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

    // Once a Server/Hybrid runtime instance reports "ready", it never reverts (IsInitialized and
    // _ipcSender only flip not-ready -> ready over an instance's lifetime). Remembering the ready
    // verdict per instance lets the hot path short-circuit the per-call reflection read - we only
    // reflect until the first "ready", then never again for that instance. A ConditionalWeakTable
    // keys on the runtime instance without keeping it alive.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IJSRuntime, object> ReadyRuntimes = new();
    private static readonly object ReadyMarker = new();

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
            case JsRuntimeKind.WebViewHybrid:
                // If we've already seen this instance become ready, skip the reflection entirely.
                if (ReadyRuntimes.TryGetValue(jsRuntime, out _)) return false;

                bool ready;
                if (kind == JsRuntimeKind.RemoteServer)
                {
                    // Server circuit is unusable until IsInitialized becomes true (after prerender).
                    var isInitialized = IsInitializedCache.GetOrAdd(type,
                        static t => t.GetProperty("IsInitialized", BindingFlags.Public | BindingFlags.Instance));
                    ready = isInitialized?.GetValue(jsRuntime) is not false;
                }
                else
                {
                    // Hybrid runtime is unusable until the WebView attaches its IPC sender.
                    var ipcSender = IpcSenderCache.GetOrAdd(type,
                        static t => t.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance));
                    ready = ipcSender is null || ipcSender.GetValue(jsRuntime) is not null;
                }

                if (ready) ReadyRuntimes.AddOrUpdate(jsRuntime, ReadyMarker);
                return ready is false;

            default:
                return false;
        }
    }
}
