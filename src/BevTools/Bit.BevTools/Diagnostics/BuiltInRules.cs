
namespace Bit.BevTools;

internal static class BuiltInRules
{
    public static IEnumerable<IBevDiagnosticRule> Create() =>
    [
        new RenderStormRule(),
        new ParentDrivenRenderRule(),
        new RenderCascadeRule(),
        new SlowRenderRule(),
        new SlowEventHandlerRule(),
        new DuplicateHttpRule(),
        new SlowHttpRule(),
        new RepeatedHttpFailureRule(),
        new SlowJsInteropRule(),
        new ExcessiveJsInteropRule(),
        new RepeatedErrorRule(),
    ];

    private static BevDiagnosticsOptions Opt(IBevDiagnosticContext ctx) => ctx.GetOptions<BevDiagnosticsOptions>() ?? new BevDiagnosticsOptions();

    private static string Ms(double value) => value.ToString("0.#") + " ms";

    /// <summary>
    /// A component that only ever re-renders because an ancestor did. This is the most common avoidable cost in a
    /// Blazor application and it is invisible to a rate-based rule: a dashboard re-rendering twice a second never
    /// reaches a "storm" threshold, yet every one of those renders is work nobody asked for.
    /// </summary>
    private sealed class ParentDrivenRenderRule : IBevDiagnosticRule
    {
        public string Id => "render.parent-driven";

        public string Title => "Renders driven only by the parent";

