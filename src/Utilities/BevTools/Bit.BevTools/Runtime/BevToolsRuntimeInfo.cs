using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bit.BevTools;

/// <summary>
/// Diagnostics that differ by where a Blazor app renders. Blazor Server shares one process, thread pool and heap with
/// every other circuit; Blazor WebAssembly runs a .NET runtime in the browser tab, single-threaded, with its own feature
/// switches; a Blazor Web App mixes static rendering, prerendering and interactive render modes on one page. Each gets
/// the facts that explain its own failure modes, read from public runtime APIs and labeled with their scope.
/// </summary>
internal static class BevToolsRuntimeInfo
{
    public static IReadOnlyList<BevToolsRuntimeSection> Build(BevToolsSession session)
    {
        var sections = new List<BevToolsRuntimeSection> { Rendering(session) };
        switch (session.Platform)
        {
            case "Server":
                sections.Add(ServerHost());
                break;
            case "WebAssembly":
                sections.Add(WebAssemblyHost(session));
                break;
            case "WebView":
                sections.Add(new BevToolsRuntimeSection("Blazor Hybrid host", "this app", [new("Renderer", "WebView: the .NET process renders into a native web view; no circuit and no network hop sit in between.")]));
                break;
        }

        if (session.Prerender is { } prerender)
        {
            sections.Add(Prerendering(prerender));
        }

        sections.Add(DotNet(session));
        return sections;
    }

    private static BevToolsRuntimeSection Rendering(BevToolsSession session)
    {
        var facts = new List<BevToolsRuntimeFact>
        {
            new("Renderer", (session.Platform ?? "unknown") + (session.IsInteractive ? ", interactive" : ", static"),
                session.Platform switch
                {
                    "Server" => "Components run on the server; every UI event and every render batch crosses the circuit's connection.",
                    "WebAssembly" => "Components run in the browser on the .NET WebAssembly runtime; nothing crosses the network to render.",
                    "WebView" => "Components run in the app's own process and render into a web view.",
                    _ => null,
                }),
        };

        if (session.RenderModeName is { } mode)
        {
            facts.Add(new("Render mode", mode));
        }

        var boundaries = session.Components.All.Where(r => r.RenderModeName is not null && !r.IsBevTools && !r.IsDisposed).ToArray();
        facts.Add(boundaries.Length == 0
            ? new("Render mode boundaries", "none in this session", "A component with @rendermode (or a [RenderMode*] attribute) starts an interactive subtree; none of the components this session tracks carries one.")
            : new("Render mode boundaries", string.Join(", ", boundaries.GroupBy(b => b.RenderModeName!).Select(g => g.Key + " × " + g.Count())),
                "Each is the root of an interactive subtree; the Components tree marks them."));
        facts.Add(new("Prerendered", session.Prerender is { } p ? $"yes: {p.Components} components rendered on the server in {BevToolsFmt.Ms(p.DurationMs)} before this session started" : "no summary received",
            session.Prerender is null ? "Either this page was not prerendered, the BevTools panel was not part of the prerendered output, or value access is below Full, which keeps the summary out of the page." : null));
        facts.Add(new("Value access", session.ValueAccess.ToString(), session.ValueAccess switch
        {
            BevToolsValueAccess.Full => "Parameters, fields and state are read and shown, with redaction.",
            BevToolsValueAccess.Shallow => "Only scalar values are shown; objects show their type. Forced re-render detection is off.",
            _ when session.ValueAccessIsConfigured => "No application value is read: BevToolsOptions.ValueAccess is set to None.",
            _ => "No application value is read: this process serves many users outside Development. Set BevToolsOptions.ValueAccess to change it.",
        }, session.ValueAccess == BevToolsValueAccess.Full ? BevToolsSeverity.Info : BevToolsSeverity.Warning));
        return new BevToolsRuntimeSection("Rendering", "this session", facts);
    }

    private static BevToolsRuntimeSection ServerHost()
    {
        ThreadPool.GetMinThreads(out var minWorkers, out _);
        var threads = ThreadPool.ThreadCount;
        var pending = ThreadPool.PendingWorkItemCount;
        // Work queued faster than the pool runs it while the pool is still at its minimum: the shape of thread pool
        // starvation, which on Blazor Server freezes every circuit at once (sync-over-async in a handler, typically).
        var starving = pending > Math.Max(threads, minWorkers) * 2;
        return new BevToolsRuntimeSection("Server process", "shared by every circuit in this process",
        [
            new("Live BevTools sessions", BevToolsSessionResolver.LiveSessionCount.ToString(), "Circuits and requests BevTools is tracking in this process right now."),
            new("Thread pool", $"{threads} threads (minimum {minWorkers}), {pending} work items waiting, {ThreadPool.CompletedWorkItemCount:N0} completed",
                starving ? "More work is waiting than the pool can start: blocking waits (.Result, .Wait(), sync I/O) in handlers or lifecycle methods starve every circuit. The pool adds threads slowly." : null,
                starving ? BevToolsSeverity.Warning : BevToolsSeverity.Info),
            new("Lock contention", Monitor.LockContentionCount.ToString("N0") + " contended lock acquisitions since the process started"),
            new("Working set", BevToolsFmt.Bytes(Environment.WorkingSet)),
            new("Processors", Environment.ProcessorCount.ToString()),
            new("GC mode", (GCSettings.IsServerGC ? "server" : "workstation") + ", " + GCSettings.LatencyMode),
        ]);
    }

