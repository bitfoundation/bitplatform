using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop.Infrastructure;

namespace Bit.BlazorUI;

public static class IJSRuntimeFastExtensions
{
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;



    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask FastInvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return FastInvokeVoid(jsRuntime, identifier, CancellationToken.None, args);
    }

    /// <summary>
    /// Invokes the specified JavaScript function with the fastest speed possible.
    /// </summary>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="timeout">The duration after which to cancel the async operation. Overrides default timeouts (<see cref="JSRuntime.DefaultAsyncTimeout"/>).</param>
    /// <param name="args">JSON-serializable arguments.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask FastInvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return FastInvokeVoid(jsRuntime, identifier, cancellationToken, args);
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
    public static ValueTask FastInvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
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
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
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
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        using var cancellationTokenSource = timeout == Timeout.InfiniteTimeSpan ? null : new CancellationTokenSource(timeout);
        var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

        return FastInvoke<TValue>(jsRuntime, identifier, cancellationToken, args);
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
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TValue> FastInvoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
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
            return jsRuntime.Invoke<TValue>(identifier, cancellationToken, args);
        }
    }
}
