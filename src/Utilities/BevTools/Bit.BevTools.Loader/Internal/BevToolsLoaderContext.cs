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

    private readonly object _lock = new();
    private Task<IBevToolsEntryPoint?>? _loading;

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
        try
        {
            return EntryPoint ??= Create(Assembly.Load(new AssemblyName(AssemblyName)), out error);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            error = AssemblyName + " is not among the assemblies this app started with (" + ex.GetType().Name + ").";
            return null;
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
        Justification = "The one assembly loaded here is Bit.BevTools, whose package roots it whole (TrimmerRootAssembly), so the trimmer kept everything it depends on.")]
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

    private static IBevToolsEntryPoint? Create(Assembly assembly, out string? error)
    {
        if (assembly.GetCustomAttribute<BevToolsEntryPointAttribute>() is not { } attribute
            || Activator.CreateInstance(attribute.EntryPointType) is not IBevToolsEntryPoint entryPoint)
        {
            error = $"{assembly.GetName().Name} {assembly.GetName().Version} does not work with this Bit.BevTools.Loader: reference the same version of both packages.";
            return null;
        }

        error = null;
        return entryPoint;
    }
}
