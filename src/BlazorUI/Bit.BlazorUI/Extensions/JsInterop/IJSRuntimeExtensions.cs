using System.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
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

    // Cache the per-runtime-type "is this runtime invalid?" probe. The probe is resolved once per
    // concrete IJSRuntime type via reflection (the framework exposes no public API for this) and
    // reused thereafter, so the reflection cost is paid once rather than on every interop call.
    private static readonly ConcurrentDictionary<Type, Func<IJSRuntime, bool>> RuntimeInvalidProbes = new();



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

        // Resolve (and cache) a probe for this concrete runtime type. The probe is defensive: it
        // relies on framework-internal type names / members that can shift between .NET releases,
        // so any missing member or reflection failure is treated as "runtime is valid" (return
        // false) rather than throwing - a wrong-but-safe answer that lets the interop call proceed
        // and surface a real error, instead of crashing here.
        var probe = RuntimeInvalidProbes.GetOrAdd(jsRuntime.GetType(), BuildRuntimeInvalidProbe);
        return probe(jsRuntime);
    }

    [SuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.", Justification = "Framework-internal members probed reflectively; failures fall back to 'valid'.")]
    private static Func<IJSRuntime, bool> BuildRuntimeInvalidProbe(Type type)
    {
        switch (type.Name)
        {
            // https://github.com/dotnet/aspnetcore/blob/main/src/Components/Endpoints/src/DependencyInjection/UnsupportedJavaScriptRuntime.cs
            case "UnsupportedJavaScriptRuntime": // Prerendering / static SSR
                return static _ => true;

            // https://github.com/dotnet/aspnetcore/blob/main/src/Components/Server/src/Circuits/RemoteJSRuntime.cs
            case "RemoteJSRuntime": // Blazor Server
            {
                var isInitialized = type.GetProperty("IsInitialized", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (isInitialized is null) return static _ => false;
                return rt =>
                {
                    try { return isInitialized.GetValue(rt) is false; }
                    catch { return false; }
                };
            }

            // https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebView/WebView/src/Services/WebViewJSRuntime.cs
            case "WebViewJSRuntime": // Blazor Hybrid
            {
                var ipcSender = type.GetField("_ipcSender", BindingFlags.NonPublic | BindingFlags.Instance);
                if (ipcSender is null) return static _ => false;
                return rt =>
                {
                    try { return ipcSender.GetValue(rt) is null; }
                    catch { return false; }
                };
            }

            default: // Blazor WASM and anything else: assume valid.
                return static _ => false;
        }
    }
}
