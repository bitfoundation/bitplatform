using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// The .NET side of the lazy scripts (see <see cref="BitBlazorUIScripts"/>): makes sure the module behind an interop
/// identifier has been <c>import()</c>ed into the page before the call goes through.
/// </summary>
/// <remarks>
/// Every interop identifier of the packages is <c>BitBlazorUI.&lt;path&gt;</c>, and the packages' embedded manifests
/// (<see cref="BitLazyScriptPackage"/>) say which module exports that path - the same answer the publish-time trimming gives
/// it. A module imports the modules of its own package it uses; what it uses of another package's namespace, and the boot
/// module of its package (the scripts the app starts with, normally imported by the package's JavaScript initializer
/// already), are imported here first, since only this side knows where each package is served from.
/// <br />
/// What has been loaded is tracked per <see cref="IJSRuntime"/> - one per WebAssembly app, Server circuit or WebView page -
/// through a <see cref="ConditionalWeakTable{TKey, TValue}"/>, so nothing outlives the runtime it belongs to. A module
/// imported a second time (another runtime on the same page, as in the Auto render mode) is the one the browser already
/// evaluated, never a second copy. Concurrent first calls share one import, and an import that does not succeed is
/// forgotten so that the next call tries again.
/// </remarks>
internal static class BitScriptLoader
{
    private const string Namespace = "BitBlazorUI.";

    private static readonly ConditionalWeakTable<IJSRuntime, RuntimeState> States = new();

    // The packages whose lazy scripts have been read: every assembly of a bit BlazorUI package loaded so far, read once.
    private static readonly object PackagesLock = new();
    private static readonly HashSet<string> ReadAssemblies = new(StringComparer.Ordinal);
    private static volatile BitLazyScriptPackage[] _packages = [];
    private static int _assembliesSeen;

    // What an identifier resolves to, the same for every runtime: the modules it calls into and whether it is a teardown.
    private static readonly ConcurrentDictionary<string, Resolution> Resolutions = new(StringComparer.Ordinal);

    private sealed class Resolution(BitLazyScriptModule[] modules, bool teardown)
    {
        public BitLazyScriptModule[] Modules { get; } = modules;

        public bool Teardown { get; } = teardown;
    }

    /// <summary>
    /// What one runtime has loaded. The question asked on every interop call is about an identifier, which is always the
    /// same string (an interned literal for all but BitMap's providers), so its answer is cached against it: an identifier
    /// in <see cref="Ready"/> needs nothing done before the call, because its modules are loaded or it is not the packages'.
    /// </summary>
    private sealed class RuntimeState
    {
        internal readonly ConcurrentDictionary<string, bool> Ready = new(StringComparer.Ordinal);
        internal readonly ConcurrentDictionary<string, Lazy<Task>> Imports = new(StringComparer.Ordinal);
        internal readonly ConcurrentDictionary<BitLazyScriptModule, bool> Loaded = new();
    }

    /// <summary>
    /// Completes once the modules that serve <paramref name="identifier"/> are loaded into the runtime's page, with true
    /// when the call is to be made - and false for a teardown function of a script this page never ran, which has nothing
    /// to undo there (a component disposed before it ever called into its script): importing it only to call that would
    /// download a script for nothing. Completed synchronously, without an allocation, in bundle mode and from the second
    /// call of an identifier on.
    /// </summary>
    internal static ValueTask<bool> EnsureLoaded(IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken)
    {
        if (BitBlazorUIScripts.LazyScriptsEnabled is false || identifier is null) return new(true);

        var state = States.GetValue(jsRuntime, static _ => new RuntimeState());
        if (state.Ready.ContainsKey(identifier)) return new(true);

        var resolution = Resolve(identifier);
        if (resolution is null)
        {
            // Not an identifier of the packages: nothing to load, now or later.
            state.Ready[identifier] = true;
            return new(true);
        }

        if (resolution.Teardown && resolution.Modules.Any(state.Loaded.ContainsKey) is false) return new(false);

        if (resolution.Modules.All(state.Loaded.ContainsKey))
        {
            state.Ready[identifier] = true;
            return new(true);
        }

        return new(LoadAsync(jsRuntime, state, identifier, resolution.Modules, cancellationToken));
    }

