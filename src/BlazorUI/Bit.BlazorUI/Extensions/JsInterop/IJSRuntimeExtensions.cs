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
    /// <remarks>
    /// With the lazy scripts on (<see cref="BitBlazorUIScripts"/>), the module of the packages the identifier calls into is
    /// imported first, the first time it is called.
    /// </remarks>
    public static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        var loading = BitScriptLoader.EnsureLoaded(jsRuntime, identifier, CancellationToken.None);
        if (loading.IsCompletedSuccessfully) return loading.Result ? jsRuntime.InvokeVoidAsync(identifier, args) : ValueTask.CompletedTask;

        return InvokeVoidAfter(loading, jsRuntime, identifier, args);

        static async ValueTask InvokeVoidAfter(ValueTask<bool> loading, IJSRuntime jsRuntime, string identifier, object?[]? args)
        {
            if (await loading) await jsRuntime.InvokeVoidAsync(identifier, args);
        }
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    /// <remarks>
    /// With the lazy scripts on (<see cref="BitBlazorUIScripts"/>), the module of the packages the identifier calls into is
    /// imported first, the first time it is called.
    /// </remarks>
    public static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        var loading = BitScriptLoader.EnsureLoaded(jsRuntime, identifier, CancellationToken.None);
        if (loading.IsCompletedSuccessfully) return loading.Result ? jsRuntime.InvokeVoidAsync(identifier, timeout, args) : ValueTask.CompletedTask;

        return InvokeVoidAfter(loading, jsRuntime, identifier, timeout, args);

        static async ValueTask InvokeVoidAfter(ValueTask<bool> loading, IJSRuntime jsRuntime, string identifier, TimeSpan timeout, object?[]? args)
        {
            if (await loading) await jsRuntime.InvokeVoidAsync(identifier, timeout, args);
        }
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    /// <remarks>
    /// With the lazy scripts on (<see cref="BitBlazorUIScripts"/>), the module of the packages the identifier calls into is
    /// imported first, the first time it is called.
    /// </remarks>
    public static ValueTask InvokeVoid(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        var loading = BitScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken);
        if (loading.IsCompletedSuccessfully) return loading.Result ? jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args) : ValueTask.CompletedTask;

        return InvokeVoidAfter(loading, jsRuntime, identifier, cancellationToken, args);

        static async ValueTask InvokeVoidAfter(ValueTask<bool> loading, IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (await loading) await jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
        }
    }



    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    /// <remarks>
    /// With the lazy scripts on (<see cref="BitBlazorUIScripts"/>), the module of the packages the identifier calls into is
    /// imported first, the first time it is called.
    /// </remarks>
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        var loading = BitScriptLoader.EnsureLoaded(jsRuntime, identifier, CancellationToken.None);
        if (loading.IsCompletedSuccessfully) return loading.Result ? jsRuntime.InvokeAsync<TValue>(identifier, args) : default;

        return InvokeAfter(loading, jsRuntime, identifier, args);

        static async ValueTask<TValue> InvokeAfter(ValueTask<bool> loading, IJSRuntime jsRuntime, string identifier, object?[]? args)
            => await loading ? await jsRuntime.InvokeAsync<TValue>(identifier, args) : default!;
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    /// <remarks>
    /// With the lazy scripts on (<see cref="BitBlazorUIScripts"/>), the module of the packages the identifier calls into is
    /// imported first, the first time it is called.
    /// </remarks>
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, TimeSpan timeout, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        var loading = BitScriptLoader.EnsureLoaded(jsRuntime, identifier, CancellationToken.None);
        if (loading.IsCompletedSuccessfully) return loading.Result ? jsRuntime.InvokeAsync<TValue>(identifier, timeout, args) : default;

        return InvokeAfter(loading, jsRuntime, identifier, timeout, args);

        static async ValueTask<TValue> InvokeAfter(ValueTask<bool> loading, IJSRuntime jsRuntime, string identifier, TimeSpan timeout, object?[]? args)
            => await loading ? await jsRuntime.InvokeAsync<TValue>(identifier, timeout, args) : default!;
    }

    /// <summary>
    /// Only tries to Invoke the js call when the runtime is valid.
    /// </summary>
    /// <remarks>
    /// With the lazy scripts on (<see cref="BitBlazorUIScripts"/>), the module of the packages the identifier calls into is
    /// imported first, the first time it is called.
    /// </remarks>
    public static ValueTask<TValue> Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (jsRuntime.IsRuntimeInvalid()) return default;

        var loading = BitScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken);
        if (loading.IsCompletedSuccessfully) return loading.Result ? jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args) : default;

        return InvokeAfter(loading, jsRuntime, identifier, cancellationToken, args);

        static async ValueTask<TValue> InvokeAfter(ValueTask<bool> loading, IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, object?[]? args)
            => await loading ? await jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args) : default!;
    }



    /// <summary>
    /// Invokes a function of the packages' scripts the way <see cref="JSRuntimeExtensions.InvokeVoidAsync(IJSRuntime, string, object?[])"/>
    /// does - throwing where the runtime cannot take the call - after importing its module where the lazy scripts are on.
    /// The form every call of the packages' code into their scripts takes that does not go through
    /// <see cref="InvokeVoid(IJSRuntime, string, object?[])"/>.
    /// </summary>
    internal static async ValueTask InvokeScriptVoidAsync(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        if (await BitScriptLoader.EnsureLoaded(jsRuntime, identifier, CancellationToken.None)) await jsRuntime.InvokeVoidAsync(identifier, args);
    }

    /// <summary>The same as <see cref="InvokeScriptVoidAsync(IJSRuntime, string, object?[])"/>, under a cancellation token.</summary>
    internal static async ValueTask InvokeScriptVoidAsync(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        if (await BitScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken)) await jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
    }

    /// <summary>
    /// Invokes a function of the packages' scripts the way <see cref="JSRuntimeExtensions.InvokeAsync{TValue}(IJSRuntime, string, object?[])"/>
    /// does - throwing where the runtime cannot take the call - after importing its module where the lazy scripts are on.
    /// </summary>
    internal static async ValueTask<TValue> InvokeScriptAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        return await BitScriptLoader.EnsureLoaded(jsRuntime, identifier, CancellationToken.None) ? await jsRuntime.InvokeAsync<TValue>(identifier, args) : default!;
    }

    /// <summary>The same as <see cref="InvokeScriptAsync{TValue}(IJSRuntime, string, object?[])"/>, under a cancellation token.</summary>
    internal static async ValueTask<TValue> InvokeScriptAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(this IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        return await BitScriptLoader.EnsureLoaded(jsRuntime, identifier, cancellationToken) ? await jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args) : default!;
    }

    /// <summary>
    /// Loads a script of the packages that acts on a component's markup without being called - kept for the component by a
    /// <c>bit-trim: owners</c> comment, and adding its listeners as it loads - where the lazy scripts are on, which nothing
    /// calling into it would ever load. A component owning one calls this as it first renders.
    /// </summary>
    internal static ValueTask EnsureScriptAsync(this IJSRuntime jsRuntime, string package, string script)
    {
        if (jsRuntime.IsRuntimeInvalid()) return ValueTask.CompletedTask;

        return BitScriptLoader.EnsureScript(jsRuntime, package, script);
    }



    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Framework-internal members are probed reflectively; a member the trimmer removed makes the probe answer 'valid', and the interop call then surfaces the real error.")]
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

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Framework-internal members are probed reflectively; a member the trimmer removed makes the probe answer 'valid', and the interop call then surfaces the real error.")]
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
