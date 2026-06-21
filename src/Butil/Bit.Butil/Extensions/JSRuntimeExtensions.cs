using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

public static class JSRuntimeExtensions
{
    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// Note: In Blazor WebAssembly mode, use this method only for synchronous JavaScript functions.
    /// </summary>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TResult"/> obtained by JSON-deserializing the return value.</returns>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TResult> FastInvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return FastInvokeAsync<TResult>(jsRuntime, identifier, CancellationToken.None, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// Note: In Blazor WebAssembly mode, use this method only for synchronous JavaScript functions.
    /// </summary>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TResult"/> obtained by JSON-deserializing the return value.</returns>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static async ValueTask<TResult> FastInvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        // Must be async: the CancellationTokenSource's timer enforces the timeout and has to
        // remain alive until the call finishes. Disposing it synchronously (as a non-async
        // method would) cancels the timer and defeats the timeout.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return await FastInvokeAsync<TResult>(jsRuntime, identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// Note: In Blazor WebAssembly mode, use this method only for synchronous JavaScript functions.
    /// </summary>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TResult"/> obtained by JSON-deserializing the return value.</returns>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TResult> FastInvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            // We deliberately do not catch JsonException here. Calling the synchronous
            // Invoke<T> against a JS function that returns a Promise produces a JSON
            // payload that cannot deserialize to TResult; surfacing the error makes the
            // mistake visible instead of silently returning default(TResult).
            return ValueTask.FromResult(jsInProcessRuntime.Invoke<TResult>(identifier, args));
        }

        return jsRuntime.InvokeAsync<TResult>(identifier, cancellationToken, args);
    }


    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask FastInvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return FastInvokeVoidAsync(jsRuntime, identifier, CancellationToken.None, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static async ValueTask FastInvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        // Async on purpose - the CTS timer must outlive the call for the timeout to fire.
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        await FastInvokeVoidAsync(jsRuntime, identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to signal the cancellation of the operation. Specifying this parameter will override any default cancellations such as due to timeouts
    /// (<see cref="JSRuntime.DefaultAsyncTimeout"/>) from being applied.
    /// </param>
    /// <param name="args">JSON-serializable arguments.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask FastInvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            // Don't swallow JsonException - see FastInvokeAsync<TResult> for rationale.
            jsInProcessRuntime.Invoke<IJSVoidResult>(identifier, args);
            return ValueTask.CompletedTask;
        }

        return jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }


    /// <summary>
    /// Invokes the specified JavaScript function synchronously.
    /// </summary>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <exception cref="InvalidOperationException"></exception>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static void FastInvoke(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        FastInvoke<IJSVoidResult>(jsRuntime, identifier, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function synchronously.
    /// </summary>
    /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An instance of <typeparamref name="TResult"/> obtained by JSON-deserializing the return value.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static TResult FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TResult>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
            return jsInProcessRuntime.Invoke<TResult>(identifier, args);

        throw new InvalidOperationException($"This operation is not available for the current instance of the JavaScript runtime: {jsRuntime.GetType()}");
    }
}
