
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
        new HttpBurstRule(),
        new HttpPerItemRule(),
        new SlowHttpRule(),
        new RepeatedHttpFailureRule(),
        new SlowJsInteropRule(),
        new ExcessiveJsInteropRule(),
        new RepeatedErrorRule(),
        new PrerenderRefetchRule(),
        new LargeRenderBatchRule(),
        new RetainedComponentsRule(),
        new FormValidationRule(),
        new NavigationLoopRule(),
        new PageAccessibilityRule(),
        new LogFloodRule(),
        new HeavyRenderAllocationRule(),
        new SlowInteractionRule(),
        new LongFrameRule(),
        new ServiceWorkerWaitingRule(),
        new MemoryGrowthRule(),
        new DbPerItemRule(),
        new SlowDbCommandRule(),
    ];

    /// <summary>
    /// One SQL command executed over and over within two seconds - typically each row of a list loading its own data
    /// (N+1). Read from Entity Framework Core's command logs, by the SQL as EF wrote it (parameters, not values).
    /// </summary>
    private sealed class DbPerItemRule : IBevDiagnosticRule
    {
        public string Id => "db.per-item";

        public string Title => "One query per item";

        public string Description => "The same SQL command executed many times within two seconds: the N+1 query pattern.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { Logs.Count: > 0 } session })
            {
                yield break;
            }

            var threshold = Opt(context).DbPerItemCount;
            var since = context.Now - TimeSpan.FromMinutes(2);
            foreach (var group in session.Logs.Snapshot().Where(l => l.DbCommand is { CommandText: not null } && l.At >= since).GroupBy(l => BevDbCommands.Normalize(l.DbCommand!.CommandText!), StringComparer.Ordinal))
            {
                var executions = group.ToArray();
                if (executions.Length < threshold || DensestRun(executions, TimeSpan.FromSeconds(2)) is not { } densest || densest.Run.Length < threshold)
                {
                    continue;
                }

                var run = densest.Run;
                var senders = run.Where(l => l.CallerComponent is not null).GroupBy(l => l.CallerComponent!).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key + " ×" + g.Count()).ToList();
                var trigger = run.Select(l => l.TriggerEventId).FirstOrDefault(t => t is not null) is { } triggerId ? session.Timeline.Find(triggerId)?.Title : null;
                var sql = group.Key.Length <= 120 ? group.Key : group.Key[..119] + "…";
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"\"{sql}\" ran {run.Length} times in {Ms(densest.Span.TotalMilliseconds)}",
                    $"{run.Length} executions of one command, {Ms(run.Sum(l => l.DbCommand!.ElapsedMs ?? 0))} in the database"
                        + (senders.Count > 0 ? ", sent from " + string.Join(", ", senders) : "")
                        + (trigger is not null ? ", during " + trigger : "") + ".",
                    "Load what the list needs in one query (Include, a join, or WHERE Id IN (…)) rather than one per row, or load an item's data when it is opened.",
                    Fingerprint: $"{Id}:{group.Key}",
                    RelatedEventIds: run.Where(l => l.TimelineEventId > 0).TakeLast(10).Select(l => l.TimelineEventId).ToList());
            }
        }
    }

    /// <summary>A database command slower than the threshold, as Entity Framework Core measured it.</summary>
    private sealed class SlowDbCommandRule : IBevDiagnosticRule
    {
        public string Id => "db.slow";

        public string Title => "Slow database command";

        public string Description => "An Entity Framework Core command took longer than the slow threshold.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { Logs.Count: > 0 } session })
            {
                yield break;
            }

            var threshold = Opt(context).SlowDbCommandMs;
            foreach (var log in session.Logs.Snapshot().Where(l => l.At >= context.Now - TimeSpan.FromMinutes(1) && l.DbCommand is { ElapsedMs: { } ms } && ms >= threshold))
            {
                var sql = log.DbCommand!.CommandText is { } text ? BevDbCommands.Normalize(text) : "(SQL not read: value access None)";
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Info,
                    $"A database command took {Ms(log.DbCommand.ElapsedMs!.Value)}",
                    (sql.Length <= 200 ? sql : sql[..199] + "…") + (log.Caller is not null ? " — sent from " + log.Caller : ""),
                    "Look at its query plan: a missing index, a large result loaded whole, or a lock it waited on.",
                    Fingerprint: $"{Id}:{log.Id}",
                    RelatedEventIds: log.TimelineEventId > 0 ? [log.TimelineEventId] : null);
            }
        }
    }

    /// <summary>
    /// The heap after full collections climbing collection after collection: what survives each one keeps growing, the
    /// outside view of a leak. Read from what the memory trend saw (Runtime → Memory over time).
    /// </summary>
    private sealed class MemoryGrowthRule : IBevDiagnosticRule
    {
        public string Id => "memory.growth";

        public string Title => "Memory keeps growing";

        public string Description => "The managed heap after full garbage collections grew steadily over the last ten minutes.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { } session } || session.Memory.Growth(context.Now) is not { } growth)
            {
                yield break;
            }

            var minutes = Math.Max(1, (growth.Last.At - growth.First.At).TotalMinutes);
            yield return new BevDiagnostic(
                Id,
                BevDiagnosticSeverity.Warning,
                $"The heap after full collections grew from {BevFmt.Bytes(growth.First.HeapAfterBytes)} to {BevFmt.Bytes(growth.Last.HeapAfterBytes)} over {growth.Count} collections",
                $"About {BevFmt.Bytes((long)((growth.Last.HeapAfterBytes - growth.First.HeapAfterBytes) / minutes))} a minute over {minutes:0.#} min; never back near where it started."
                    + (session.IsInMultiUserHost ? " The heap is the process's: every circuit adds to it." : ""),
                "Something keeps objects alive: Runtime → Disposed components still alive finds components a service or a timer holds; for anything else take two heap snapshots (dotnet-gcdump on a server, the browser's memory tool or the runtime's GC dump on WebAssembly) and compare them.",
                Fingerprint: Id);
        }
    }

    /// <summary>
    /// A service worker installed and waiting: the page runs the previous version until every tab of the app closes, or
    /// the new worker calls skipWaiting. The classic "I deployed and users still see the old app". From the last storage
    /// read (Browser → Storage).
    /// </summary>
    private sealed class ServiceWorkerWaitingRule : IBevDiagnosticRule
    {
        public string Id => "pwa.update-waiting";

        public string Title => "Service worker update waiting";

        public string Description => "A new service worker is installed but not active, so the page still runs the previous version of the app.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session.Browser.AppStorage.ServiceWorkers: { } workers })
            {
                yield break;
            }

            foreach (var worker in workers.Where(w => w.Waiting is not null))
            {
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Info,
                    "An update is waiting for " + worker.Scope,
                    "Active: " + (worker.Active ?? "none") + "; waiting: " + worker.Waiting + ". The page keeps running the active version until every tab of it closes.",
                    "Prompt the user to reload and have the waiting worker call skipWaiting (Bit.Bswup does this through its update handler), or reload all tabs to see the new version.",
                    Fingerprint: Id + ":" + worker.Scope + ":" + worker.Waiting);
            }
        }
    }

    /// <summary>
    /// Interactions whose next paint came late, one finding per kind of interaction and element, from what the browser
    /// measured (Event Timing). Says which part was slow: input delay (the main thread busy with something else),
    /// processing (the handlers) or presentation (rendering the frame), and on WebAssembly the UI event that ran.
    /// </summary>
    private sealed class SlowInteractionRule : IBevDiagnosticRule
    {
        public string Id => "browser.slow-interaction";

        public string Title => "Slow interaction";

        public string Description => "A click, tap or key press whose next paint came later than the slow threshold (Interaction to Next Paint).";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { } session })
            {
                yield break;
            }

            var threshold = Opt(context).SlowInteractionMs;
            var since = context.Now - TimeSpan.FromMinutes(2);
            foreach (var group in session.Browser.Responsiveness.Interactions.Where(i => i.At >= since && i.DurationMs >= threshold).GroupBy(i => (i.Name, i.Target)))
            {
                var slowest = group.MaxBy(i => i.DurationMs)!;
                var part = new[] { ("input delay", slowest.InputDelayMs), ("processing", slowest.ProcessingMs), ("presentation", slowest.PresentationMs) }.MaxBy(p => p.Item2);
                var count = group.Count();
                yield return new BevDiagnostic(
                    Id,
                    slowest.DurationMs >= 500 ? BevDiagnosticSeverity.Error : BevDiagnosticSeverity.Warning,
                    $"{group.Key.Name}{(group.Key.Target is null ? "" : " on " + group.Key.Target)}: next paint after {Ms(slowest.DurationMs)}" + (count > 1 ? $" ({count} times over {Ms(threshold)})" : ""),
                    BevResponsiveness.Breakdown(slowest) + ". Mostly " + part.Item1 + ".",
                    part.Item1 switch
                    {
                        "input delay" => "The main thread was busy when the input arrived: look for long frames before it (Browser → Responsiveness), a timer or a render storm.",
                        "processing" => "Its handlers took the time: on WebAssembly that is the .NET event handler and the renders it caused (Profiler); await long work, show progress first, or move it off the UI thread.",
                        _ => "Rendering the frame after it took the time: a large DOM update (a list without Virtualize, a render cascade) or expensive styles.",
                    },
                    Fingerprint: $"{Id}:{group.Key.Name}:{group.Key.Target}",
                    RelatedEventIds: group.Where(i => i.TimelineEventId > 0).Select(i => i.TimelineEventId).TakeLast(10).ToList());
            }
        }
    }

    /// <summary>
    /// Frames during which the browser's main thread was blocked: input waited, nothing painted. On WebAssembly it is the
    /// thread .NET renders on, so the finding names the UI events and render batches that ran inside the frame.
    /// </summary>
    private sealed class LongFrameRule : IBevDiagnosticRule
    {
        public string Id => "browser.long-frame";

        public string Title => "Blocked main thread";

        public string Description => "Frames the browser could not paint because the main thread was busy past the blocking threshold.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { } session })
            {
                yield break;
            }

            var threshold = Opt(context).LongFrameBlockingMs;
            // A frame BevTools' own panel mostly spent rendering is not among them (BevResponsiveness.Record).
            var frames = session.Browser.Responsiveness.Frames.Where(f => f.At >= context.Now - TimeSpan.FromMinutes(1) && f.BlockingMs >= threshold).ToArray();
            if (frames.Length == 0)
            {
                yield break;
            }

            var longest = frames.MaxBy(f => f.BlockingMs)!;
            yield return new BevDiagnostic(
                Id,
                longest.BlockingMs >= 500 ? BevDiagnosticSeverity.Error : BevDiagnosticSeverity.Warning,
                $"The main thread was blocked for {Ms(frames.Sum(f => f.BlockingMs))} in {frames.Length} frame{(frames.Length == 1 ? "" : "s")} over the last minute",
                "Longest: " + Ms(longest.DurationMs) + ", " + Ms(longest.BlockingMs) + " blocking; " + BevResponsiveness.Describe(longest) + ".",
                session.Platform == "WebAssembly"
                    ? "On WebAssembly .NET runs on this thread: split long handlers with await Task.Yield(), render less per batch (Virtualize, ShouldRender), or move CPU-bound work to a worker."
                    : "Script on the page kept the thread busy: the scripts named are where the time went; a large render batch applied at once shows up here too.",
                // One finding, renewed as frames arrive: the evidence is the last minute's.
                Fingerprint: Id,
                RelatedEventIds: frames.Where(f => f.TimelineEventId > 0).Select(f => f.TimelineEventId).TakeLast(10).ToList());
        }
    }

    /// <summary>
    /// A component whose render fragment allocates a lot on each render: what each render costs the garbage collector,
    /// which on WebAssembly pauses the only thread the UI has. Read from the retained samples of the last thirty
    /// seconds, so a component that stopped rendering stops being reported.
    /// </summary>
    private sealed class HeavyRenderAllocationRule : IBevDiagnosticRule
    {
        private const int MinRenders = 3;

        public string Id => "render.allocations";

        public string Title => "Render allocates heavily";

        public string Description => "A component's BuildRenderTree allocates many bytes per render: strings, closures, collections it builds every time.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (!BevAllocations.IsMeasurable || context is not DiagnosticContext { Session: { } session })
            {
                yield break;
            }

            var threshold = Opt(context).HeavyRenderAllocationBytes;
            var since = context.Now - TimeSpan.FromSeconds(30);
            foreach (var group in session.Components.All
                .Where(c => !c.IsBevTools && !c.IsHidden && c.MaxAllocatedBytes >= threshold && c.LastRenderAt >= since)
                .Select(c => (Record: c, Samples: c.Renders.Where(s => s.At >= since && s.AllocatedBytes is not null).ToArray()))
                .Where(c => c.Samples.Length >= MinRenders)
                .GroupBy(c => c.Record.TypeName))
            {
                var samples = group.SelectMany(c => c.Samples).ToArray();
                var average = samples.Average(s => (double)s.AllocatedBytes!.Value);
                if (average < threshold)
                {
                    continue;
                }

                var first = group.First().Record;
                var instances = group.Count();
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"{first.DisplayName} allocates {BevToolsHttpMessageHandler.FormatBytes((long)average)} per render",
                    $"{samples.Length} renders in the last 30 s" + (instances > 1 ? $" of {instances} instances" : "") + $", up to {BevToolsHttpMessageHandler.FormatBytes(samples.Max(s => s.AllocatedBytes!.Value))} in one; its children are not counted.",
                    "Build what the markup shows once - in OnParametersSet, or when the data changes - rather than in every render: LINQ chains, ToList, string formatting and per-item closures in BuildRenderTree are allocated again each time.",
                    Fingerprint: $"{Id}:{group.Key}",
                    ComponentInstanceId: first.InstanceId,
                    RelatedEventIds: samples.Where(s => s.EventId > 0).TakeLast(5).Select(s => s.EventId).ToList());
            }
        }
    }

    /// <summary>
    /// One message template written over and over in a short window: logging on a hot path (a render, a parameter
    /// update, a list item), or a retry loop. Every entry costs its providers (the browser console is slow on WebAssembly)
    /// and buries the entries that matter. Read from the log entries the session holds, by their own times, so a burst
    /// that ended before the panel opened is still found.
    /// </summary>
    private sealed class LogFloodRule : IBevDiagnosticRule
    {
        public string Id => "log.flood";

        public string Title => "Log flood";

        public string Description => "The same log message, whatever values it carried, written many times within ten seconds.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { Logs.Count: > 0 } session })
            {
                yield break;
            }

            var threshold = Opt(context).LogFloodCount;
            var logs = session.Logs.Snapshot();
            if (logs.Length < threshold)
            {
                yield break;
            }

            var window = TimeSpan.FromSeconds(10);
            var since = context.Now - TimeSpan.FromMinutes(5);
            // An entry whose message and template value access None hides cannot be told from any other.
            foreach (var group in logs.Where(l => l.At >= since && (l.MessageTemplate ?? l.Message) is not null).GroupBy(l => (l.Category, Template: (l.MessageTemplate ?? l.Message)!)))
            {
                var entries = group.ToArray();
                if (entries.Length < threshold || DensestRun(entries, window) is not { } densest || densest.Run.Length < threshold)
                {
                    continue;
                }

                var burst = densest.Run;
                var best = burst.Length;
                var senders = burst.Where(l => l.CallerComponent is not null).GroupBy(l => l.CallerComponent!).OrderByDescending(g => g.Count()).Take(3)
                    .Select(g => g.Key + " ×" + g.Count()).ToList();
                var level = burst[^1].Level;
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"{group.Key.Category}: \"{Shorten(group.Key.Template)}\" logged {best} times in {Ms((burst[^1].At - burst[0].At).TotalMilliseconds)}",
                    $"{best} {level} entries with this template within ten seconds" + (senders.Count > 0 ? ", sent from " + string.Join(", ", senders) : "") + ".",
                    "Log once per operation rather than per render or item, or lower the entry's level below what the host records: every entry costs each logging provider, and the browser console is slow on WebAssembly.",
                    Fingerprint: $"{Id}:{group.Key.Category}:{group.Key.Template}",
                    RelatedEventIds: burst.Where(l => l.TimelineEventId > 0).TakeLast(10).Select(l => l.TimelineEventId).ToList());
            }
        }

        private static string Shorten(string text) => text.Length <= 80 ? text : text[..79] + "…";
    }

    /// <summary>The page's last accessibility check (Browser panel), one finding per check that failed, with the components.</summary>
    private sealed class PageAccessibilityRule : IBevDiagnosticRule
    {
        public string Id => "a11y.page";

        public string Title => "Accessibility of the page";

        public string Description => "Markup a screen reader or a keyboard user cannot use, from the last check of the page (Browser panel).";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session.Browser.Accessibility: { Count: > 0 } findings })
            {
                yield break;
            }

            foreach (var group in findings.GroupBy(f => f.Rule))
            {
                var first = group.First();
                var components = group.Select(f => f.Component).Where(c => c is not null).GroupBy(c => c).Select(c => c.Count() > 1 ? c.Key + " ×" + c.Count() : c.Key).Take(6).ToList();
                yield return new BevDiagnostic(
                    Id,
                    first.Rule == "page-lang" ? BevDiagnosticSeverity.Info : BevDiagnosticSeverity.Warning,
                    group.Count() + " × " + first.Rule + " (WCAG " + first.Wcag + ")",
                    first.Message + " Elements: " + string.Join(", ", group.Select(f => f.Element).Distinct().Take(5)) + (components.Count > 0 ? ". In: " + string.Join(", ", components) + "." : "."),
                    "Browser → Accessibility shows each element in the page and selects its component.",
                    Fingerprint: Id + ":" + first.Rule);
            }
        }
    }

    /// <summary>
    /// Navigations that feed each other: two pages redirecting to one another (an authorization redirect whose target
    /// redirects back is the usual one) or a burst of navigations no user made. Read from the navigation events of the
    /// last five minutes, judged by their own times - a loop that ran while the panel was closed is still one when it
    /// opens - and only from navigations code made: a person clicking between a list and its details, or a search box
    /// that writes the query string as they type, is not a loop.
    /// </summary>
    private sealed class NavigationLoopRule : IBevDiagnosticRule
    {
        private const int LoopNavigations = 6;
        private const int StormNavigations = 10;

        public string Id => "navigation.loop";

        public string Title => "Navigation loop";

        public string Description => "Navigations bouncing between two destinations, or too many of them in a moment.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var session = (context as DiagnosticContext)?.Session;
            // A redirect made while the previous navigation ran has that navigation as its parent: only a UI event parent
            // (a click that navigated) makes it a person's.
            string Cause(BevToolsEvent n) => n.ParentEventId is { } parent && (session is null || session.Timeline.Find(parent)?.Kind == BevToolsEventKind.UiEvent)
                ? "from a UI event"
                : session?.NavigationCause(n.Id) ?? n.Detail ?? "programmatic";
            bool MadeByCode(BevToolsEvent n) => Cause(n) == "programmatic";

            var navigations = context.EventsInWindow(BevToolsEventKind.Navigation, TimeSpan.FromMinutes(5)).Reverse().Where(MadeByCode).ToList();
            if (navigations.Count < LoopNavigations)
            {
                yield break;
            }

            // A ping-pong: each navigation goes where the one before the previous went, and not where the previous did.
            for (var end = navigations.Count - 1; end >= LoopNavigations - 1; end--)
            {
                var start = end;
                while (start >= 2 && navigations[start].Title == navigations[start - 2].Title && navigations[start].Title != navigations[start - 1].Title)
                {
                    start--;
                }

                start = Math.Max(0, start - 1);
                var seconds = System.Diagnostics.Stopwatch.GetElapsedTime(navigations[start].StartTicks, navigations[end].StartTicks).TotalSeconds;
                if (end - start + 1 >= LoopNavigations && seconds <= 10)
                {
                    var a = navigations[end - 1].Title;
                    var b = navigations[end].Title;
                    yield return new BevDiagnostic(
                        Id,
                        BevDiagnosticSeverity.Error,
                        "Navigation loop: " + a + " ↔ " + b,
                        (end - start + 1) + " navigations in " + seconds.ToString("0.#") + " s alternating between the two, every one made by code (NavigateTo), none by a click.",
                        "Check the condition each page redirects on: a redirect whose target redirects back (a login page that itself requires authentication, a check the redirect does not satisfy) loops for as long as the user stays.",
                        Fingerprint: Id + ":" + string.Join("|", new[] { a, b }.Order(StringComparer.Ordinal)),
                        RelatedEventIds: navigations.Skip(start).Take(end - start + 1).Select(n => n.Id).Take(20).ToList());
                    yield break;
                }
            }

            for (var i = 0; i + StormNavigations - 1 < navigations.Count; i++)
            {
                var last = navigations[i + StormNavigations - 1];
                if (System.Diagnostics.Stopwatch.GetElapsedTime(navigations[i].StartTicks, last.StartTicks).TotalSeconds <= 2)
                {
                    yield return new BevDiagnostic(
                        Id,
                        BevDiagnosticSeverity.Warning,
                        StormNavigations + "+ navigations within 2 s",
                        "Destinations: " + string.Join(", ", navigations.Skip(i).Take(StormNavigations).Select(n => n.Title).Distinct().Take(5)) + ".",
                        "A navigation made from a lifecycle method or a timer, or a redirect chain: find what calls NavigateTo (the Timeline shows what each navigation was caused by).",
                        Fingerprint: Id + ":storm",
                        RelatedEventIds: navigations.Skip(i).Take(StormNavigations).Select(n => n.Id).ToList());
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Two form mistakes that fail silently. Validation attributes on a model do nothing unless a validator component
    /// (<c>DataAnnotationsValidator</c>) listens to the form, so <c>Validate()</c> and <c>OnValidSubmit</c> always pass; and a
    /// form that fails validation with nothing to display the messages leaves the user with a button that does nothing.
    /// </summary>
    private sealed class FormValidationRule : IBevDiagnosticRule
    {
        public string Id => "forms.validation";

        public string Title => "Form validation that does not work";

        public string Description => "Validation attributes nothing checks, and validation messages nothing shows.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { } session })
            {
                yield break;
            }

            session.Forms.Discover();
            foreach (var form in session.Forms.Forms())
            {
                // A form whose submit the code handles may validate by hand, and one holding messages is validated by
                // something: neither is a form nothing checks.
                if (form.ValidationAttributes.Count > 0 && form.Validators.Count == 0 && !form.HandlesSubmit && form.MessageCount == 0)
                {
                    yield return new BevDiagnostic(
                        Id,
                        BevDiagnosticSeverity.Warning,
                        (form.OwnerName ?? "A form") + ": " + form.ModelType + "'s validation attributes are never checked",
                        string.Join("; ", form.ValidationAttributes.Take(4)) + (form.ValidationAttributes.Count > 4 ? "; …" : "") + ". No component listens to the form's validation requests (EditContext.OnValidationRequested has no handler), so Validate() - and OnValidSubmit - always succeed.",
                        "Add <DataAnnotationsValidator /> inside the EditForm (or the validator component you use).",
                        Fingerprint: Id + ":ignored:" + form.OwnerName + ":" + form.ModelType,
                        ComponentInstanceId: form.OwnerInstanceId);
                }

                if (form.LastValidationPassed == false && form.MessageCount > 0 && form.MessageDisplays.Count == 0)
                {
                    yield return new BevDiagnostic(
                        Id,
                        BevDiagnosticSeverity.Info,
                        (form.OwnerName ?? "A form") + ": validation fails and no message is displayed",
                        form.MessageCount + " validation message" + (form.MessageCount == 1 ? "" : "s") + " after the last validation, and no ValidationSummary or ValidationMessage listens to the form; inputs only get the invalid CSS class.",
                        "Add <ValidationSummary /> or a <ValidationMessage For=\"...\" /> per field (or display EditContext.GetValidationMessages() yourself).",
                        Fingerprint: Id + ":hidden:" + form.OwnerName + ":" + form.ModelType,
                        ComponentInstanceId: form.OwnerInstanceId);
                }
            }
        }
    }

    /// <summary>
    /// Disposed components that survive full garbage collections: the renderer let go, something else did not. The most
    /// common Blazor leak - a handler left on a long-lived service - keeps the whole component, and what it references,
    /// for as long as that service lives: the circuit, or the app.
    /// </summary>
    private sealed class RetainedComponentsRule : IBevDiagnosticRule
    {
        public string Id => "memory.retained-components";

        public string Title => "Disposed components still alive";

        public string Description => "Components the renderer disposed that full garbage collections did not reclaim.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { } session })
            {
                yield break;
            }

            foreach (var group in session.Leaks.Retained())
            {
                var causes = group.Causes;
                var evidence = (group.Count == 1 ? "Survived " : "Each survived ") + group.FullCollections + "+ full garbage collections since it was disposed (the oldest " + BevFmt.Ago(group.OldestDisposal) + "). "
                    + (causes.Count > 0
                        ? "Held by: " + string.Join("; ", causes.Take(3).Select(c => c.Holder + ": " + c.Detail)) + "."
                        : "No reference to it was found in its injected services, its cascading values, the JS runtime's object references or its own timers: something else holds it (a static field, a cache, an object it handed itself to).");
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    group.DisplayName + ": " + group.Count + " disposed instance" + (group.Count == 1 ? "" : "s") + " still alive",
                    evidence,
                    causes.Count > 0 ? causes[0].Suggestion : "Take a heap snapshot (dotnet-gcdump for a server process, the browser's memory tool for WebAssembly) and follow the path that retains it.",
                    Fingerprint: Id + ":" + group.TypeName);
            }
        }
    }

    /// <summary>
    /// On Blazor Server every render batch crosses the network, and its size is what the user waits for: a page that
    /// re-renders a long list sends all of it. The browser bridge sizes each batch as it arrives (the bin argument of
    /// <c>JS.RenderBatch</c>) and times how long the browser takes to apply it.
    /// </summary>
    private sealed class LargeRenderBatchRule : IBevDiagnosticRule
    {
        public string Id => "circuit.large-render-batch";

        public string Title => "Large render batches over the circuit";

        public string Description => "A render batch the circuit sent to the browser was large enough to be felt as latency.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: var session })
            {
                yield break;
            }

            foreach (var socket in session.Browser.Sockets)
            {
                var batches = socket.RenderBatches;
                if (socket.Kind != "blazor-circuit" || batches.MaxBytes < BevTrafficView.LargeBatch)
                {
                    continue;
                }

                var applied = batches.Count - batches.Pending;
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"A render batch of {BevFmt.Bytes(batches.MaxBytes)} was sent over the circuit",
                    $"{batches.Count} render batches, {BevFmt.Bytes(batches.Bytes)} in all, {BevFmt.Bytes(batches.Count == 0 ? 0 : batches.Bytes / batches.Count)} on average"
                        + (applied > 0 ? $"; the browser took up to {Ms(batches.MaxApplyMs)} to apply one." : ".")
                        + " Every byte of a batch crosses the network before the user sees the update, and the circuit sends nothing else meanwhile.",
                    "Render less at once: Virtualize long lists, page large tables, give list items a @key so a change re-renders one row instead of all of them, and keep components that re-render often small (see the Profiler for which ones).",
                    Fingerprint: Id + ":" + socket.Id);
            }
        }
    }

    /// <summary>
    /// The classic cost of prerendering: <c>OnInitializedAsync</c> runs on the server to produce the HTML and again when
    /// the page becomes interactive, and each run loads the same data. Evidence comes from both sides - the prerendering
    /// session's requests, handed over in the page (<see cref="BevPrerenderSummary"/>), and this session's - so the rule
    /// says nothing when the page was not prerendered or the summary did not arrive.
    /// </summary>
    private sealed class PrerenderRefetchRule : IBevDiagnosticRule
    {
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);

        public string Id => "webapp.prerender-refetch";

        public string Title => "Data fetched twice: while prerendering and when interactive";

        public string Description => "A request made while the page was prerendered on the server was made again once the page became interactive.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { Prerender: { Requests.Count: > 0 } prerender } session })
            {
                yield break;
            }

            var interactive = session.Http.Snapshot().Where(r => r.StartedAt - session.StartedAt < Window).ToArray();
            foreach (var group in prerender.Requests.GroupBy(r => BevPrerenderSummary.Signature(r.Method, r.Url)))
            {
                var again = interactive.Where(r => BevPrerenderSummary.Signature(r.Method, r.Url) == group.Key).ToList();
                if (again.Count == 0)
                {
                    continue;
                }

                var prerendered = group.ToList();
                var after = (again[0].StartedAt - session.StartedAt).TotalSeconds;
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"{group.Key} ran while prerendering and again when the page became interactive",
                    $"Prerendering requested it {Times(prerendered.Count)} on the server ({Ms(prerendered.Sum(r => r.DurationMs ?? 0))}); this {session.Platform ?? "interactive"} session requested it {Times(again.Count)} more, the first {after:0.0} s after it started. "
                        + "OnInitializedAsync runs once to prerender the HTML and once more when the component becomes interactive, and nothing carried the first result over.",
                    "Hand the data over: [PersistentState] on the property, or PersistentComponentState.PersistAsJson and TryTakeFromJson. Or turn prerendering off for the component: new InteractiveServerRenderMode(prerender: false).",
                    Fingerprint: Id + ":" + group.Key,
                    RelatedEventIds: again.Select(r => r.TimelineEventId).Where(id => id > 0).Take(10).ToList());
            }
        }

        private static string Times(int count) => count == 1 ? "once" : count + " times";
    }

    private static BevDiagnosticsOptions Opt(IBevDiagnosticContext ctx) => ctx.GetOptions<BevDiagnosticsOptions>() ?? new BevDiagnosticsOptions();

    private static string Ms(double value) => value.ToString("0.#") + " ms";

    /// <summary>How far back the rate rules look for a burst: the rules run when the panel is shown, not as it happens.</summary>
    private static readonly TimeSpan BurstLookback = TimeSpan.FromSeconds(30);

    /// <summary>The events of the busiest stretch of <paramref name="window"/> among <paramref name="events"/>, oldest first.</summary>
    private static List<BevToolsEvent> DensestWindow(IEnumerable<BevToolsEvent> events, TimeSpan window)
    {
        var ordered = events.OrderBy(e => e.StartTicks).ToList();
        var span = (long)(window.TotalSeconds * System.Diagnostics.Stopwatch.Frequency);
        var (bestStart, bestCount, start) = (0, 0, 0);
        for (var end = 0; end < ordered.Count; end++)
        {
            while (ordered[end].StartTicks - ordered[start].StartTicks > span)
            {
                start++;
            }

            if (end - start + 1 > bestCount)
            {
                (bestStart, bestCount) = (start, end - start + 1);
            }
        }

        return ordered.GetRange(bestStart, bestCount);
    }

    /// <summary>
    /// The most recent of the busiest runs of log entries within <paramref name="window"/>, and how long it lasted:
    /// <paramref name="entries"/> are in the order they were logged.
    /// </summary>
    private static (BevLogRecord[] Run, TimeSpan Span)? DensestRun(IReadOnlyList<BevLogRecord> entries, TimeSpan window)
    {
        var (best, start) = (0, 0);
        for (int first = 0, last = 0; last < entries.Count; last++)
        {
            while (entries[last].At - entries[first].At > window)
            {
                first++;
            }

            if (last - first + 1 >= best)
            {
                (best, start) = (last - first + 1, first);
            }
        }

        if (best == 0)
        {
            return null;
        }

        var run = entries.Skip(start).Take(best).ToArray();
        return (run, run[^1].At - run[0].At);
    }

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
            var driven = new List<(long InstanceId, string Name, string Ancestor, List<BevToolsEvent> Renders, int ParentDriven)>();
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

                var ancestor = all.Select(e => e.Detail).FirstOrDefault(d => d is not null)?.Replace(" re-rendered (parameters re-applied)", "") ?? "its parent";
                driven.Add((group.Key, all[0].ComponentName ?? "component", ancestor, all, parentDriven));
            }

            // The rows of a list share their type and their parent, and so the fix: one finding for the 120 of them,
            // not 120 findings that bury every other one.
            foreach (var same in driven.GroupBy(d => (d.Name, d.Ancestor)))
            {
                var instances = same.OrderByDescending(d => d.ParentDriven).ToList();
                var all = instances.SelectMany(d => d.Renders).ToList();
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
                var (name, ancestor) = same.Key;
                var most = instances[0].ParentDriven;
                var fewest = instances[^1].ParentDriven;
                var title = instances.Count == 1
                    ? $"{name} re-rendered {most} times, every one of them because {ancestor} re-rendered"
                    : $"{name} ×{instances.Count} instances re-rendered {(fewest == most ? "" : "up to ")}{most} times each, every one of them because {ancestor} re-rendered";
                yield return new BevDiagnostic(
                    Id,
                    severity,
                    title,
                    $"None of these renders came from an event, navigation or its own state; total build time {Ms(totalMs)} in the last 10 s"
                        + (instances.Count == 1 ? ". " : $" across the {instances.Count} instances. ")
                        + "The parent re-applies parameters on every one of its own renders, and Blazor cannot know the values are equivalent. "
                        + parameterEvidence,
                    "Override ShouldRender, pass values the parent does not recreate per render (no new lambdas, collections or anonymous objects), or move the fast-changing state out of the parent so it does not re-render at all.",
                    Fingerprint: $"{Id}:{name}:{ancestor}",
                    ComponentInstanceId: instances[0].InstanceId,
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
            // The rules run when the panel is shown, often seconds after the storm: the densest window of the last
            // half minute, not the one that ends now.
            var renders = context.EventsInWindow(BevToolsEventKind.Render, BurstLookback).Where(e => e.ComponentInstanceId is not null).ToList();
            foreach (var instance in renders.GroupBy(e => e.ComponentInstanceId!.Value))
            {
                var group = DensestWindow(instance, TimeSpan.FromMilliseconds(options.RenderStormWindowMs));
                var count = group.Count;
                if (count < options.RenderStormCount)
                {
                    continue;
                }

                var name = group[0].ComponentName ?? "component";
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
                    Fingerprint: $"{Id}:{instance.Key}",
                    ComponentInstanceId: instance.Key,
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
            var cascades = new List<(BevToolsEvent Batch, int Count, BevToolsEvent? Root, string? First)>();
            foreach (var batch in context.EventsInWindow(BevToolsEventKind.RenderBatch, TimeSpan.FromSeconds(30)))
            {
                if (batch.Data is null || !batch.Data.TryGetValue("count", out var countObj) || countObj is not int count || count < options.RenderCascadeSize)
                {
                    continue;
                }

                var root = context.Events.FirstOrDefault(e => e.Id == batch.ParentEventId);
                var first = context.Events.FirstOrDefault(e => e.Kind == BevToolsEventKind.Render && e.ParentEventId == batch.Id)?.ComponentName;
                cascades.Add((batch, count, root, first));
            }

            // Every keystroke into a list's filter is a batch of the same cascade, from the same event: one finding for
            // what they share, not one per keystroke.
            foreach (var same in cascades.GroupBy(c => c.Root?.Title ?? "started with " + c.First))
            {
                var batches = same.ToList();
                var largest = batches.Max(b => b.Count);
                var root = batches[^1].Root;
                var first = batches[^1].First;
                var slowest = batches.Max(b => b.Batch.DurationMs ?? 0);
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    batches.Count == 1 ? $"Render batch touched {largest} components" : $"{batches.Count} render batches touched up to {largest} components each",
                    (root is not null ? $"Triggered by {root.Title}{(batches.Count == 1 ? "" : " each time")}. " : "") + (first is not null ? $"Started with {first}. " : "")
                        + (batches.Count == 1 ? $"Building took {Ms(slowest)}." : $"Building took up to {Ms(slowest)} per batch."),
                    "A cascade usually means a high-level component (layout, cascading value provider) re-rendered. Narrow what changes: cascade immutable values, or give children ShouldRender/keyed parameters.",
                    Fingerprint: $"{Id}:{same.Key}",
                    RelatedEventIds: batches.TakeLast(20).Select(b => b.Batch.Id).ToList());
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

    /// <summary>
    /// A request per keystroke: one endpoint called again and again as the input changes - in its query values
    /// (<c>?q=s</c>, <c>?q=sh</c>) or, for a lookup by id, in its path when each request comes from its own UI event.
    /// Exact duplicates are <see cref="DuplicateHttpRule"/>'s; this is the other half of the pattern, and the one with a
    /// race in it - when an earlier request is still running as the next one starts, its response can arrive last and win.
    /// </summary>
    private sealed class HttpBurstRule : IBevDiagnosticRule
    {
        public string Id => "http.burst";

        public string Title => "Burst of requests to one endpoint";

        public string Description => "One endpoint was called repeatedly in a short window with different values, typically once per keystroke.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { } session })
            {
                yield break;
            }

            var options = Opt(context);
            var count = Math.Max(2, options.HttpBurstCount);
            foreach (var ordered in RecentByTemplate(session, context))
            {
                if (ordered.Select(r => r.Signature).Distinct(StringComparer.Ordinal).Take(2).Count() < 2)
                {
                    continue;
                }

                // Same path with other query values (a search box), or other ids each from its own event (a lookup
                // typed into a box); other ids from one event, or from none, are rows loading theirs (http.per-item).
                var burst = FirstWindow(ordered, TimeSpan.FromMilliseconds(options.HttpBurstWindowMs),
                    w => w.Size >= count && w.Signatures.Count >= 2 && (w.Endpoints.Count == 1 || EachFromItsOwnEvent(w)));
                if (burst is null)
                {
                    continue;
                }

                // Overlap: the previous request had not completed when the next one started.
                var overlapping = 0;
                for (var k = 1; k < burst.Count; k++)
                {
                    var previous = burst[k - 1];
                    if (previous.DurationMs is not { } took || previous.StartedAt.AddMilliseconds(took) > burst[k].StartedAt)
                    {
                        overlapping++;
                    }
                }

                var samePath = burst.Select(r => r.Endpoint).Distinct(StringComparer.Ordinal).Count() == 1;
                var key = samePath ? burst[0].Endpoint : burst[0].EndpointTemplate;
                var cancelled = burst.Count(r => r.IsCancelled);
                var triggers = burst.Select(r => r.TriggerDescription).Where(t => t is not null).Distinct().ToList();
                var evidence = $"{burst.Count} requests within {(burst[^1].StartedAt - burst[0].StartedAt).TotalMilliseconds:0} ms "
                    + (samePath ? "with different query values" : "for different ids, each from its own UI event")
                    + (triggers.Count > 0 ? "; triggered by " + string.Join(", ", triggers.Take(3)) : "; not attributable to a UI event")
                    + (overlapping > 0 ? $"; {overlapping} started while the previous one was still running, so an older response can arrive last" : "")
                    + (cancelled > 0 ? $"; {cancelled} cancelled" : "") + ".";
                var advice = overlapping > 0 && cancelled == 0
                    ? "Debounce the input that issues them, and cancel the superseded request (a CancellationTokenSource per query) so a slower, older response cannot overwrite the newer result."
                    : "Debounce the input that issues them (wait until typing pauses), or require a minimum query length.";
                yield return new BevDiagnostic(
                    Id,
                    overlapping > 0 && cancelled == 0 ? BevDiagnosticSeverity.Warning : BevDiagnosticSeverity.Info,
                    $"{key}: {burst.Count} requests in {options.HttpBurstWindowMs / 1000.0:0.#} s",
                    evidence,
                    advice,
                    Fingerprint: $"{Id}:{key}",
                    RelatedEventIds: burst.Select(r => r.TimelineEventId).Where(id => id > 0).Take(20).ToList());
            }
        }
    }

    /// <summary>
    /// One request per list item (N+1): the same endpoint called for one id after another, typically from each row's
    /// OnInitializedAsync. Each request has its own path, so neither the duplicate rule nor a burst by query values sees
    /// them; they share <see cref="BevHttpRecord.EndpointTemplate"/>. When most of them come from UI events of their own,
    /// they are a lookup typed into a box, which is <see cref="HttpBurstRule"/>'s.
    /// </summary>
    private sealed class HttpPerItemRule : IBevDiagnosticRule
    {
        public string Id => "http.per-item";

        public string Title => "One request per item";

        public string Description => "One endpoint was called for many different ids in a short window: a list whose items each load their own data.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            if (context is not DiagnosticContext { Session: { } session })
            {
                yield break;
            }

            var options = Opt(context);
            var count = Math.Max(2, options.HttpPerItemCount);
            foreach (var ordered in RecentByTemplate(session, context))
            {
                if (!ordered[0].EndpointTemplate.Contains("{id}", StringComparison.Ordinal)
                    || ordered.Select(r => r.Endpoint).Distinct(StringComparer.Ordinal).Take(count).Count() < count)
                {
                    continue;
                }

                var run = FirstWindow(ordered, TimeSpan.FromMilliseconds(options.HttpBurstWindowMs), w => w.Endpoints.Count >= count && !EachFromItsOwnEvent(w));
                if (run is null)
                {
                    continue;
                }

                var key = run[0].EndpointTemplate;
                var items = run.Select(r => r.Endpoint).Distinct(StringComparer.Ordinal).Count();
                var triggers = run.Select(r => r.TriggerDescription).Where(t => t is not null).Distinct().ToList();
                // Who sent them, from the stack: "OrderRow.OnInitializedAsync ×25" says which list's rows load their own.
                var callers = run.Where(r => r.Caller is not null).GroupBy(r => r.Caller!).OrderByDescending(g => g.Count()).Select(g => g.Key + " ×" + g.Count()).Take(3).ToList();
                var failed = run.Count(r => r.IsFailed);
                var evidence = $"{run.Count} requests for {items} different ids within {(run[^1].StartedAt - run[0].StartedAt).TotalMilliseconds:0} ms"
                    + (callers.Count > 0 ? "; sent from " + string.Join(", ", callers) : "")
                    + (triggers.Count > 0 ? "; during " + string.Join(", ", triggers.Take(3)) : callers.Count > 0 ? "" : "; not inside a UI event (lifecycle methods, such as each row's OnInitializedAsync)")
                    + (failed > 0 ? $"; {failed} failed" : "") + ".";
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Warning,
                    $"{key}: one request per item ({items} ids)",
                    evidence,
                    "Load the items' data in one request (an endpoint that takes the ids, or include it in the list's own response), or load an item's details only when it is opened or scrolled into view (Virtualize).",
                    Fingerprint: $"{Id}:{key}",
                    RelatedEventIds: run.Select(r => r.TimelineEventId).Where(id => id > 0).Take(20).ToList());
            }
        }
    }

    /// <summary>The last 30 s of requests per endpoint template, each group in start order.</summary>
    private static IEnumerable<List<BevHttpRecord>> RecentByTemplate(BevToolsSession session, IBevDiagnosticContext context) =>
        session.Http.Snapshot()
            .Where(r => context.Now - r.StartedAt < TimeSpan.FromSeconds(30))
            .GroupBy(r => r.EndpointTemplate, StringComparer.Ordinal)
            .Select(g => g.OrderBy(r => r.StartedAt).ToList());

    /// <summary>Most requests of the window come from a UI event of their own: typed, one by one.</summary>
    private static bool EachFromItsOwnEvent(RequestWindow window) => window.Triggers.Count * 2 >= window.Size;

    /// <summary>
    /// The first window of requests starting within <paramref name="span"/> of its first one that satisfies
    /// <paramref name="test"/>. The window slides over the requests once, keeping its distinct counts as it goes, so a
    /// storm of hundreds of requests - when the rules matter - costs a pass, not a pass per request.
    /// </summary>
    private static List<BevHttpRecord>? FirstWindow(List<BevHttpRecord> ordered, TimeSpan span, Func<RequestWindow, bool> test)
    {
        var window = new RequestWindow();
        var end = 0;
        for (var start = 0; start < ordered.Count; start++)
        {
            while (end < ordered.Count && ordered[end].StartedAt - ordered[start].StartedAt <= span)
            {
                window.Add(ordered[end++]);
            }

            if (test(window))
            {
                return ordered.GetRange(start, end - start);
            }

            window.Remove(ordered[start]);
        }

        return null;
    }

    private sealed class RequestWindow
    {
        public int Size { get; private set; }

        public DistinctCounter<string> Signatures { get; } = new();

        public DistinctCounter<string> Endpoints { get; } = new();

        public DistinctCounter<long> Triggers { get; } = new();

        public void Add(BevHttpRecord request)
        {
            Size++;
            Signatures.Add(request.Signature);
            Endpoints.Add(request.Endpoint);
            if (request.TriggerEventId is { } trigger)
            {
                Triggers.Add(trigger);
            }
        }

        public void Remove(BevHttpRecord request)
        {
            Size--;
            Signatures.Remove(request.Signature);
            Endpoints.Remove(request.Endpoint);
            if (request.TriggerEventId is { } trigger)
            {
                Triggers.Remove(trigger);
            }
        }
    }

    private sealed class DistinctCounter<T> where T : notnull
    {
        private readonly Dictionary<T, int> _counts = [];

        public int Count => _counts.Count;

        public void Add(T value) => _counts[value] = _counts.TryGetValue(value, out var n) ? n + 1 : 1;

        public void Remove(T value)
        {
            if (_counts.TryGetValue(value, out var n))
            {
                if (n <= 1)
                {
                    _counts.Remove(value);
                }
                else
                {
                    _counts[value] = n - 1;
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
            // The busiest second of the last half minute: a burst is over before anyone opens the panel to look.
            foreach (var identifier in context.EventsInWindow(BevToolsEventKind.JsInterop, BurstLookback).GroupBy(e => e.Title))
            {
                var group = DensestWindow(identifier, TimeSpan.FromSeconds(1));
                var count = group.Count;
                if (count >= options.ExcessiveJsInteropCount)
                {
                    var components = group.Select(e => e.ComponentName).Where(n => n is not null).Distinct().ToList();
                    yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Warning, $"{identifier.Key} called {count} times in 1 s", components.Count > 0 ? "From " + string.Join(", ", components) + "." : "Origin not attributable to a component.", "Throttle, batch arguments into one call, or keep the loop in JavaScript.", Fingerprint: $"{Id}:{identifier.Key}", RelatedEventIds: group.Take(10).Select(e => e.Id).ToList());
                }
            }
        }
    }

    private sealed class RepeatedErrorRule : IBevDiagnosticRule
    {
        public string Id => "error.repeated";

        public string Title => "Repeated error";

        public string Description => "The same error occurred several times, whatever ids or values its messages carried.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            var options = Opt(context);
            // Each occurrence is a timeline event that carries its error's group, so "Order 1042 not found" and "Order
            // 1043 not found" from the same place count together, also once their records left the error buffer.
            var occurrences = context.EventsInWindow(BevToolsEventKind.Error, TimeSpan.FromMinutes(5))
                .Where(e => !e.Category.StartsWith("log:", StringComparison.Ordinal))
                .Select(e => (Event: e, Message: e.Title.Replace("(repeat) ", ""), Group: e.TryGetData("errorGroup", out var g) ? g as string : null));
            foreach (var group in occurrences.GroupBy(o => o.Group ?? o.Message, StringComparer.Ordinal))
            {
                var count = group.Count();
                if (count < options.RepeatedErrorCount)
                {
                    continue;
                }

                var first = group.First();
                var name = first.Event.TryGetData("errorTemplate", out var template) && template is string named ? named : first.Message;
                var messages = group.Select(o => o.Message).Distinct(StringComparer.Ordinal).Count();
                yield return new BevDiagnostic(
                    Id,
                    BevDiagnosticSeverity.Error,
                    $"{name} occurred {count} times" + (messages > 1 ? $" with {messages} different messages" : ""),
                    first.Event.Detail ?? "",
                    "Open the Errors panel (Groups shows them together) to see the preceding events for each occurrence.",
                    Fingerprint: $"{Id}:{group.Key}",
                    ComponentInstanceId: first.Event.ComponentInstanceId,
                    RelatedEventIds: group.Take(10).Select(o => o.Event.Id).ToList());
            }
        }
    }
}
