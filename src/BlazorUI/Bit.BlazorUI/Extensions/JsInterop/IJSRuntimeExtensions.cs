using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;



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
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, timeout, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        return jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }



    [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
    public static bool IsRuntimeInvalid(this IJSRuntime jsRuntime)
    {
        if (jsRuntime is null) return false;

        var type = jsRuntime.GetType();

        return type.Name switch
        {
            "UnsupportedJavaScriptRuntime" => true, // Prerendering
            "RemoteJSRuntime" => (bool)type.GetProperty("IsInitialized")!.GetValue(jsRuntime)! is false, // Blazor server
            "WebViewJSRuntime" => type.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(jsRuntime) is null, // Blazor Hybrid
            _ => false // Blazor WASM
        };
    }
}
