using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Butil;

public static class BitButil
{
    /// <summary>
    /// Registers every Butil service - each class marked with <see cref="ButilServiceAttribute"/> - as scoped.
    /// </summary>
    /// <remarks>
    /// Scoped matches Blazor's "one circuit / one WASM app instance per user" model. Transient would create
    /// a fresh wrapper on every <c>@inject</c>, fragmenting per-instance listener bookkeeping and keeping
    /// captured component delegates alive longer than the component itself.
    /// <br/>
    /// The services are discovered by reflection rather than by a list of <c>AddScoped&lt;T&gt;()</c> calls,
    /// and that is a trimming decision, not a style one. <c>AddScoped&lt;T&gt;()</c> annotates <c>T</c> with
    /// <see cref="DynamicallyAccessedMemberTypes.PublicConstructors"/>, so naming all the Butil classes in
    /// one method roots all of them: a consumer that injects only <see cref="LocalStorage"/> still carried
    /// every other Butil class in their trimmed output. Reflecting over the assembly gives the trimmer
    /// nothing to follow, so a class nobody injects is removed from the consumer's app and is simply not
    /// there to be discovered here - <see cref="System.Reflection.Assembly.GetTypes"/> returns only the
    /// types that survived. Constructors of the classes that DO survive are preserved by the annotated
    /// type argument of <see cref="ButilServiceAttribute"/>; see that type for why it is shaped that way.
    /// <br/>
    /// Consequence worth knowing: in a trimmed app this registers a subset, so injecting a Butil class from
    /// code the trimmer removed (or purely through reflection) fails at runtime rather than at build time.
    /// Untrimmed apps - Blazor Server, and the prerendering host of a WebAssembly app - register everything.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Enumerating this assembly's types is the point: types the trimmer removed are absent from the consumer's app, so skipping them is correct rather than a defect. Constructors of the surviving types are preserved by ButilServiceAttribute's annotated type argument.")]
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        foreach (var type in typeof(BitButil).Assembly.GetTypes())
        {
            if (type.GetCustomAttribute<ButilServiceAttribute>(inherit: false) is not { } butilService) continue;

            // Registering ServiceType rather than the scanned type is what keeps this call trim-clean:
            // the property carries the PublicConstructors annotation, so no suppression is needed here.
            // TryAdd so that a consumer registration made before this call wins, and so that calling this
            // method twice on the same collection cannot produce duplicate descriptors.
            services.TryAddScoped(butilService.ServiceType);
        }

        return services;
    }

    /// <summary>
    /// Registers every Butil service (see <see cref="AddBitButilServices(IServiceCollection)"/>) and applies the
    /// library's runtime options in the same call - lazy scripts, the modules path, fast invoke - so an app can
    /// configure everything in <c>Program.cs</c> without an MSBuild property.
    /// </summary>
    /// <remarks>
    /// The options set the same process-wide toggles as <see cref="UseLazyScripts"/> / <see cref="UseFastInvoke"/>
    /// and friends; see <see cref="BitButilOptions"/> for what that does and does not cover compared with the
    /// <c>BitButilLazyScripts</c> and <c>BitButilTrimScripts</c> MSBuild properties.
    /// </remarks>
    public static IServiceCollection AddBitButilServices(this IServiceCollection services, Action<BitButilOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BitButilOptions();
        configure(options);

        switch (options.LazyScripts)
        {
            case true: UseLazyScripts(options.ScriptModulesPath); break;
            case false: UseBundledScripts(); break;
            default:
                // Mode left alone; a modules path on its own still applies, for an app that turned lazy
                // scripts on through the MSBuild property but serves the package's assets from elsewhere.
                if (string.IsNullOrWhiteSpace(options.ScriptModulesPath) is false) SetScriptModulesPath(options.ScriptModulesPath);
                break;
        }

        switch (options.FastInvoke)
        {
            case true: UseFastInvoke(); break;
            case false: UseNormalInvoke(); break;
        }

        return AddBitButilServices(services);
    }

    private static volatile bool _fastInvokeEnabled;

    internal static bool FastInvokeEnabled => _fastInvokeEnabled;

    /// <summary>
    /// Enables the synchronous in-process ("fast") invoke path for the APIs that opt into it.
    /// <br/>
    /// Only APIs backed by synchronous JavaScript functions (for example <see cref="LocalStorage"/>,
    /// <see cref="SessionStorage"/>, <see cref="Cookie"/>, <see cref="Console"/> and <see cref="Location"/>)
    /// use this path; everything that wraps an asynchronous (Promise-returning) browser API always runs
    /// asynchronously regardless of this setting, so enabling it can't break those calls.
    /// Only effective on Blazor WebAssembly (where an <see cref="Microsoft.JSInterop.IJSInProcessRuntime"/> is available).
    /// <br/>
    /// NOTE: this is a process-wide static toggle, not per-app/per-circuit. It is intended to be set
    /// once at startup. On Blazor Server it is effectively a no-op (the fast path always falls back to
    /// the async path because there is no in-process runtime), so sharing it across circuits is benign.
    /// </summary>
    public static void UseFastInvoke()
    {
        _fastInvokeEnabled = true;
    }

    /// <summary>
    /// Disables the synchronous in-process ("fast") invoke path; all calls run asynchronously.
    /// Process-wide static toggle - see <see cref="UseFastInvoke"/>.
    /// </summary>
    public static void UseNormalInvoke()
    {
        _fastInvokeEnabled = false;
    }


    /// <summary>The AppContext switch the <c>BitButilLazyScripts</c> MSBuild property sets.</summary>
    internal const string LazyScriptsSwitchName = "Bit.Butil.LazyScripts";

    /// <summary>The default location of the per-module scripts, relative to the app's base href.</summary>
    internal const string DefaultScriptModulesPath = "./_content/Bit.Butil/modules/";

    // null = not overridden at runtime, so the build-time switch decides.
    private static volatile object? _lazyScriptsOverride;
    private static volatile string _scriptModulesPath = DefaultScriptModulesPath;

    /// <summary>
    /// The build-time half of the lazy-scripts switch: <c>&lt;BitButilLazyScripts&gt;true&lt;/BitButilLazyScripts&gt;</c>
    /// in the consumer's project ends up here as an <see cref="AppContext"/> switch. Marked as a feature
    /// switch so the trimmer can treat it as a constant in a trimmed publish (the runtime override below is
    /// what keeps <see cref="UseLazyScripts"/> working there regardless).
    /// </summary>
