using System.Diagnostics.CodeAnalysis;
using Bit.Butil;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace ButilTests.Manual;

/// <summary>
/// Exercises the lazy-scripts loader against a recording <see cref="IJSRuntime"/>: with
/// <see cref="BitButil.UseLazyScripts"/> on, the first call into an API must <c>import()</c> that API's
/// module - and only that one - before invoking it, later calls must not import again, a failed import must
/// be retried, and with lazy scripts off nothing may be imported at all.
/// </summary>
/// <remarks>
/// This is the one place the loader's behaviour is pinned outside a browser. It runs in both the untrimmed
/// and the trimmed harness, so it also proves the lazy path survives trimming even though this project's
/// build never sets the <c>BitButilLazyScripts</c> switch (the runtime override has to keep working in a
/// trimmed app for hosts that cannot use the MSBuild property).
/// </remarks>
internal static class LazyScripts
{
    private const string ModulesPath = "./_content/Bit.Butil/modules/";

    public static async Task<(int Passed, int Failed)> Run(List<string> failures)
    {
        var passed = 0;
        var before = failures.Count;

        // Lazy mode. A fresh runtime per scenario: the loader tracks loaded modules per IJSRuntime instance.
        BitButil.UseLazyScripts();
        try
        {
            var runtime = new RecordingJSRuntime();
            var (clipboard, window) = Resolve(runtime);

            await clipboard.IsSupported();
            Expect(runtime, ["import " + ModulesPath + "clipboard.js", "BitButil.clipboard.isSupported"],
                "the first call into Clipboard imports clipboard.js and then invokes", failures, ref passed);

            await clipboard.IsSupported();
            Expect(runtime, ["import " + ModulesPath + "clipboard.js", "BitButil.clipboard.isSupported", "BitButil.clipboard.isSupported"],
                "a second call into Clipboard does not import again", failures, ref passed);

            await window.GetLocationBar();
            Expect(runtime, ["import " + ModulesPath + "clipboard.js", "BitButil.clipboard.isSupported", "BitButil.clipboard.isSupported", "import " + ModulesPath + "window.js", "BitButil.window.locationbar"],
                "the first call into Window imports window.js only (clipboard.js is not re-imported)", failures, ref passed);

            // Two services on the same runtime instance share the loaded set: a second Clipboard resolved
            // from a new scope must not import clipboard.js again.
            var (clipboardAgain, _) = Resolve(runtime);
            await clipboardAgain.IsSupported();
            Expect(runtime, ["import " + ModulesPath + "clipboard.js", "BitButil.clipboard.isSupported", "BitButil.clipboard.isSupported", "import " + ModulesPath + "window.js", "BitButil.window.locationbar", "BitButil.clipboard.isSupported"],
                "modules are tracked per runtime, not per service instance", failures, ref passed);

            // A failed import is not remembered as loaded: the next call retries it.
            var failing = new RecordingJSRuntime { FailNextImport = true };
            var (flaky, _) = Resolve(failing);
            var threw = false;
            try { await flaky.IsSupported(); } catch (JSException) { threw = true; }
            if (threw is false) failures.Add("lazy scripts: a failed import() did not surface as an exception to the caller.");
            else passed++;
            await flaky.IsSupported();
            Expect(failing, ["import " + ModulesPath + "clipboard.js", "import " + ModulesPath + "clipboard.js", "BitButil.clipboard.isSupported"],
                "a failed import is retried on the next call", failures, ref passed);

            // A custom modules path is honoured.
            BitButil.UseLazyScripts("/cdn/butil");
            var relocated = new RecordingJSRuntime();
            var (fromCdn, _) = Resolve(relocated);
            await fromCdn.IsSupported();
            Expect(relocated, ["import /cdn/butil/clipboard.js", "BitButil.clipboard.isSupported"],
                "UseLazyScripts(modulesPath) changes where modules are imported from", failures, ref passed);
        }
        finally
        {
            BitButil.UseLazyScripts(ModulesPath);
            BitButil.UseBundledScripts();
        }

        // Bundle mode (the default, and what the rest of this harness runs under): no imports, ever.
        {
            var runtime = new RecordingJSRuntime();
            var (clipboard, window) = Resolve(runtime);
            await clipboard.IsSupported();
            await window.GetLocationBar();
            Expect(runtime, ["BitButil.clipboard.isSupported", "BitButil.window.locationbar"],
                "with bundled scripts nothing is imported", failures, ref passed);
        }

        // The same switches through the options overload of AddBitButilServices - the C# alternative to the
        // MSBuild property - including turning lazy scripts back off and leaving the mode alone (null).
        try
        {
            var runtime = new RecordingJSRuntime();
            var (clipboard, _) = Resolve(runtime, options => { options.LazyScripts = true; options.ScriptModulesPath = "/static/butil"; });
            await clipboard.IsSupported();
            Expect(runtime, ["import /static/butil/clipboard.js", "BitButil.clipboard.isSupported"],
                "AddBitButilServices(options) with LazyScripts = true and a modules path imports from that path", failures, ref passed);

            var untouched = new RecordingJSRuntime();
            var (stillLazy, _) = Resolve(untouched, options => { options.FastInvoke = false; });
            await stillLazy.IsSupported();
            Expect(untouched, ["import /static/butil/clipboard.js", "BitButil.clipboard.isSupported"],
                "AddBitButilServices(options) with LazyScripts left null keeps the current mode", failures, ref passed);

            var bundled = new RecordingJSRuntime();
            var (backToBundle, _) = Resolve(bundled, options => options.LazyScripts = false);
            await backToBundle.IsSupported();
            Expect(bundled, ["BitButil.clipboard.isSupported"],
                "AddBitButilServices(options) with LazyScripts = false turns lazy scripts off again", failures, ref passed);
        }
        finally
        {
            BitButil.UseLazyScripts(ModulesPath);
            BitButil.UseBundledScripts();
        }

        return (passed, failures.Count - before);
    }

    private static (Clipboard Clipboard, Window Window) Resolve(IJSRuntime runtime, Action<BitButilOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(runtime);
        if (configure is null) services.AddBitButilServices();
        else services.AddBitButilServices(configure);
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        // Non-generic resolution, for the same reason as ConsumerComponent: it is what @inject compiles to.
        return ((Clipboard)scope.ServiceProvider.GetRequiredService(typeof(Clipboard)), (Window)scope.ServiceProvider.GetRequiredService(typeof(Window)));
    }

    private static void Expect(RecordingJSRuntime runtime, string[] expected, string what, List<string> failures, ref int passed)
    {
        if (runtime.Calls.SequenceEqual(expected, StringComparer.Ordinal))
        {
            passed++;
            return;
        }

        failures.Add($"lazy scripts: {what} - expected [{string.Join(" | ", expected)}] but saw [{string.Join(" | ", runtime.Calls)}].");
    }

    /// <summary>
    /// Records every identifier invoked (an <c>import</c> together with the URL it was asked to load), answers
    /// with <c>default</c>, and can be told to fail its next import to exercise the retry path.
    /// </summary>
    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public List<string> Calls { get; } = [];

        public bool FailNextImport { get; set; }

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import")
            {
                Calls.Add($"import {args?[0]}");
                if (FailNextImport)
                {
                    FailNextImport = false;
                    return ValueTask.FromException<TValue>(new JSException("simulated failed import"));
                }
            }
            else
            {
                Calls.Add(identifier);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
