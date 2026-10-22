using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools;

/// <summary>
/// What <c>AddBitBevToolsLoaderServices</c> decided and captured, plus Bit.BevTools once it is loaded. Singleton:
/// the assembly is loaded once per process, whichever scope asked for it first.
/// </summary>
internal sealed class BevToolsLoaderContext(BitBevToolsLoaderOptions options, IServiceCollection services)
{
    internal const string AssemblyName = "Bit.BevTools";
    private const string EntryPointTypeName = "Bit.BevTools.BevToolsLoaderEntryPoint";

    private readonly object _lock = new();
    private Task<IBevToolsEntryPoint?>? _loading;
    // 0: not checked yet, 1: no, 2: yes.
    private int _multiUserHost;

    public BitBevToolsLoaderOptions Options { get; } = options;

    /// <summary>
    /// The app's service collection. A session attached after the container was built cannot capture it any more, so
    /// the loader keeps it from startup for the Services panel; the container holds the same descriptors anyway.
    /// </summary>
    public IServiceCollection Services { get; } = services;

    public bool RegisteredAtStartup { get; private set; }

    /// <summary>Why BevTools was or was not registered at startup. The About panel of an attached session shows it.</summary>
    public string StartupDecision { get; private set; } = "";

    public IBevToolsEntryPoint? EntryPoint { get; private set; }

    /// <summary>Why the last attempt to load Bit.BevTools failed.</summary>
    public string? LoadError { get; private set; }

    /// <summary>
    /// True in a process that serves many users: an ASP.NET Core host (Blazor Server circuits, prerendering, static
    /// rendering), told by the <c>IServer</c> it registers - the signal BevTools itself goes by. Read by name from the
    /// collection the container was built from, which needs neither reflection nor a reference to ASP.NET Core.
    /// </summary>
    public bool IsMultiUserHost
    {
        get
        {
            var known = Volatile.Read(ref _multiUserHost);
            if (known == 0)
            {
                known = !OperatingSystem.IsBrowser() && Services.Any(d => d.ServiceType.FullName == BevToolsAttachPolicy.ServerTypeName) ? 2 : 1;
                Volatile.Write(ref _multiUserHost, known);
            }

            return known == 2;
        }
    }

    /// <summary>Whether BevTools may be attached in the scope of <paramref name="services"/>; <see cref="BevToolsAttachPolicy"/> says why.</summary>
    internal (bool Allowed, string Reason) ResolveAttach(IServiceProvider services) =>
        BevToolsAttachPolicy.Resolve(services, IsMultiUserHost, Options.AllowInMultiUserHost, "Set BitBevToolsLoaderOptions.AllowInMultiUserHost to allow it.");

    internal void Registered(IBevToolsEntryPoint entryPoint, string reason)
    {
        EntryPoint = entryPoint;
        RegisteredAtStartup = true;
        StartupDecision = reason;
    }

    internal void Deferred(IBevToolsEntryPoint? entryPoint, string reason)
    {
        EntryPoint = entryPoint;
        StartupDecision = reason;
    }

    /// <summary>Loads Bit.BevTools if it is among the assemblies the app started with; never downloads anything.</summary>
    internal IBevToolsEntryPoint? TryLoadFromApp(out string? error)
    {
        error = null;
        if (EntryPoint is { } loaded)
        {
            return loaded;
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.Load(new AssemblyName(AssemblyName));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            error = AssemblyName + " is not among the assemblies this app started with (" + ex.GetType().Name + ").";
            return null;
        }

        return EntryPoint = Create(assembly, out error);
    }

    /// <summary>
    /// Registers BevTools at startup through the loaded assembly. A Bit.BevTools of another version than this loader
    /// fails here rather than when it is loaded, so that is reported instead of failing the app's startup.
    /// </summary>
    internal bool TryAddServices(IBevToolsEntryPoint entryPoint, IServiceCollection services, out string reason)
    {
        try
        {
            return entryPoint.TryAddServices(services, this, out reason);
        }
        catch (Exception ex) when (IsVersionMismatch(ex))
        {
            reason = VersionMismatch(entryPoint, ex);
            return false;
        }
    }

