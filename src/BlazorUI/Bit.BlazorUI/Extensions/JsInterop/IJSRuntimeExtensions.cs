using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop.Infrastructure;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return;

        await jsRuntime.InvokeVoidAsync(identifier, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return;

        await jsRuntime.InvokeVoidAsync(identifier, timeout, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return;

        await jsRuntime.FastInvokeVoid(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask FastInvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

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
            return jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
        }
    }



    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        this IJSRuntime jsRuntime,
        string identifier,
        params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        this IJSRuntime jsRuntime,
        string identifier,
        TimeSpan timeout,
        params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, timeout, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        this IJSRuntime jsRuntime,
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.FastInvoke<TValue>(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static ValueTask<TResult> FastInvoke<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TResult>(
        this IJSRuntime jsRuntime,
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
        {
            try
            {
                return ValueTask.FromResult(jsInProcessRuntime.Invoke<TResult>(identifier, args));
            }
            catch (JsonException ex)
            {
                System.Console.Error.WriteLine($"Error invoking '{identifier}' using {nameof(IJSInProcessRuntime)}. A JSON-related issue occurred: {ex.Message}.");
                return ValueTask.FromResult(default(TResult)!);
            }
        }
        else
        {
            return jsRuntime.InvokeAsync<TResult>(identifier, cancellationToken, args);
        }
    }



    [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
    internal static bool IsRuntimeInvalid(this IJSRuntime jsRuntime)
    {
        var type = jsRuntime.GetType();

        return type.Name switch
        {
            "UnsupportedJavaScriptRuntime" => true, // Prerendering
            "RemoteJSRuntime" => (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)! is false, // Blazor server
            _ => false // Blazor WASM/Hybrid
        };
    }
}
