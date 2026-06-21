using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using static Bit.BlazorUI.JsInteropConstants;

namespace Bit.BlazorUI;

/// <summary>
/// Extension methods that invoke JavaScript with the least possible overhead by using the synchronous
/// <see cref="IJSInProcessRuntime"/> path when it is available (Blazor WebAssembly), and falling back to the
/// regular asynchronous invocation otherwise (Blazor Server, Hybrid, and prerendering).
/// </summary>
/// <remarks>
/// Threading caveat (multithreaded WebAssembly): the synchronous in-process path uses
/// <see cref="IJSInProcessRuntime"/>, which on a multithreaded WebAssembly runtime
/// (<c>&lt;WasmEnableThreads&gt;</c>) can only be used on the main thread. Invoking these methods from a
/// background/pool thread (for example after <c>ConfigureAwait(false)</c>) would throw in that configuration.
/// Default single-threaded WebAssembly is unaffected. Call these methods on the renderer's synchronization
/// context (the default for component lifecycle and event callbacks).
/// A best-effort <c>DEBUG</c>-only assertion (see <c>AssertInProcessInteropThread</c>) flags the common
/// thread-pool case during development; it is compiled out of shipping builds so the fast path stays
/// branch-free, and the framework still throws its own exception in that configuration at runtime.
/// </remarks>
[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Interop arguments are JSON-serializable types owned by the framework/components; the JSON metadata they require is preserved by the [DynamicallyAccessedMembers(JsonSerialized)] annotations on the generic overloads and by the component models themselves, so the void path is safe to invoke under trimming.", Scope = "member", Target = "~M:Bit.BlazorUI.IJSRuntimeFastExtensions.FastInvokeVoid(Microsoft.JSInterop.IJSRuntime,System.String,System.Threading.CancellationToken,System.Object[])~System.Threading.Tasks.ValueTask")]
public static class IJSRuntimeFastExtensions
{

    /// <summary>
    /// Optional handler for errors swallowed by the fast (in-process) invocation path on Blazor WebAssembly,
    /// and for unexpected-null results reported via <see cref="ReportIfUnexpectedNull{T}"/> on any runtime.
    /// </summary>
    /// <remarks>
    /// When a <see cref="JsonException"/> or <see cref="JSException"/> occurs while invoking JavaScript
    /// synchronously, the call is not re-thrown (the operation returns its default result) so a single bad
    /// interop call can't tear down the component, and the error is reported here so it remains observable.
    /// <para>
    /// Important: <see cref="JSException"/> covers more than a missing/unwired function. A correctly wired
    /// synchronous JavaScript function whose own body throws (a real logic bug in the script) surfaces as the
    /// same <see cref="JSException"/> and is therefore swallowed too - the call returns its default result and
    /// the only signal is this report. Such errors previously propagated and tore down the call loudly; on the
    /// fast path they no longer do. The default reporter (see <c>ReportError</c>) classifies the cause - a
    /// missing/unwired function, an error thrown inside the function (a script bug), a serialization issue, or
    /// an unexpected null - so the distinction is preserved in the message even without a custom handler.
    /// Because of this, wiring <see cref="OnError"/> to a real logger should be treated as required in
    /// production, not optional - it is the single channel through which both missing functions and in-body
    /// logic errors remain visible.
    /// </para>
    /// <para>
    /// This is a <em>process-global</em> hook. Assign it exactly once during application startup (before any
    /// component renders) to route the report to a real logger (e.g. an <c>ILogger</c>); when left
    /// <see langword="null"/> the error is written to <see cref="System.Console.Error"/> and a one-time warning
    /// is emitted recommending that a handler be wired. The setter is not synchronized and the handler is shared
    /// by every consumer in the process, so on Blazor Server it cannot carry per-circuit/per-user context -
    /// capture any ambient state inside the handler accordingly. The handler receives the invoked identifier and
    /// the caught exception, and must not throw (a throwing handler is caught and ignored).
    /// </para>
    /// </remarks>
    public static Action<string, Exception>? OnError { get; set; }

    // Tracks whether the "no OnError handler wired" warning has already been emitted, so the default reporter
    // nags about observability exactly once instead of flooding the console on every swallowed error.
    private static int _missingHandlerWarned;

    private static void ReportError(string identifier, Exception exception)
    {
        var handler = OnError;
        if (handler is null)
        {
            System.Console.Error.WriteLine(
                $"Error invoking '{identifier}': {DescribeCause(exception)} {exception.Message}");

            // Emit the "wire up OnError" guidance once. Without a handler these errors (including genuine
            // in-body script bugs) are only visible on the console, so surface the recommendation a single time.
            if (Interlocked.Exchange(ref _missingHandlerWarned, 1) == 0)
            {
                System.Console.Error.WriteLine(
                    $"{nameof(IJSRuntimeFastExtensions)}.{nameof(OnError)} is not set. Swallowed fast-invoke interop " +
                    "errors - including JavaScript functions that throw in their own body - are only reported to the " +
                    "console. Assign a handler during startup to route them to a real logger.");
            }

            return;
        }

        try
        {
            handler(identifier, exception);
        }
        catch
        {
            // A faulty error handler must never escape the interop call and break the caller.
        }
    }

    // Classifies a swallowed interop failure so the default console report distinguishes a missing/unwired
    // function from a real script bug (a present function that threw), a serialization problem, or an
    // unexpected null. This keeps the most actionable case - an in-body logic error - from hiding behind the
    // same generic text as a simple typo'd identifier.
    private static string DescribeCause(Exception exception) => exception switch
    {
        JsonException => "a JSON serialization issue occurred:",
        JSException jsException when IsMissingFunctionError(jsException) => "the JavaScript function is missing or unwired:",
        JSException => "the JavaScript function threw while executing (a script bug):",
        InvalidOperationException => "the interop call produced an unexpected result:",
        _ => "an unexpected error occurred:",
    };

    // Heuristic that recognizes the framework's "function not found" JSException by the phrasing the
    // in-process runtime uses. It is intentionally conservative: when it can't tell, the cause is reported as
    // an in-body script error (the louder, more actionable classification) rather than as a missing function.
    private static bool IsMissingFunctionError(JSException jsException)
    {
        var message = jsException.Message;
        return message.Contains("is not a function", StringComparison.OrdinalIgnoreCase)
            || message.Contains("was undefined", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Could not find", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// DEBUG-only guard for the synchronous in-process interop path. On a multithreaded WebAssembly runtime
    /// (<c>&lt;WasmEnableThreads&gt;</c>) <see cref="IJSInProcessRuntime"/> may only be used on the main thread,
    /// so running it from a thread-pool (background) thread - the usual outcome of a preceding
    /// <c>ConfigureAwait(false)</c> - throws. This fails fast with an actionable message ahead of the
    /// framework's lower-level exception. It is a heuristic: it covers the thread-pool case (not custom
    /// background threads) and only runs in the browser, where single-threaded WASM has no thread pool so it
    /// never false-fires. Marked <c>[Conditional("DEBUG")]</c> so the call and its argument evaluation are
    /// removed entirely from shipping builds, keeping the fast path overhead-free.
    /// </summary>
    [Conditional("DEBUG")]
    private static void AssertInProcessInteropThread(string identifier)
    {
        // Format the message only on failure: Debug.Assert(bool, string) evaluates its message argument
        // eagerly, and these methods are hot paths, so building the string on every passing call would
        // add needless allocations to DEBUG builds.
        if (OperatingSystem.IsBrowser() && Thread.CurrentThread.IsThreadPoolThread)
        {
            Debug.Fail(
                $"FastInvoke('{identifier}') ran synchronous in-process JS interop on a thread-pool thread. " +
                "On multithreaded WebAssembly (<WasmEnableThreads>) this is only valid on the main thread and will throw. " +
                "Invoke it on the renderer's synchronization context (component lifecycle/event callbacks) without a " +
                "preceding ConfigureAwait(false), or use the regular asynchronous invocation instead.");
        }
    }

    /// <summary>
    /// When a FastInvoke call returns <see langword="null"/> and the runtime can service interop, reports the
    /// missing result so component setup (drag/drop, pointer handlers, JS disposables, etc.) is not completely
    /// silent. Skipped when <see cref="IJSRuntimeExtensions.IsRuntimeInvalid"/> is <see langword="true"/> (prerender,
    /// disconnected circuit) where <see langword="null"/> is expected.
    /// </summary>
    /// <remarks>
    /// Scope (deliberate asymmetry): the <c>where T : class</c> constraint means this only covers reference-type
    /// results (<see cref="IJSObjectReference"/>, <see cref="string"/>, <c>BitFileInfo[]</c>). Value-type returns
    /// that call sites request as nullable (<c>bool?</c>, <c>double?</c>, <c>float?</c>, <c>decimal?</c> in
    /// Splitter, Utils, Chart, MarkdownViewer) are not routed through here; they are coalesced at the call site
    /// (<c>?? 0</c>, <c>is true</c>), so a valid runtime that returns <see langword="null"/> <em>without</em> an
    /// exception is silently treated as the default value rather than reported. This gap is narrow: the
    /// <see cref="IJSRuntimeExtensions.IsRuntimeInvalid"/> case is legitimately silent for both reference and value
    /// types (the <see langword="null"/> is expected), and a swallowed <see cref="JsonException"/> or
    /// <see cref="JSException"/> is still reported via <see cref="OnError"/> for both. The only unreported case for
    /// value types is "valid runtime, no exception, but the result was <see langword="null"/>", which is accepted
    /// because a nullable value-type return is already distinguishable from a legitimate <c>false</c>/zero at the
    /// call site (see the <c>FastInvoke</c> return remarks).
    /// </remarks>
    /// <remarks>
    /// Only call this for identifiers whose JavaScript function is expected to <em>always</em> return a non-null
    /// reference on a healthy runtime (e.g. <c>setup</c>/<c>init</c> helpers that hand back an id or a JS object
    /// reference). A function that can legitimately return <see langword="null"/> on a valid runtime would be
    /// reported here as a false positive; route those through a plain <c>FastInvoke</c> without this helper.
    /// The report is raised as an <see cref="InvalidOperationException"/> so a wired <see cref="OnError"/> handler
    /// can filter unexpected-null reports apart from <see cref="JSException"/>/<see cref="JsonException"/> ones.
    /// </remarks>
    public static T? ReportIfUnexpectedNull<T>(this IJSRuntime jsRuntime, string identifier, T? result)
        where T : class
    {
        if (result is not null || jsRuntime.IsRuntimeInvalid()) return result;

        ReportError(identifier, new InvalidOperationException("The interop call completed without a result."));
        return null;
    }



    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <remarks>
    /// In Blazor WebAssembly the call runs synchronously through <see cref="IJSInProcessRuntime"/>, so the
    /// target JavaScript function must be synchronous. Targeting an asynchronous (Promise-returning) function
    /// turns this into a fire-and-forget call: the caller continues before the work finishes and any error is lost.
    /// Use the regular asynchronous invocation for asynchronous JavaScript functions.
    /// </remarks>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    public static ValueTask FastInvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return FastInvokeVoid(jsRuntime, identifier, CancellationToken.None, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <remarks>
    /// In Blazor WebAssembly the call runs synchronously through <see cref="IJSInProcessRuntime"/>, so the
    /// target JavaScript function must be synchronous. Targeting an asynchronous (Promise-returning) function
    /// turns this into a fire-and-forget call: the caller continues before the work finishes and any error is lost.
    /// Use the regular asynchronous invocation for asynchronous JavaScript functions.
    /// </remarks>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    public static async ValueTask FastInvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        // The CancellationTokenSource must stay alive until the underlying invocation completes.
        // Awaiting here (instead of returning the ValueTask) keeps the `using` scope open for the
        // full duration of the async path, so the timeout can actually fire and the source isn't
        // disposed while a callback is still registered on its token.
        // Note: the timeout only has an effect on the asynchronous fallback path (Server/Hybrid). On
        // WebAssembly the call runs synchronously through IJSInProcessRuntime, which ignores the token, so
        // the CancellationTokenSource is created but never observed there.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        await FastInvokeVoid(jsRuntime, identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <remarks>
    /// In Blazor WebAssembly the call runs synchronously through <see cref="IJSInProcessRuntime"/>, so the
    /// target JavaScript function must be synchronous. Targeting an asynchronous (Promise-returning) function
    /// turns this into a fire-and-forget call: the caller continues before the work finishes and any error is lost.
    /// Use the regular asynchronous invocation for asynchronous JavaScript functions.
    /// </remarks>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    public static ValueTask FastInvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        // Hold both invocation paths to the same contract: skip the call when the runtime can't service
        // interop (prerendering, an uninitialized Blazor Server circuit, or a disposed WebView). The async
        // fallback already guards this inside InvokeVoid; checking here means the synchronous in-process
        // path no-ops too instead of throwing. For a normal WASM runtime this is a cheap, false-returning
        // check. A missing JS function - and any error thrown inside a present synchronous JS function - then
        // surfaces as a swallowed <see cref="JSException"/> routed through <see cref="OnError"/> rather than
        // an unhandled synchronous throw.
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            AssertInProcessInteropThread(identifier);
            try
            {
                jsInProcessRuntime.Invoke<IJSVoidResult>(identifier, args);
                return ValueTask.CompletedTask;
            }
            // Swallows both a missing/unwired function and an error thrown inside a present synchronous JS
            // function (a real script bug) - they arrive as the same JSException. See OnError remarks.
            catch (Exception ex) when (ex is JsonException or JSException)
            {
                ReportError(identifier, ex);
                return ValueTask.CompletedTask;
            }
        }
        else
        {
            // The runtime-validity guard already ran above, so skip the re-checking bit InvokeVoid
            // extension and call the framework's asynchronous interop directly.
            return jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
        }
    }



    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <remarks>
    /// In Blazor WebAssembly the call runs synchronously through <see cref="IJSInProcessRuntime"/>, so the
    /// target JavaScript function must be synchronous. Targeting an asynchronous (Promise-returning) function
    /// turns this into a fire-and-forget call: the caller continues before the work finishes and any error is lost.
    /// Use the regular asynchronous invocation for asynchronous JavaScript functions.
    /// </remarks>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>
    /// An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.
    /// When <typeparamref name="TValue"/> is a non-nullable value type, prefer a nullable form (e.g. <see cref="bool"/> → <see cref="Nullable{T}"/>)
    /// at call sites so a swallowed interop error (<see langword="default"/>, which is <see langword="null"/> for nullable value types)
    /// is distinguishable from a legitimate JavaScript return of <see langword="false"/> or zero.
    /// </returns>
    public static ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return FastInvoke<TValue>(jsRuntime, identifier, CancellationToken.None, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <remarks>
    /// In Blazor WebAssembly the call runs synchronously through <see cref="IJSInProcessRuntime"/>, so the
    /// target JavaScript function must be synchronous. Targeting an asynchronous (Promise-returning) function
    /// turns this into a fire-and-forget call: the caller continues before the work finishes and any error is lost.
    /// Use the regular asynchronous invocation for asynchronous JavaScript functions.
    /// </remarks>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <inheritdoc cref="FastInvoke{TValue}(IJSRuntime, string, object?[])"/>
    public static async ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        // The CancellationTokenSource must stay alive until the underlying invocation completes.
        // Awaiting here (instead of returning the ValueTask) keeps the `using` scope open for the
        // full duration of the async path, so the timeout can actually fire and the source isn't
        // disposed while a callback is still registered on its token.
        // Note: the timeout only has an effect on the asynchronous fallback path (Server/Hybrid). On
        // WebAssembly the call runs synchronously through IJSInProcessRuntime, which ignores the token, so
        // the CancellationTokenSource is created but never observed there.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return await FastInvoke<TValue>(jsRuntime, identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <remarks>
    /// In Blazor WebAssembly the call runs synchronously through <see cref="IJSInProcessRuntime"/>, so the
    /// target JavaScript function must be synchronous. Targeting an asynchronous (Promise-returning) function
    /// turns this into a fire-and-forget call: the caller continues before the work finishes and any error is lost.
    /// Use the regular asynchronous invocation for asynchronous JavaScript functions.
    /// </remarks>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <inheritdoc cref="FastInvoke{TValue}(IJSRuntime, string, object?[])"/>
    public static ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        // See FastInvokeVoid for the rationale: keep the synchronous in-process path and the async
        // fallback consistent by skipping the call when the runtime can't service interop.
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.FromResult(default(TValue)!);

        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            AssertInProcessInteropThread(identifier);
            try
            {
                return ValueTask.FromResult(jsInProcessRuntime.Invoke<TValue>(identifier, args));
            }
            // Swallows both a missing/unwired function and an error thrown inside a present synchronous JS
            // function (a real script bug) - they arrive as the same JSException. See OnError remarks.
            catch (Exception ex) when (ex is JsonException or JSException)
            {
                ReportError(identifier, ex);
                return ValueTask.FromResult(default(TValue)!);
            }
        }
        else
        {
            // The runtime-validity guard already ran above, so skip the re-checking bit Invoke
            // extension and call the framework's asynchronous interop directly.
            return jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }
    }
}