        public string Description => "A component re-renders only because an ancestor re-rendered, never because of its own state or an event.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            var window = TimeSpan.FromSeconds(10);
            var renders = context.EventsInWindow(BevToolsEventKind.Render, window).Where(e => e.ComponentInstanceId is not null).ToList();
            foreach (var group in renders.GroupBy(e => e.ComponentInstanceId!.Value))
            {
                if (context.IsFrameworkInternal(group.Key))
                {
                    continue;
                }

                var all = group.ToList();
                var parentDriven = all.Count(e => string.Equals(e.DataString("cause"), "ParentRender", StringComparison.Ordinal));
                if (all.Count < options.ParentDrivenRenderCount || parentDriven < all.Count - 1)
                {
                    continue;
                }

                var name = all[0].ComponentName ?? "component";
                var ancestor = all.Select(e => e.Detail).FirstOrDefault(d => d is not null)?.Replace(" re-rendered (parameters re-applied)", "") ?? "its parent";
                var totalMs = all.Sum(e => e.DurationMs ?? 0);
                // Parameter comparison only runs while the component is inspected, so this is extra evidence when
                // present and silence when absent, never a guess.
                var compared = all.Count(e => e.TryGetData("parametersUnchanged", out var v) && v is bool);
                var unchanged = all.Count(e => e.TryGetData("parametersUnchanged", out var v) && v is true);
                var parameterEvidence = compared == 0
                    ? "Select the component in the inspector to check whether the re-applied parameter values actually differ."
                    : unchanged == compared
                        ? $"All {compared} renders whose parameters were compared received values identical to the previous render: pure waste."
                        : $"{unchanged} of {compared} compared renders received identical parameter values; the others got new values.";
                var severity = compared > 0 && unchanged == compared ? BevDiagnosticSeverity.Warning : BevDiagnosticSeverity.Info;
                yield return new BevDiagnostic(
                    Id,
                    severity,
                    $"{name} re-rendered {parentDriven} times, every one of them because {ancestor} re-rendered",
                    $"None of these renders came from an event, navigation or its own state; total build time {Ms(totalMs)} in the last 10 s. "
                        + "The parent re-applies parameters on every one of its own renders, and Blazor cannot know the values are equivalent. "
                        + parameterEvidence,
                    "Override ShouldRender, pass values the parent does not recreate per render (no new lambdas, collections or anonymous objects), or move the fast-changing state out of the parent so it does not re-render at all.",
                    Fingerprint: $"{Id}:{group.Key}",
                    ComponentInstanceId: group.Key,
                    RelatedEventIds: all.Take(20).Select(e => e.Id).ToList());
            }
        }
    }

    /// <summary>Many renders of one component in a short window. Evidence includes the cause breakdown so the developer knows where to look.</summary>
    private sealed class RenderStormRule : IBevDiagnosticRule
    {
        public string Id => "render.storm";

        public string Title => "Render storm";

        public string Description => "A component rendered many times within a short window.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            var window = TimeSpan.FromMilliseconds(options.RenderStormWindowMs);
            var renders = context.EventsInWindow(BevToolsEventKind.Render, window).Where(e => e.ComponentInstanceId is not null).ToList();
            foreach (var group in renders.GroupBy(e => e.ComponentInstanceId!.Value))
            {
                var count = group.Count();
                if (count < options.RenderStormCount)
                {
                    continue;
                }

                var name = group.First().ComponentName ?? "component";
                var causes = group.GroupBy(e => e.DataString("cause") ?? "Unknown").OrderByDescending(g => g.Count()).ToList();
                var breakdown = string.Join("; ", causes.Select(c => $"{c.Count()} × {Describe(c.Key, c)}"));
                var totalMs = group.Sum(e => e.DurationMs ?? 0);
                var suggestion = causes[0].Key switch
                {
                    "ParentRender" => "The parent re-renders and re-applies parameters. Override ShouldRender, pass stable (memoized) parameter values, or move fast-changing state out of the parent.",
                    "UiEvent" => "Each event handler triggers a render. Debounce the input or batch updates.",
                    "StateHasChanged" => "Renders come from StateHasChanged outside events: timers, state-container notifications or async continuations. Check the subscription that calls StateHasChanged.",
                    _ => "Inspect the render causes in the Profiler panel.",
                };
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"{name} rendered {count} times in {options.RenderStormWindowMs / 1000.0:0.#} s",
                    $"{breakdown}. Total build time {Ms(totalMs)}.",
                    suggestion,
                    Fingerprint: $"{Id}:{group.Key}",
                    ComponentInstanceId: group.Key,
                    RelatedEventIds: group.Take(20).Select(e => e.Id).ToList());
            }
        }

        private static string Describe(string cause, IEnumerable<BevToolsEvent> events) => cause switch
        {
            "ParentRender" => "parent re-render (" + (events.Select(e => e.Detail).FirstOrDefault(d => d is not null)?.Replace(" re-rendered (parameters re-applied)", "") ?? "?") + ")",
            "UiEvent" => "UI event (" + string.Join(", ", events.Select(e => e.Detail?.Replace("Event: ", "")).Where(d => d is not null).Distinct().Take(3)) + ")",
            "StateHasChanged" => "StateHasChanged outside an event",
            "Initial" => "first render",
            "Navigation" => "navigation",
            _ => cause,
        };
    }

    private sealed class RenderCascadeRule : IBevDiagnosticRule
    {
        public string Id => "render.cascade";

        public string Title => "Render cascade";

        public string Description => "One render batch re-rendered a large number of components.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            foreach (var batch in context.EventsInWindow(BevToolsEventKind.RenderBatch, TimeSpan.FromSeconds(30)))
            {
                if (batch.Data is null || !batch.Data.TryGetValue("count", out var countObj) || countObj is not int count || count < options.RenderCascadeSize)
                {
                    continue;
                }

                var renders = context.Events.Where(e => e.Kind == BevToolsEventKind.Render && e.ParentEventId == batch.Id).ToList();
                var root = context.Events.FirstOrDefault(e => e.Id == batch.ParentEventId);
                var first = renders.FirstOrDefault();
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"Render batch touched {count} components",
                    (root is not null ? $"Triggered by {root.Title}. " : "") + (first is not null ? $"Started with {first.ComponentName}. " : "") + $"Building took {Ms(batch.DurationMs ?? 0)}.",
                    "A cascade usually means a high-level component (layout, cascading value provider) re-rendered. Narrow what changes: cascade immutable values, or give children ShouldRender/keyed parameters.",
                    Fingerprint: $"{Id}:{batch.Id}",
                    RelatedEventIds: [batch.Id]);
            }
        }
    }

    private sealed class SlowRenderRule : IBevDiagnosticRule
    {
        public string Id => "render.slow";

        public string Title => "Slow render";

        public string Description => "A component's BuildRenderTree takes long.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            foreach (var component in context.Components)
            {
                if (component.IsDisposed || component.RenderCount == 0)
                {
                    continue;
                }

                if (component.MaxRenderMs >= options.SlowRenderMs && (component.RenderCount >= 3 ? component.AverageRenderMs >= options.SlowRenderMs / 4 : true))
                {
                    yield return new BevDiagnostic(
                        Id,
                        BevDiagnosticSeverity.Warning,
                        $"{component.DisplayName} renders slowly (max {Ms(component.MaxRenderMs)})",
                        $"{component.RenderCount} renders, average {Ms(component.AverageRenderMs)}, total {Ms(component.TotalRenderMs)}. Measured time covers BuildRenderTree only, not DOM diffing.",
                        "Move work out of the render path: compute derived data in OnParametersSet, cache formatted values, virtualize large lists, and avoid LINQ over large collections inside markup.",
                        Fingerprint: $"{Id}:{component.InstanceId}",
                        ComponentInstanceId: component.InstanceId);
                }
            }
        }
    }

    private sealed class SlowEventHandlerRule : IBevDiagnosticRule
    {
        public string Id => "event.slow";

        public string Title => "Slow event handler";

        public string Description => "An event handler blocked the renderer for long.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            foreach (var evt in context.EventsInWindow(BevToolsEventKind.UiEvent, TimeSpan.FromSeconds(60)))
            {
                if (evt.DurationMs is { } d && d >= options.SlowEventHandlerMs)
                {
                    yield return new BevDiagnostic(
                        Id,
                        BevDiagnosticSeverity.Warning,
                        $"{evt.Title} took {Ms(d)}",
                        "The duration spans the handler and every await inside it; while it runs no other work for this session is processed.",
                        "Move CPU work off the renderer with Task.Run (Server) or split awaited steps so intermediate renders can flush.",
                        Fingerprint: $"{Id}:{evt.Id}",
                        RelatedEventIds: [evt.Id]);
                }
            }
        }
    }

    private sealed class DuplicateHttpRule : IBevDiagnosticRule
    {
        public string Id => "http.duplicate";

        public string Title => "Duplicate HTTP requests";

        public string Description => "The same request was issued repeatedly within a short window.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            var window = TimeSpan.FromMilliseconds(options.DuplicateHttpWindowMs);
            var requests = context.EventsInWindow(BevToolsEventKind.Http, TimeSpan.FromSeconds(30)).ToList();
            foreach (var group in requests.GroupBy(e => e.Title))
            {
                var ordered = group.OrderBy(e => e.StartTicks).ToList();
                for (var i = 0; i + options.DuplicateHttpCount - 1 < ordered.Count; i++)
                {
                    var last = ordered[i + options.DuplicateHttpCount - 1];
                    if ((last.StartTicks - ordered[i].StartTicks) / (double)System.Diagnostics.Stopwatch.Frequency <= window.TotalSeconds)
                    {
                        var triggers = ordered.Skip(i).Take(options.DuplicateHttpCount).Select(e => context.Events.FirstOrDefault(x => x.Id == e.ParentEventId)?.Title).Where(t => t is not null).Distinct().ToList();
                        yield return new BevDiagnostic(
                            Id,
                            BevDiagnosticSeverity.Warning,
                            $"{group.Key} issued {options.DuplicateHttpCount}+ times within {options.DuplicateHttpWindowMs} ms",
                            $"{ordered.Count} requests in the last 30 s." + (triggers.Count > 0 ? " Triggered by: " + string.Join(", ", triggers.Take(3)) + "." : " Not attributable to a UI event (lifecycle or timer)."),
                            "Cache the response, dedupe in-flight requests, or move the call from OnParametersSet to OnInitialized when parameters do not affect it.",
                            Fingerprint: $"{Id}:{group.Key}",
                            RelatedEventIds: ordered.Skip(i).Take(options.DuplicateHttpCount).Select(e => e.Id).ToList());
                        break;
                    }
                }
            }
        }
    }

    private sealed class SlowHttpRule : IBevDiagnosticRule
    {
        public string Id => "http.slow";

        public string Title => "Slow HTTP request";

        public string Description => "An HTTP request exceeded the slow threshold.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            foreach (var evt in context.EventsInWindow(BevToolsEventKind.Http, TimeSpan.FromSeconds(60)))
            {
                if (evt.DurationMs is { } d && d >= options.SlowHttpMs)
                {
                    yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Info, $"{evt.Title} took {Ms(d)}", evt.Detail ?? "", "Show a loading state, cancel superseded requests, or paginate the endpoint.", Fingerprint: $"{Id}:{evt.Id}", RelatedEventIds: [evt.Id]);
                }
            }
        }
    }

    private sealed class RepeatedHttpFailureRule : IBevDiagnosticRule
    {
        public string Id => "http.failures";

        public string Title => "Repeated HTTP failures";

        public string Description => "The same request keeps failing.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            foreach (var group in context.EventsInWindow(BevToolsEventKind.Http, TimeSpan.FromMinutes(2)).Where(e => e.Severity == BevToolsSeverity.Error).GroupBy(e => e.Title))
            {
                var count = group.Count();
                if (count >= 3)
                {
                    yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Error, $"{group.Key} failed {count} times", string.Join("; ", group.Select(e => e.Detail).Distinct().Take(3)), "Check the endpoint and add retry/backoff only for transient failures.", Fingerprint: $"{Id}:{group.Key}", RelatedEventIds: group.Take(10).Select(e => e.Id).ToList());
                }
            }
        }
    }

    private sealed class SlowJsInteropRule : IBevDiagnosticRule
    {
        public string Id => "jsinterop.slow";

        public string Title => "Slow JS interop";

        public string Description => "A JS interop call exceeded the slow threshold.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            foreach (var evt in context.EventsInWindow(BevToolsEventKind.JsInterop, TimeSpan.FromSeconds(60)))
            {
                if (evt.DurationMs is { } d && d >= options.SlowJsInteropMs)
                {
                    yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Warning, $"{evt.Title} took {Ms(d)}", (evt.ComponentName is null ? "" : "From " + evt.ComponentName + ". ") + "On Server every interop call is a SignalR round trip.", "Batch several calls into one JS function, or move the work to JS entirely.", Fingerprint: $"{Id}:{evt.Id}", ComponentInstanceId: evt.ComponentInstanceId, RelatedEventIds: [evt.Id]);
                }
            }
        }
    }

    private sealed class ExcessiveJsInteropRule : IBevDiagnosticRule
    {
        public string Id => "jsinterop.excessive";

        public string Title => "Excessive JS interop";

        public string Description => "The same JS function was called very frequently.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            foreach (var group in context.EventsInWindow(BevToolsEventKind.JsInterop, TimeSpan.FromSeconds(1)).GroupBy(e => e.Title))
            {
                var count = group.Count();
                if (count >= options.ExcessiveJsInteropCount)
                {
                    var components = group.Select(e => e.ComponentName).Where(n => n is not null).Distinct().ToList();
                    yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Warning, $"{group.Key} called {count} times in 1 s", components.Count > 0 ? "From " + string.Join(", ", components) + "." : "Origin not attributable to a component.", "Throttle, batch arguments into one call, or keep the loop in JavaScript.", Fingerprint: $"{Id}:{group.Key}", RelatedEventIds: group.Take(10).Select(e => e.Id).ToList());
                }
            }
        }
    }

    private sealed class RepeatedErrorRule : IBevDiagnosticRule
    {
        public string Id => "error.repeated";

        public string Title => "Repeated error";

        public string Description => "The same error occurred several times.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            foreach (var group in context.EventsInWindow(BevToolsEventKind.Error, TimeSpan.FromMinutes(5)).Where(e => !e.Category.StartsWith("log:", StringComparison.Ordinal)).GroupBy(e => e.Title.Replace("(repeat) ", "")))
            {
                var count = group.Count();
                if (count >= options.RepeatedErrorCount)
                {
                    yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Error, $"{group.Key} occurred {count} times", group.First().Detail ?? "", "Open the Errors panel to see the preceding events for each occurrence.", Fingerprint: $"{Id}:{group.Key}", ComponentInstanceId: group.First().ComponentInstanceId, RelatedEventIds: group.Take(10).Select(e => e.Id).ToList());
                }
            }
        }
    }
}
