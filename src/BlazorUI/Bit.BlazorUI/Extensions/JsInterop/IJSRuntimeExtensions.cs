using System.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public static class IJSRuntimeExtensions
{
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

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



    [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
    public static bool IsRuntimeInvalid(this IJSRuntime jsRuntime)
    {
        if (jsRuntime is null) return false;

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
            case "UnsupportedJavaScriptRuntime": // Prerendering
                return static _ => true;

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