    /// <summary>
    /// Loads Bit.BevTools once per process: from the app's own assemblies when it is one of them, otherwise in Blazor
    /// WebAssembly through <see cref="LazyAssemblyLoader"/>, which downloads it when it is marked BlazorWebAssemblyLazyLoad.
    /// A failed load is retried on the next call, so a dropped connection does not disable BevTools until a reload.
    /// </summary>
    public Task<IBevToolsEntryPoint?> LoadAsync(IJSRuntime js)
    {
        if (EntryPoint is { } loaded)
        {
            return Task.FromResult<IBevToolsEntryPoint?>(loaded);
        }

        lock (_lock)
        {
            if (_loading is null || _loading is { IsCompleted: true, Result: null })
            {
                _loading = LoadCoreAsync(js);
            }

            return _loading;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "The one assembly loaded here is Bit.BevTools, which the DynamicDependency on Create keeps, and whose embedded ILLink.Descriptors.xml then keeps whole, with everything it depends on.")]
    private async Task<IBevToolsEntryPoint?> LoadCoreAsync(IJSRuntime js)
    {
        try
        {
            var entryPoint = TryLoadFromApp(out var error);
            if (entryPoint is not null || !OperatingSystem.IsBrowser())
            {
                LoadError = error;
                return entryPoint;
            }

            var loaded = await new LazyAssemblyLoader(js).LoadAssembliesAsync([AssemblyName + ".wasm"]);
            var assembly = loaded.FirstOrDefault(IsBevTools) ?? AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(IsBevTools);
            if (assembly is null)
            {
                LoadError = "LazyAssemblyLoader did not load " + AssemblyName + ".";
                return null;
            }

            EntryPoint = Create(assembly, out error);
            LoadError = error;
            return EntryPoint;
        }
        catch (Exception ex)
        {
            // LazyAssemblyLoader throws when the assembly is not marked BlazorWebAssemblyLazyLoad (and the app started
            // without it, or this would not have been reached), or when the download fails.
            LoadError = "Loading " + AssemblyName + " failed: " + ex.Message;
            return null;
        }

        static bool IsBevTools(Assembly assembly) => string.Equals(assembly.GetName().Name, AssemblyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// The app reaches Bit.BevTools only by name, so the trimmer is told here what it would not see otherwise. It keeps
    /// the entry point only where the loader itself is kept, and Bit.BevTools' embedded descriptor then keeps the whole
    /// assembly; an app that never uses BevTools ships none of it. A build that leaves Bit.BevTools out has nothing to
    /// keep, which is what IL2035 would warn about.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, EntryPointTypeName, AssemblyName)]
    [UnconditionalSuppressMessage("Trimming", "IL2035:Unresolved assembly in DynamicDependencyAttribute",
        Justification = "A build may leave Bit.BevTools out, in which case the loader passes everything through.")]
    private static IBevToolsEntryPoint? Create(Assembly assembly, out string? error)
    {
        try
        {
            if (assembly.GetCustomAttribute<BevToolsEntryPointAttribute>() is { } attribute
                && Activator.CreateInstance(attribute.EntryPointType) is IBevToolsEntryPoint entryPoint)
            {
                error = null;
                return entryPoint;
            }
        }
        catch (Exception ex) when (IsVersionMismatch(ex))
        {
        }

        error = VersionMismatch(assembly.GetName());
        return null;
    }

    /// <summary>
    /// What the runtime throws when Bit.BevTools was built against another version of this loader: it meets the
    /// internal contract that differs when it loads the entry point, or when it first runs a method that uses it.
    /// </summary>
    internal static bool IsVersionMismatch(Exception exception) => exception is TypeLoadException or MissingMemberException or InvalidCastException;

    internal static string VersionMismatch(IBevToolsEntryPoint entryPoint, Exception exception) =>
        VersionMismatch(entryPoint.GetType().Assembly.GetName()) + " (" + exception.GetType().Name + ": " + exception.Message + ")";

    private static string VersionMismatch(AssemblyName assembly) =>
        $"{assembly.Name} {assembly.Version} does not work with this Bit.BevTools.Loader {typeof(BevToolsLoaderContext).Assembly.GetName().Version}: reference the same version of both packages.";
}