    /// <summary>
    /// Completes once a script of a package is loaded: for a component whose script acts on its markup without being called
    /// (it is kept for the component by a <c>bit-trim: owners</c> comment, and adds its listeners as it loads), which in lazy
    /// mode nothing else would ever load.
    /// </summary>
    internal static ValueTask EnsureScript(IJSRuntime jsRuntime, string package, string script)
    {
        if (BitBlazorUIScripts.LazyScriptsEnabled is false) return ValueTask.CompletedTask;

        // A package whose assembly loaded after the last read is read now (the component asking is of it).
        var module = Packages().FirstOrDefault(p => p.Id == package)?.FindModule(script)
                  ?? RefreshPackages()?.FirstOrDefault(p => p.Id == package)?.FindModule(script);
        if (module is null) return ValueTask.CompletedTask;

        var state = States.GetValue(jsRuntime, static _ => new RuntimeState());
        if (state.Loaded.ContainsKey(module)) return ValueTask.CompletedTask;

        return new(ImportAsync(jsRuntime, state, module, CancellationToken.None, []));
    }

    /// <summary>The URL a file of a package's static web assets is imported from.</summary>
    internal static string UrlOf(BitLazyScriptPackage package, string path) => $"{BitBlazorUIScripts.ContentPath}{package.Id}/{path}";

    private static async Task<bool> LoadAsync(IJSRuntime jsRuntime, RuntimeState state, string identifier, BitLazyScriptModule[] modules, CancellationToken cancellationToken)
    {
        foreach (var module in modules)
        {
            await ImportAsync(jsRuntime, state, module, cancellationToken, []).ConfigureAwait(false);
        }

        // Reached only on success: an import that threw or was cancelled leaves the identifier out, and the next call tries
        // again.
        state.Ready[identifier] = true;
        return true;
    }

    private static async Task ImportAsync(IJSRuntime jsRuntime, RuntimeState state, BitLazyScriptModule module, CancellationToken cancellationToken, HashSet<BitLazyScriptModule> visiting)
    {
        if (state.Loaded.ContainsKey(module) || visiting.Add(module) is false) return;

        var package = module.Package;

        // The scripts the package starts with, which its JavaScript initializer imports as the app starts - here as well,
        // for a host that runs no initializer, and so that they run ahead of every other script of the package wherever
        // the initializer has not finished yet. A module the browser already evaluated is not evaluated again.
        if (package.Boot is not null)
        {
            await Import(jsRuntime, state, UrlOf(package, package.Boot)).WaitAsync(cancellationToken).ConfigureAwait(false);
            foreach (var booted in package.Modules.Where(m => m.Boot).SelectMany(m => m.Closure)) state.Loaded[booted] = true;

            // A script the boot module imports (Utils.Keys, the theme) has nothing left to import.
            if (state.Loaded.ContainsKey(module)) return;
        }

        foreach (var external in ExternalModulesOf(module))
        {
            await ImportAsync(jsRuntime, state, external, cancellationToken, visiting).ConfigureAwait(false);
        }

        // The import itself is shared and never cancelled by one caller's token; each caller only stops waiting for it.
        await Import(jsRuntime, state, UrlOf(package, module.Path)).WaitAsync(cancellationToken).ConfigureAwait(false);

        foreach (var loaded in module.Closure) state.Loaded[loaded] = true;
    }

    // What a module uses of other packages, resolved against their exports the first time it is imported.
    private static BitLazyScriptModule[] ExternalModulesOf(BitLazyScriptModule module)
    {
        if (module.ExternalModules is { } resolved) return resolved;

        var externals = new List<BitLazyScriptModule>();
        foreach (var path in module.Externals)
        {
            if (ResolvePath(path) is { } modules) externals.AddRange(modules.Where(m => m.Package != module.Package));
        }

        return module.ExternalModules = [.. externals.Distinct()];
    }

