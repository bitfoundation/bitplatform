using System.Text.Json;
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
/// </remarks>
[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>", Scope = "member", Target = "~M:Bit.BlazorUI.IJSRuntimeFastExtensions.FastInvokeVoid(Microsoft.JSInterop.IJSRuntime,System.String,System.Threading.CancellationToken,System.Object[])~System.Threading.Tasks.ValueTask")]
public static class IJSRuntimeFastExtensions
{
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Optional handler for errors swallowed by the fast (in-process) invocation path on Blazor WebAssembly.
    /// </summary>
    /// <remarks>
    /// When a JSON (de)serialization error occurs while invoking JavaScript synchronously, the call is not
    /// re-thrown (the operation returns its default result) so a single bad interop call can't tear down the
    /// component. The error is reported here so it remains observable. Assign this once during application
    /// startup to route the report to a real logger (e.g. an <c>ILogger</c>); when left <see langword="null"/>
    /// the error is written to <see cref="System.Console.Error"/> to preserve the previous behavior.
    /// The handler receives the invoked identifier and the caught exception, and must not throw.
    /// </remarks>
    public static Action<string, Exception>? OnError { get; set; }

    private static void ReportError(string identifier, Exception exception)
    {
        var handler = OnError;
        if (handler is null)
        {
            System.Console.Error.WriteLine($"Error invoking '{identifier}' using {nameof(IJSInProcessRuntime)}. A JSON-related issue occurred: {exception.Message}.");
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
        // check. It does not cover "the JS function isn't loaded yet" - that still surfaces as an error.
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            try
            {
                jsInProcessRuntime.Invoke<IJSVoidResult>(identifier, args);
                return ValueTask.CompletedTask;
            }
            catch (JsonException ex)
            {
                ReportError(identifier, ex);
                return ValueTask.CompletedTask;
            }
        }
        else
        {
            return jsRuntime.InvokeVoid(identifier, cancellationToken, args);
        }
    }



    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// Note: In Blazor WebAssembly mode, use this method only for synchronous JavaScript functions.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public static ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return FastInvoke<TValue>(jsRuntime, identifier, CancellationToken.None, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// Note: In Blazor WebAssembly mode, use this method only for synchronous JavaScript functions.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public static async ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        // The CancellationTokenSource must stay alive until the underlying invocation completes.
        // Awaiting here (instead of returning the ValueTask) keeps the `using` scope open for the
        // full duration of the async path, so the timeout can actually fire and the source isn't
        // disposed while a callback is still registered on its token.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return await FastInvoke<TValue>(jsRuntime, identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// Note: In Blazor WebAssembly mode, use this method only for synchronous JavaScript functions.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public static ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        // See FastInvokeVoid for the rationale: keep the synchronous in-process path and the async
        // fallback consistent by skipping the call when the runtime can't service interop.
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.FromResult(default(TValue)!);

        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            try
            {
                return ValueTask.FromResult(jsInProcessRuntime.Invoke<TValue>(identifier, args));
            }
            catch (JsonException ex)
            {
                ReportError(identifier, ex);
                return ValueTask.FromResult(default(TValue)!);
            }
        }
        else
        {
            return jsRuntime.Invoke<TValue>(identifier, cancellationToken, args);
        }
    }
}
