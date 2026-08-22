using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The lazy-scripts loader: makes sure the JavaScript module behind an interop identifier has been
/// <c>import()</c>ed into the page before the call goes through. See <see cref="BitButil.UseLazyScripts"/>.
/// </summary>
/// <remarks>
/// Every Butil interop identifier is <c>BitButil.&lt;module&gt;.&lt;function&gt;</c>, and every module is
/// published as a self-contained file <c>&lt;modules path&gt;/&lt;module&gt;.js</c> (it carries its own
/// dependencies and is safe to evaluate more than once - see <c>build.mjs</c>), so the identifier alone says
/// which one file to import. Modules are tracked per <see cref="IJSRuntime"/> - one per WebAssembly app or
/// Server circuit - through a <see cref="ConditionalWeakTable{TKey,TValue}"/> so nothing here outlives the
/// runtime it belongs to. Concurrent first calls into the same module share one import; a failed import is
/// forgotten so the next call retries instead of failing forever on a transient error.
/// </remarks>
internal static class ButilScriptLoader
{
    private static readonly ConditionalWeakTable<IJSRuntime, ConcurrentDictionary<string, Lazy<Task>>> LoadedModules = new();

    /// <summary>
    /// Completes once the module that serves <paramref name="identifier"/> is loaded into the runtime's page.
    /// Synchronously completed (no allocation, no await) after the first successful load of that module.
    /// </summary>
    internal static ValueTask EnsureLoaded(IJSRuntime jsRuntime, string identifier, CancellationToken cancellationToken)
    {
        if (TryGetModule(identifier, out var module) is false) return default;

        var modules = LoadedModules.GetValue(jsRuntime, static _ => new ConcurrentDictionary<string, Lazy<Task>>(StringComparer.Ordinal));
        if (modules.TryGetValue(module, out var loaded) && loaded.IsValueCreated && loaded.Value.IsCompletedSuccessfully) return default;

        return new ValueTask(LoadAsync(jsRuntime, modules, module, cancellationToken));
    }

    /// <summary>
    /// The module of an interop identifier: <c>BitButil.clipboard.readText</c> is served by <c>clipboard</c>.
    /// False for identifiers that are not Butil's (nothing to load for those).
    /// </summary>
    internal static bool TryGetModule(string identifier, out string module)
    {
        const string prefix = "BitButil.";
        module = string.Empty;

        if (identifier is null || identifier.StartsWith(prefix, StringComparison.Ordinal) is false) return false;

        var end = identifier.IndexOf('.', prefix.Length);
        if (end <= prefix.Length) return false;

        // A module name becomes part of an import path, so accept only what a module is actually named -
        // anything with punctuation or a path separator in it is not Butil's and must not reach import().
        for (var i = prefix.Length; i < end; i++)
        {
            if (char.IsLetterOrDigit(identifier[i]) is false && identifier[i] != '_') return false;
        }

        module = identifier.Substring(prefix.Length, end - prefix.Length);
        return true;
    }

    private static async Task LoadAsync(IJSRuntime jsRuntime, ConcurrentDictionary<string, Lazy<Task>> modules, string module, CancellationToken cancellationToken)
    {
        // The import itself is shared and never cancelled by one caller's token; each caller only stops
        // waiting for it. That keeps a cancelled first call from failing every other call to the module.
        await GetOrStartImport(jsRuntime, modules, module).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The one shared import task of a module, started on first use.
    /// </summary>
    private static Task GetOrStartImport(IJSRuntime jsRuntime, ConcurrentDictionary<string, Lazy<Task>> modules, string module)
    {
        // Through a Lazy rather than straight from the GetOrAdd factory: ConcurrentDictionary may run that
        // factory on more than one thread under contention and keep only one of the results, which here would
        // mean a second import() nobody ever awaits. Constructing a Lazy has no side effect, and only the
        // instance that wins the race has its Value read, so the import runs exactly once per module.
        Lazy<Task>? entry = null;
        entry = new Lazy<Task>(() =>
        {
            // A runtime that rejects the call outright (a disposed one, for instance) throws right here
            // instead of handing back a task; as a faulted task it takes the same path as any other failed
            // import - reported to the caller, forgotten, retried - rather than being cached by the Lazy as
            // a thrown exception, which no later call could ever get past.
            Task import;
            try
            {
                import = ImportAsync(jsRuntime, module);
            }
            catch (Exception exception)
            {
                import = Task.FromException(exception);
            }

            // Forget a failed import so a later call retries it; a lost connection or a 404 while a deploy
            // is rolling out should not pin the module as broken for the rest of the session. Hung off the
            // import rather than done in a caller's catch block, because by the time it fails every caller
            // may have stopped waiting (each waits under its own cancellation token) - which would leave the
            // failure cached for the next call, and its exception unobserved.
            import.ContinueWith(static (faulted, state) =>
            {
                _ = faulted.Exception; // Observed here; the callers still waiting see it through their await.
                var (map, name, self) = ((ConcurrentDictionary<string, Lazy<Task>> Map, string Name, Lazy<Task> Self))state!;
                map.TryRemove(new KeyValuePair<string, Lazy<Task>>(name, self));
            },
            (Map: modules, Name: module, Self: entry!),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

            return import;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        return modules.GetOrAdd(module, entry).Value;
    }

    private static Task ImportAsync(IJSRuntime jsRuntime, string module)
    {
        // "import" is JS interop's built-in dynamic import; the URL is resolved against the document's base
        // href, so the default "./_content/Bit.Butil/modules/<module>.js" is the package's static web asset.
        // The module has no exports (it registers itself on window.BitButil), so there is no reference to keep.
        return jsRuntime.InvokeVoidAsync("import", BitButil.ScriptModulesPath + module + ".js").AsTask();
    }
}
