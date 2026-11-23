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
internal sealed class BevToolsLoaderContext(BevToolsLoaderOptions options, IServiceCollection services)
{
    internal const string AssemblyName = "Bit.BevTools";
    private const string EntryPointTypeName = "Bit.BevTools.BevToolsLoaderEntryPoint";

    private readonly object _lock = new();
    private Task<IBevToolsEntryPoint?>? _loading;
    // 0: not checked yet, 1: no, 2: yes.
    private int _multiUserHost;

    public BevToolsLoaderOptions Options { get; } = options;

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
    /// Whether the last attempt to load Bit.BevTools failed because an assembly could not be downloaded (offline, or an
    /// HTTP error), rather than because of how the app is set up.
    /// </summary>
    public bool LoadFailedToDownload { get; private set; }

    /// <summary>
    /// How many of <see cref="BevToolsLoaderOptions.BevToolsConfigurations"/> the BevTools registered at startup has had
    /// applied: a later <c>AddBitBevToolsLoaderServices</c> call can add more, and only those are applied then.
    /// </summary>
    internal int StartupConfigurationsApplied { get; set; }

    /// <summary>
    /// What <see cref="BevToolsAutoStart.Logs"/> captured until a session takes it over; null when that part was not
    /// started (not asked for, or BevTools was registered at startup).
    /// </summary>
    public BevToolsAutoStartLogs? AutoStartLogs { get; private set; }

    internal BevToolsAutoStartLogs StartAutoStartLogs() => AutoStartLogs ??= new BevToolsAutoStartLogs(this);

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
        BevToolsAttachPolicy.Resolve(services, IsMultiUserHost, Options.AllowInMultiUserHost, Options.Authorize is not null,
            "Set BevToolsLoaderOptions.AllowInMultiUserHost, with BevToolsLoaderOptions.Authorize, to allow it.");

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

    /// <summary>
    /// Loads Bit.BevTools if it is among the assemblies the app started with, together with every one of
    /// <see cref="BevToolsLoaderOptions.AdditionalAssemblies"/>; never downloads anything. A BevTools whose additional
    /// assemblies are missing is not loaded: the app's <see cref="BevToolsLoaderOptions.ConfigureBevTools"/> callbacks
    /// may name their types, and would fail on the first one.
    /// </summary>
    internal IBevToolsEntryPoint? TryLoadFromApp(out string? error)
    {
        if (!TryLoadAssembliesFromApp(out var bevTools, out error))
        {
            return null;
        }

        return EntryPoint ??= Create(bevTools!, out error);
    }

    /// <summary>
    /// Loads Bit.BevTools and every one of <see cref="BevToolsLoaderOptions.AdditionalAssemblies"/> - those a later
    /// <c>AddBitBevToolsLoaderServices</c> call added included - from the assemblies the app started with; never
    /// downloads anything. False, with the missing ones in <paramref name="error"/>, when any of them is not there.
    /// </summary>
    internal bool TryLoadAssembliesFromApp(out Assembly? bevTools, out string? error)
    {
        error = null;
        bevTools = null;
        List<string>? missing = null;
        foreach (var name in AssemblyNames())
        {
            if (TryLoad(name, out var exception) is { } assembly)
            {
                if (name == AssemblyName)
                {
                    bevTools = assembly;
                }
            }
            else
            {
                (missing ??= []).Add(name + " (" + exception!.GetType().Name + ")");
            }
        }

        if (missing is not null)
        {
            error = string.Join(", ", missing) + (missing.Count == 1 ? " is" : " are") + " not among the assemblies this app started with.";
            return false;
        }

        return true;
    }

