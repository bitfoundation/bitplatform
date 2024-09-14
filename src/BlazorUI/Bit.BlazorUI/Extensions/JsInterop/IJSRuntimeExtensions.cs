﻿using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return;

        await jsRuntime.InvokeVoidAsync(identifier, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return;

        await jsRuntime.InvokeVoidAsync(identifier, timeout, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static async ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return;

        await jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }



    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, timeout, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }



    public static bool IsRuntimeInvalid(this IJSRuntime jsRuntime)
    {
        var type = jsRuntime.GetType();

        if (type.Name is "UnsupportedJavaScriptRuntime") return true;

        if (type.Name is not "RemoteJSRuntime") return false; // Blazor WASM/Hybrid

        return (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)! is false;
    }
}
