using System.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;



    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        return jsRuntime.InvokeVoidAsync(identifier, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        return jsRuntime.InvokeVoidAsync(identifier, timeout, args);
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    public static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        return jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
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



    // Cache the reflected framework members per runtime type. The runtime type is stable for the
    // lifetime of the process, so this avoids repeating the reflection lookup on every interop call.
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _remoteIsInitializedPropertyCache = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo?> _webViewIpcSenderFieldCache = new();

    [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
    public static bool IsRuntimeInvalid(this IJSRuntime jsRuntime)
    {
        if (jsRuntime is null) return false;

        var type = jsRuntime.GetType();

        switch (type.Name)
        {
            case "UnsupportedJavaScriptRuntime": // Prerendering
                return true;

            case "RemoteJSRuntime": // Blazor Server
                {
                    // RemoteJSRuntime.IsInitialized is an internal framework member accessed via reflection.
                    // If a future .NET release renames or removes it, fall back to treating the runtime as
                    // valid so legitimate interop still runs (and any genuine failure surfaces as the
                    // framework's own exception) instead of silently dropping every JS call.
                    var property = _remoteIsInitializedPropertyCache.GetOrAdd(type, static t => t.GetProperty("IsInitialized"));
                    return property?.GetValue(jsRuntime) is bool isInitialized && isInitialized is false;
                }

            case "WebViewJSRuntime": // Blazor Hybrid
                {
                    // WebViewJSRuntime._ipcSender is a private framework field accessed via reflection.
                    // See the RemoteJSRuntime note above for the rationale behind the safe fallback.
                    var field = _webViewIpcSenderFieldCache.GetOrAdd(type, static t => t.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance));
                    if (field is null) return false;
                    return field.GetValue(jsRuntime) is null;
                }

            default: // Blazor WASM
                return false;
        }
    }
}