#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition(LazyScriptsSwitchName)]
#endif
    internal static bool LazyScriptsFeature => AppContext.TryGetSwitch(LazyScriptsSwitchName, out var enabled) && enabled;

    /// <summary>
    /// True when Bit.Butil loads its JavaScript per module, on first use, instead of expecting the whole
    /// <c>bit-butil.js</c> bundle to have been loaded by a <c>&lt;script&gt;</c> tag. Decided by the build-time
    /// switch unless <see cref="UseLazyScripts"/> / <see cref="UseBundledScripts"/> overrode it at runtime.
    /// </summary>
    internal static bool LazyScriptsEnabled => _lazyScriptsOverride is bool overridden ? overridden : LazyScriptsFeature;

    /// <summary>Where the per-module scripts are served from; see <see cref="UseLazyScripts"/>.</summary>
    internal static string ScriptModulesPath => _scriptModulesPath;

    /// <summary>
    /// Loads Bit.Butil's JavaScript per module, on first use, instead of from the single <c>bit-butil.js</c>
    /// bundle: the first call into a Butil API <c>import()</c>s just that API's module
    /// (<c>_content/Bit.Butil/modules/clipboard.js</c> for <see cref="Clipboard"/>, and so on), so an app
    /// downloads only the JavaScript for the APIs it actually calls and needs no <c>&lt;script&gt;</c> tag at all.
    /// <br/>
    /// The preferred way to turn this on is the <c>&lt;BitButilLazyScripts&gt;true&lt;/BitButilLazyScripts&gt;</c>
    /// MSBuild property in the app's project, which also keeps the bundle out of the published output; this
    /// method exists for hosts where the build-time switch cannot be applied. Process-wide static toggle
    /// intended to be set once at startup, before any Butil call - see <see cref="UseFastInvoke"/> for the
    /// reasoning.
    /// </summary>
    /// <param name="modulesPath">
    /// Where the module files are served from, relative to the app's base href, when it is not the default
    /// <c>./_content/Bit.Butil/modules/</c>.
    /// </param>
    public static void UseLazyScripts(string? modulesPath = null)
    {
        _lazyScriptsOverride = true;

        if (string.IsNullOrWhiteSpace(modulesPath) is false) SetScriptModulesPath(modulesPath);
    }

    private static void SetScriptModulesPath(string modulesPath)
    {
        _scriptModulesPath = modulesPath.EndsWith('/') ? modulesPath : modulesPath + "/";
    }

    /// <summary>
    /// Expects the whole <c>bit-butil.js</c> bundle to be loaded by the app (a <c>&lt;script&gt;</c> tag) and
    /// never loads modules on demand - the default. Overrides the build-time switch; process-wide static toggle,
    /// see <see cref="UseLazyScripts"/>.
    /// </summary>
    public static void UseBundledScripts()
    {
        _lazyScriptsOverride = false;
    }
}