    /// <summary>The one shared import of a URL in a runtime, started on first use.</summary>
    private static Task Import(IJSRuntime jsRuntime, RuntimeState state, string url)
    {
        // Through a Lazy rather than the GetOrAdd factory itself, which may run more than once under contention and keep
        // only one of the results: here that would be a second import() nobody waits for.
        Lazy<Task>? entry = null;
        entry = new Lazy<Task>(() =>
        {
            // A runtime that rejects the call outright (a disposed one) throws here rather than handing back a task; as a
            // faulted task it is reported, forgotten and tried again like any other failed import.
            Task import;
            try
            {
                import = jsRuntime.InvokeVoidAsync("import", url).AsTask();
            }
            catch (Exception exception)
            {
                import = Task.FromException(exception);
            }

            // Forgotten when it does not succeed, so a lost connection or a 404 during a deploy does not pin the script as
            // broken for the rest of the session. Hung off the import rather than a caller's catch, since every caller may
            // have stopped waiting by then; not only on a fault, since an import past the interop timeout ends cancelled.
            import.ContinueWith(static (completed, value) =>
            {
                _ = completed.Exception;
                var (imports, key, self) = ((ConcurrentDictionary<string, Lazy<Task>> Imports, string Key, Lazy<Task> Self))value!;
                imports.TryRemove(new KeyValuePair<string, Lazy<Task>>(key, self));
            },
            (Imports: state.Imports, Key: url, Self: entry!),
            CancellationToken.None,
            TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

            return import;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        return state.Imports.GetOrAdd(url, entry).Value;
    }

    private static Resolution? Resolve(string identifier)
    {
        if (Resolutions.TryGetValue(identifier, out var cached)) return cached;
        if (identifier.StartsWith(Namespace, StringComparison.Ordinal) is false) return null;

        var path = identifier.Substring(Namespace.Length);
        if (IsPath(path) is false) return null;

        var modules = ResolvePath(path);
        if (modules is null) return null;

        var resolution = new Resolution(modules, modules.Length == 1 && modules[0].Teardown.Contains(path));
        return Resolutions.GetOrAdd(identifier, resolution);
    }

    /// <summary>
    /// The modules a path under the namespace calls into: the module exporting it, or the longest part of it ("Callouts"
    /// for Callouts.toggle) - or, for a path stopping at a namespace several files add to, every module adding to it.
    /// </summary>
    private static BitLazyScriptModule[]? ResolvePath(string path)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var packages = attempt == 0 ? Packages() : RefreshPackages();
            if (packages is null) break;

            for (var key = path; key.Length > 0;)
            {
                var found = packages.Select(p => p.FindExport(key)).Where(m => m is not null).SelectMany(m => m!).ToArray();
                if (found.Length > 0) return found;

                var dot = key.LastIndexOf('.');
                if (dot < 0) break;
                key = key.Substring(0, dot);
            }

            var under = packages.SelectMany(p => p.FindUnder(path)).Distinct().ToArray();
            if (under.Length > 0) return under;
        }

        return null;
    }

    // A path an import URL is made from takes only what a member of the namespace is named of.
    private static bool IsPath(string path)
    {
        if (path.Length == 0) return false;

        foreach (var c in path)
        {
            if (char.IsLetterOrDigit(c) is false && c != '_' && c != '$' && c != '.') return false;
        }

        return true;
    }

    private static BitLazyScriptPackage[] Packages() => _packages.Length > 0 ? _packages : RefreshPackages() ?? [];

    /// <summary>
    /// Reads the lazy scripts of every package assembly loaded since the last read; null when there is nothing new, so a
    /// caller that asked because of a miss knows a second look would find the same.
    /// </summary>
    private static BitLazyScriptPackage[]? RefreshPackages()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        if (assemblies.Length == Volatile.Read(ref _assembliesSeen) && _packages.Length > 0) return null;

        lock (PackagesLock)
        {
            var packages = new List<BitLazyScriptPackage>(_packages);
            foreach (var assembly in assemblies)
            {
                var name = assembly.GetName().Name;
                if (name is null || name.StartsWith("Bit.BlazorUI", StringComparison.Ordinal) is false || ReadAssemblies.Add(name) is false) continue;

                if (BitLazyScriptPackage.Load(assembly) is { } package) packages.Add(package);
            }

            _packages = [.. packages];
            Volatile.Write(ref _assembliesSeen, assemblies.Length);
            return _packages;
        }
    }
}