    private static BevToolsRuntimeSection WebAssemblyHost(BevToolsSession session)
    {
        var metrics = BevToolsCapabilityReport.MetricsAreSupported();
        var invariant = AppContext.TryGetSwitch("System.Globalization.Invariant", out var isInvariant) && isInvariant;
        return new BevToolsRuntimeSection("WebAssembly runtime", "this browser tab",
        [
            new("Code execution", RuntimeFeature.IsDynamicCodeCompiled ? "JIT-compiled" : "interpreted or AOT-compiled (no JIT)",
                RuntimeFeature.IsDynamicCodeCompiled ? null : "The browser runs .NET without a JIT: tight loops and reflection-heavy code cost more than on the server. AOT compilation (RunAOTCompilation) trades download size for speed."),
            new("Threads", "single-threaded", "Everything - rendering, event handlers, awaited continuations - shares the browser's UI thread. A long synchronous handler freezes the page."),
            new("Metrics support", metrics ? "on" : "off (WebAssembly default)",
                metrics ? null : "Add <MetricsSupport>true</MetricsSupport> to the project for render-diff and lifecycle timings.",
                metrics ? BevToolsSeverity.Info : BevToolsSeverity.Warning),
            new("Globalization", invariant ? "invariant mode" : "ICU data loaded"),
            new("Loaded on demand", session.IsAttachedAtRuntime ? "yes: BevTools was attached to the running app" : "no: registered at startup"),
        ]);
    }

    private static BevToolsRuntimeSection Prerendering(BevToolsPrerenderSummary prerender)
    {
        var facts = new List<BevToolsRuntimeFact>
        {
            new("Page", prerender.Path ?? "?"),
            new("Server render", $"{prerender.Components} components, {prerender.Renders} renders, {BevToolsFmt.Ms(prerender.RenderMs)} building, {BevToolsFmt.Ms(prerender.DurationMs)} in all"),
        };
        if (prerender.RenderMode is { } mode)
        {
            facts.Add(new("Prerendered for", mode));
        }

        foreach (var component in prerender.Slowest.Take(5))
        {
            facts.Add(new("Slowest: " + component.Name, $"{component.Renders} renders, {BevToolsFmt.Ms(component.TotalMs)}"));
        }

        facts.Add(new("Requests while prerendering", prerender.Requests.Count == 0 ? "none" : string.Join(", ", prerender.Requests.Take(8).Select(r => r.Method + " " + BevToolsHttpMessageHandler.StripOrigin(r.Url) + " → " + (r.StatusCode?.ToString() ?? r.Error ?? "?"))),
            prerender.Requests.Count == 0 ? null : "Each of these ran on the server for the HTML; the Diagnostics panel flags the ones this session requested again."));
        if (prerender.Errors.Count > 0)
        {
            facts.Add(new("Errors while prerendering", string.Join("; ", prerender.Errors.Take(5)), Severity: BevToolsSeverity.Error));
        }

        return new BevToolsRuntimeSection("Prerendering", "the server render this page started from", facts);
    }

    private static BevToolsRuntimeSection DotNet(BevToolsSession session)
    {
        var gc = GC.GetGCMemoryInfo();
        var scope = session.Platform == "Server" ? "shared by every circuit in this process" : "this app";
        return new BevToolsRuntimeSection(".NET runtime", scope,
        [
            new("Runtime", RuntimeInformation.FrameworkDescription + " · " + RuntimeInformation.ProcessArchitecture + " · " + RuntimeInformation.OSDescription),
            new("Components", typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly.GetName().Version?.ToString() ?? "?"),
            new("Managed heap", BevToolsFmt.Bytes(GC.GetTotalMemory(false)) + " in use, " + BevToolsFmt.Bytes(gc.HeapSizeBytes) + " heap"),
            new("Allocated", BevToolsFmt.Bytes(GC.GetTotalAllocatedBytes()) + " since start"),
            new("Collections", $"gen0 {GC.CollectionCount(0):N0} · gen1 {GC.CollectionCount(1):N0} · gen2 {GC.CollectionCount(2):N0} · {BevToolsFmt.Ms(GC.GetTotalPauseDuration().TotalMilliseconds)} paused"),
        ]);
    }
}