    /// <summary>Bit.BevTools first, then <see cref="BevToolsLoaderOptions.AdditionalAssemblies"/>, each once.</summary>
    private IEnumerable<string> AssemblyNames() =>
        new[] { AssemblyName }.Concat(Options.AdditionalAssemblies.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim())).Distinct(StringComparer.Ordinal);

    /// <summary>
    /// The assembly when it is loaded or among the app's own: one the runtime holds already is taken from it, since a
    /// lazily loaded one is known to the runtime but may not be found by name again.
    /// </summary>
    private static Assembly? TryLoad(string name, out Exception? exception)
    {
        exception = null;
        if (FindLoaded(name) is { } loaded)
        {
            return loaded;
        }

        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            exception = ex;
            return null;
        }
    }

    private static Assembly? FindLoaded(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.Ordinal));

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
    /// Loads Bit.BevTools and <see cref="BevToolsLoaderOptions.AdditionalAssemblies"/> once per process: from the app's
    /// own assemblies when they are among them, otherwise in Blazor WebAssembly through <see cref="LazyAssemblyLoader"/>,
    /// which downloads the ones marked BlazorWebAssemblyLazyLoad. A failed load is retried on the next call, so a dropped
    /// connection does not disable BevTools until a reload. An entry point loaded already is not the end of it: a later
    /// <c>AddBitBevToolsLoaderServices</c> call may have added assemblies after it was, and the app's callbacks need those
    /// too.
    /// </summary>
    public Task<IBevToolsEntryPoint?> LoadAsync(IJSRuntime js)
    {
        if (EntryPoint is { } loaded && AssemblyNames().All(name => FindLoaded(name) is not null))
        {
            return Task.FromResult<IBevToolsEntryPoint?>(loaded);
        }

        lock (_lock)
        {
            // A finished load that gave the entry point is done again too: reached here, an assembly it did not load is
            // still missing.
            if (_loading is null || _loading.IsCompleted)
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
        LoadFailedToDownload = false;
        var missing = new List<string>();
        try
        {
            var entryPoint = TryLoadFromApp(out var error);
            if (entryPoint is not null || !OperatingSystem.IsBrowser())
            {
                LoadError = error;
                return entryPoint;
            }

            missing.AddRange(AssemblyNames().Where(name => FindLoaded(name) is null));
            if (missing.Count == 0)
            {
                // Everything is loaded, and Bit.BevTools still did not give its entry point: another version of it.
                LoadError = error;
                return null;
            }

            // The runtime keeps a download that failed for the rest of the page's life, and answers every later request
            // for that assembly with the same failure: a browser that knows it is offline is not asked, so the next
            // attempt, once it is back online, can still succeed.
            if (await IsOfflineAsync(js))
            {
                LoadFailedToDownload = true;
                LoadError = string.Join(", ", missing) + " could not be downloaded: the browser is offline.";
                return null;
            }

            // One call, so the assemblies download side by side; each of them is marked BlazorWebAssemblyLazyLoad, or
            // the runtime says which one is not.
            await new LazyAssemblyLoader(js).LoadAssembliesAsync(missing.Select(name => name + ".wasm"));
            if (FindLoaded(AssemblyName) is not { } assembly)
            {
                LoadError = "LazyAssemblyLoader did not load " + AssemblyName + ".";
                return null;
            }

            if (EntryPoint is null)
            {
                EntryPoint = Create(assembly, out error);
            }
            else
            {
                // Bit.BevTools was loaded before; this load was for assemblies added after it.
                error = null;
            }

            LoadError = error;
            return EntryPoint;
        }
        catch (Exception ex)
        {
            // LazyAssemblyLoader throws when an assembly is not marked BlazorWebAssemblyLazyLoad (and the app started
            // without it, or this would not have been reached), or when the download fails.
            LoadError = await DescribeLoadFailureAsync(js, missing, ex);
            return null;
        }
    }

    /// <summary>
    /// What a failed lazy load says, in words the app can show whoever pressed its button. A download failure is told
    /// apart from a project that does not mark an assembly lazy - the runtime names that one, and the app has to change
    /// - since a download failure is when someone troubleshoots, and the app may want to offer a fallback of its own.
    /// </summary>
    private async Task<string> DescribeLoadFailureAsync(IJSRuntime js, List<string> names, Exception exception)
    {
        var assemblies = names.Count == 0 ? AssemblyName : string.Join(", ", names);
        var message = exception.Message;
        if (message.Contains("BlazorWebAssemblyLazyLoad", StringComparison.Ordinal))
        {
            return "Loading " + assemblies + " failed: " + message;
        }

        LoadFailedToDownload = true;
        return assemblies + " could not be downloaded: " + (await IsOfflineAsync(js) ? "the browser went offline." : DescribeDownloadError(message))
            + " The runtime keeps a failed download until the page is reloaded: reload it to try again.";
    }

    private static async Task<bool> IsOfflineAsync(IJSRuntime js)
    {
        try
        {
            return await js.GetValueAsync<bool>("navigator.onLine") is false;
        }
        catch
        {
            // Not knowing whether the browser is online is no reason to report anything other than the error itself.
            return false;
        }
    }

    /// <summary>
    /// The runtime words a download the server refused as "download '&lt;url&gt;' for &lt;name&gt; failed &lt;status&gt;
    /// &lt;text&gt;"; any other failure (a connection that dropped, a fetch the browser refused) is its own message.
    /// </summary>
    internal static string DescribeDownloadError(string message)
    {
        const string marker = " failed ";
        var at = message.LastIndexOf(marker, StringComparison.Ordinal);
        if (at >= 0)
        {
            var status = message.AsSpan(at + marker.Length);
            var end = status.IndexOfAny('\r', '\n');
            status = (end < 0 ? status : status[..end]).Trim();
            if (status.Length >= 3 && char.IsAsciiDigit(status[0]) && char.IsAsciiDigit(status[1]) && char.IsAsciiDigit(status[2]) && (status.Length == 3 || status[3] == ' '))
            {
                return "the server answered HTTP " + status.ToString() + ".";
            }

            // Status 0 is a request that got no answer at all: the browser says why after it.
            if (status.Length >= 1 && status[0] == '0' && (status.Length == 1 || status[1] == ' '))
            {
                var reason = status[1..].Trim().ToString();
                return "no answer from the server" + (reason.Length == 0 ? "." : " (" + reason + ").");
            }
        }

        return message.EndsWith('.') ? message : message + ".";
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
