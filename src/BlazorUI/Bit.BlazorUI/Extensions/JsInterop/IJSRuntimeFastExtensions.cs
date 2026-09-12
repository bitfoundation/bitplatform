using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop.Infrastructure;

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
    /// The set of <see cref="DynamicallyAccessedMemberTypes"/> required to preserve the JSON metadata of types
    /// that are serialized/deserialized across JS interop, so they survive trimming.
    /// </summary>
    /// <remarks>
    /// Kept as a public member of this class for source compatibility with consumers that reference it;
    /// <see cref="JsInteropConstants.JsonSerialized"/> is the single definition of the value.
    /// </remarks>
    public const DynamicallyAccessedMemberTypes JsonSerialized = JsInteropConstants.JsonSerialized;



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
        // check. Only a JSON problem is swallowed below: a missing function, or an error thrown inside the
        // JavaScript function, propagates as a JSException on this path exactly as it does on the async one,
        // so a component behaves the same on every host.
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            AssertInProcessInteropThread(identifier);
            try
            {
                jsInProcessRuntime.Invoke<IJSVoidResult>(identifier, args);
                return ValueTask.CompletedTask;
            }
            catch (JsonException ex)
            {
                System.Console.Error.WriteLine($"Error invoking '{identifier}' using {nameof(IJSInProcessRuntime)}. A JSON-related issue occurred: {ex.Message}.");
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
    /// An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value, or
    /// <see langword="default"/> when the runtime cannot service interop (prerendering, a circuit that is not
    /// initialized, a detached WebView) - the same answer the asynchronous invocation gives in that state.
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
            catch (JsonException ex)
            {
                System.Console.Error.WriteLine($"Error invoking '{identifier}' using {nameof(IJSInProcessRuntime)}. A JSON-related issue occurred: {ex.Message}.");
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
