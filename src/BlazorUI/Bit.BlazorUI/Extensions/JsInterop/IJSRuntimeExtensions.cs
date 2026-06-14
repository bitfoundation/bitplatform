using System.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static Bit.BlazorUI.JsInteropConstants;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{


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

    /// <summary>
    /// Detects Blazor host runtimes where JavaScript interop must not be attempted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matches framework implementations by runtime <c>Type.Name</c> and probes internal state via reflection.
    /// This is unsupported by the ASP.NET Core team and may break on .NET upgrades; when a reflected member is
    /// missing we treat the runtime as <em>valid</em> so calls fail loudly instead of being silently dropped.
    /// </para>
    /// <para>Upstream types (aspnetcore <c>main</c>):</para>
    /// <list type="bullet">
    /// <item><description><see href="https://github.com/dotnet/aspnetcore/blob/main/src/Components/Endpoints/src/DependencyInjection/UnsupportedJavaScriptRuntime.cs">UnsupportedJavaScriptRuntime</see> — static/prerender host</description></item>
    /// <item><description><see href="https://github.com/dotnet/aspnetcore/blob/main/src/Components/Server/src/Circuits/RemoteJSRuntime.cs">RemoteJSRuntime</see> — reflects <c>IsInitialized</c></description></item>
    /// <item><description><see href="https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebView/WebView/src/Services/WebViewJSRuntime.cs">WebViewJSRuntime</see> — reflects <c>_ipcSender</c></description></item>
    /// </list>
    /// <para>Guarded by <c>IsRuntimeInvalidFrameworkContractTests</c>.</para>
    /// </remarks>
    [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "Reflection here only reads a well-known bool property (RemoteJSRuntime.IsInitialized) and a private field (WebViewJSRuntime._ipcSender) by name for host-runtime detection; no members are dynamically invoked or instantiated, and a missing member is handled by treating the runtime as valid, so trimming cannot break this probe.")]
    public static bool IsRuntimeInvalid(this IJSRuntime jsRuntime)
    {
        // A null runtime can't service interop, so report it as invalid. This lets the no-op paths in
        // InvokeVoid/Invoke/FastInvoke skip the call instead of dereferencing null (the async fallback
        // would otherwise throw ArgumentNullException from the framework's JSRuntimeExtensions).
        if (jsRuntime is null) return true;

        var type = jsRuntime.GetType();

        switch (type.Name)
        {
            // https://github.com/dotnet/aspnetcore/blob/main/src/Components/Endpoints/src/DependencyInjection/UnsupportedJavaScriptRuntime.cs
            case "UnsupportedJavaScriptRuntime": // Prerendering / static SSR
                return true;

            // https://github.com/dotnet/aspnetcore/blob/main/src/Components/Server/src/Circuits/RemoteJSRuntime.cs
            case "RemoteJSRuntime": // Blazor Server
                {
                    // RemoteJSRuntime.IsInitialized is an internal framework member accessed via reflection.
                    // If a future .NET release renames or removes it, fall back to treating the runtime as
                    // valid so legitimate interop still runs (and any genuine failure surfaces as the
                    // framework's own exception) instead of silently dropping every JS call.
                    var property = _remoteIsInitializedPropertyCache.GetOrAdd(type, static t => t.GetProperty("IsInitialized"));
                    return property?.GetValue(jsRuntime) is bool isInitialized && isInitialized is false;
                }

            // https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebView/WebView/src/Services/WebViewJSRuntime.cs
            case "WebViewJSRuntime": // Blazor Hybrid
                {
                    // WebViewJSRuntime._ipcSender is a private framework field accessed via reflection.
                    // See the RemoteJSRuntime note above for the rationale behind the safe fallback.
                    var field = _webViewIpcSenderFieldCache.GetOrAdd(type, static t => t.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance));
                    if (field is null) return false;
                    return field.GetValue(jsRuntime) is null;
                }

            default: // Blazor WASM and other valid runtimes
                return false;
        }
    }
}
