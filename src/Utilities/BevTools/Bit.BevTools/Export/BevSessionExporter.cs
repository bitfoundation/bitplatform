using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Bit.BevTools;

/// <summary>What <see cref="BevSessionExporter.ToJson"/> includes. Everything is bounded by the session's own buffers.</summary>
public sealed class BevSessionExportOptions
{
    /// <summary>Include per-event timeline rows (the largest section). Default true.</summary>
    public bool IncludeTimeline { get; set; } = true;

    /// <summary>Include the structured <c>Data</c> bag of each timeline event. Default true.</summary>
    public bool IncludeEventData { get; set; } = true;

    /// <summary>Include individual JS interop calls in addition to the per-identifier aggregates. Default true.</summary>
    public bool IncludeInteropCalls { get; set; } = true;

    /// <summary>Include the change history of state providers (diffs only, never full snapshots). Default true.</summary>
    public bool IncludeStateHistory { get; set; } = true;

    /// <summary>
    /// Include the render profile per component type (<c>profile</c>), what <see cref="BevProfileComparison"/> compares.
    /// Default true; a frontend that polls and never shows it leaves it out, as it reads every retained render sample.
    /// </summary>
    public bool IncludeProfile { get; set; } = true;

    /// <summary>Include recently disposed components in the component list. Default true.</summary>
    public bool IncludeDisposedComponents { get; set; } = true;

    /// <summary>Pretty-print the JSON. Default true: exports are read by people first.</summary>
    public bool Indented { get; set; } = true;

    /// <summary>
    /// Only the timeline events after this id, plus the most recent ones (whose duration or detail can still change
    /// while their activity completes). For a frontend that already holds the rest; null writes the whole buffer.
    /// </summary>
    public long? TimelineAfterEventId { get; set; }

    /// <summary>Only these top-level sections ("components", "timeline", "http", …); null writes every one.</summary>
    public IReadOnlySet<string>? Sections { get; set; }

    internal bool Includes(string section) => Sections is null || Sections.Contains(section);
}

/// <summary>
/// Turns everything a session captured into a file a developer can attach to an issue or hand to a colleague. Nothing
/// new is read from the application here: the export contains only what the panels already show, with the same
/// redaction, so exporting is never a way to see more than BevTools would display.
/// </summary>
public static class BevSessionExporter
{
    /// <summary>Format identifier written into every export so readers can tell versions apart.</summary>
    public const string Format = "bit-bevtools/1";

    private const int RecentlyUpdatedEvents = 200;

    /// <summary>Full machine-readable export.</summary>
    /// <remarks>
    /// Call it on the session's dispatcher, as the panel and the protocol do: it reads the live stores, and when the
    /// diagnostics section is included and anything was captured since the rules last ran, it runs them first.
    /// </remarks>
    public static string ToJson(BevToolsSession session, BevSessionExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        options ??= new BevSessionExportOptions();
        // Before anything is written, so the session's version, the timeline and the findings in the file agree.
        if (options.Includes("diagnostics"))
        {
            EvaluateIfStale(session);
        }

        var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = options.Indented, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("format", Format);
            writer.WriteString("exportedAt", DateTimeOffset.UtcNow);
            WriteSession(writer, session);
            if (options.Includes("options"))
            {
                WriteOptions(writer, session);
            }

            if (options.Includes("capabilities"))
            {
                WriteCapabilities(writer, session);
            }

            WriteOverhead(writer, session.Overhead);
            if (options.Includes("components"))
            {
                WriteComponents(writer, session, options.IncludeDisposedComponents);
            }

            if (options.IncludeProfile && options.Includes("profile"))
            {
                BevRenderProfile.Of(session).Write(writer);
            }

            if (options.IncludeTimeline && options.Includes("timeline"))
            {
                var events = session.Timeline.Snapshot();
                if (options.TimelineAfterEventId is { } after)
                {
                    // The newest events are sent again: an HTTP request or a UI event in progress completes later.
                    var resent = Math.Max(0, events.Length - RecentlyUpdatedEvents);
                    events = events.Where((e, i) => e.Id > after || i >= resent).ToArray();
                }

                WriteTimeline(writer, events, options.IncludeEventData ? session.Inspector : null);
            }

            if (options.Includes("errors"))
            {
                var errorRecords = session.Errors.Snapshot();
                WriteErrors(writer, errorRecords);
                WriteErrorGroups(writer, errorRecords);
            }

            if (options.Includes("http"))
            {
                var requests = session.Http.Snapshot();
                WriteHttp(writer, requests);
                WriteHttpEndpoints(writer, requests);
            }

            if (options.Includes("interop"))
            {
                WriteInterop(writer, session, options.IncludeInteropCalls);
            }

            if (options.Includes("state"))
            {
                WriteState(writer, session, options.IncludeStateHistory);
            }

            if (options.Includes("diagnostics"))
            {
                WriteDiagnostics(writer, session.Diagnostics.Findings);
            }

            if (options.Includes("browser"))
            {
                WriteBrowser(writer, session.Browser);
            }

            if (options.Includes("routing"))
            {
                WriteRouting(writer, session);
            }

            if (options.Includes("forms"))
            {
                WriteForms(writer, session);
            }

            if (options.Includes("runtime"))
            {
                WriteRuntime(writer, session);
            }

            if (options.Includes("prerender"))
            {
                WritePrerender(writer, session.Prerender);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    /// <summary>
    /// A short human-readable summary (Markdown) for bug reports and chat: findings, errors, the hottest components,
    /// failed or slow requests and the tail of the timeline.
    /// </summary>
    /// <summary>
    /// Offers <see cref="ToJson"/> as a file download and reports the outcome. The one implementation behind the About
    /// panel button and the <c>session.export</c> command, so the file name and the messages cannot drift apart.
    /// </summary>
    internal static async Task DownloadJsonAsync(BevToolsSession session, IBevToolsCommandContext context)
    {
        var name = $"bit-bevtools-session-{session.Id}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
        var downloaded = await context.DownloadTextAsync(name, ToJson(session), "application/json");
        context.Notify(downloaded ? "Export downloaded: " + name : "Export unavailable: the browser bridge is not attached");
    }

    /// <summary>Copies <see cref="ToMarkdownReport"/> to the clipboard and reports the outcome (About panel and <c>session.report</c>).</summary>
    internal static async Task CopyMarkdownReportAsync(BevToolsSession session, IBevToolsCommandContext context)
    {
        var copied = await context.CopyTextAsync(ToMarkdownReport(session));
        context.Notify(copied ? "Report copied as Markdown" : "Clipboard unavailable");
    }

    /// <summary>"3 of 40 compared": a forced-render count only means something next to how many renders were compared.</summary>
    private static string ForcedRenders(BevComponentRecord component) => component.RendersWithComparedParameters == 0
        ? "–"
        : component.ParentRendersWithUnchangedParameters.ToString(CultureInfo.InvariantCulture) + " of "
            + component.RendersWithComparedParameters.ToString(CultureInfo.InvariantCulture) + " compared";

    /// <summary>The report <i>About → Copy report</i> gives. Call it on the session's dispatcher (see <see cref="ToJson"/>).</summary>
    public static string ToMarkdownReport(BevToolsSession session, int timelineTail = 40)
    {
        ArgumentNullException.ThrowIfNull(session);
        EvaluateIfStale(session);
        var sb = new StringBuilder();
        var framework = typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly.GetName().Version;
        sb.AppendLine("# BevTools report");
        sb.AppendLine();
        sb.Append("- Session #").Append(session.Id)
            .Append(" · platform ").Append(session.Platform ?? "unknown")
            .Append(session.RenderModeName is null ? "" : " (" + session.RenderModeName + ")")
            .Append(" · components ").Append(framework?.ToString() ?? "?")
            .Append(" · ").Append(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription)
            .AppendLine();
        sb.Append("- Started ").Append(Iso(session.StartedAt)).Append(" · exported ").Append(Iso(DateTimeOffset.UtcNow))
            .Append(session.IsCapturing ? "" : " · capture paused").AppendLine();
        sb.Append("- ").Append(session.Components.LiveCount).Append(" live components · ")
            .Append(session.Timeline.Count).Append(" timeline events · ")
            .Append(session.Errors.TotalOccurrences).Append(" error occurrences · ")
            .Append(session.Http.Count).Append(" HTTP requests (").Append(session.Http.FailedCount).Append(" failed) · ")
            .Append(session.Interop.Count).Append(" JS interop calls").AppendLine();
        sb.AppendLine();

        var findings = session.Diagnostics.Findings.Where(f => !f.Dismissed).ToArray();
        sb.Append("## Diagnostics (").Append(findings.Length).AppendLine(" active)");
        sb.AppendLine();
        if (findings.Length == 0)
        {
            sb.AppendLine("No active findings.");
        }

        foreach (var finding in findings)
        {
            var d = finding.Diagnostic;
            sb.Append("- **[").Append(d.Severity).Append("] ").Append(d.Title).Append("** (`").Append(d.RuleId).AppendLine("`)");
            sb.Append("  - ").AppendLine(d.Evidence);
            if (!string.IsNullOrEmpty(d.Suggestion))
            {
                sb.Append("  - Suggestion: ").AppendLine(d.Suggestion);
            }
        }

        sb.AppendLine();
        var errors = session.Errors.Snapshot();
        sb.Append("## Errors (").Append(errors.Length).AppendLine(" distinct)");
        sb.AppendLine();
        if (errors.Length == 0)
        {
            sb.AppendLine("No errors recorded.");
        }

        var groups = BevErrorGroups.Summarize(errors).Where(g => g.DistinctMessages > 1).OrderByDescending(g => g.Occurrences).Take(10).ToArray();
        if (groups.Length > 0)
        {
            sb.AppendLine("One problem with different values in its messages:");
            foreach (var g in groups)
            {
                sb.Append("- `").Append(g.Source).Append("` ").Append(ErrorCenter.ShortTypeName(g.ExceptionType)).Append(": ").Append(g.MessageTemplate.ReplaceLineEndings(" "))
                    .Append(" (×").Append(g.Occurrences).Append(", ").Append(g.DistinctMessages).Append(" messages)");
                if (g.SourceLocation is not null)
                {
                    sb.Append(" @ ").Append(g.SourceLocation);
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        foreach (var error in errors.Reverse().Take(25))
        {
            sb.Append("- ").Append(Time(error.LastSeen)).Append(" `").Append(error.Source).Append("` ")
                .Append(ErrorCenter.ShortTypeName(error.ExceptionType)).Append(": ").Append(error.Message.ReplaceLineEndings(" "));
            if (error.Count > 1)
            {
                sb.Append(" (×").Append(error.Count).Append(')');
            }

            if (error.ComponentName is not null)
            {
                sb.Append(" — ").Append(error.ComponentName);
            }

            if (error.SourceLocation is not null)
            {
                sb.Append(" @ ").Append(error.SourceLocation);
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("## Hottest components (by total render time)");
        sb.AppendLine();
        var hot = session.Components.All.Where(c => !c.IsBevTools && !c.IsHidden && c.RenderCount > 0).OrderByDescending(c => c.TotalRenderMs).Take(10).ToArray();
        if (hot.Length == 0)
        {
            sb.AppendLine("No renders captured.");
        }
        else
        {
            sb.AppendLine("| Component | Renders | Total | Avg | Max | Forced re-renders | Last cause |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|---|");
            foreach (var c in hot)
            {
                sb.Append("| ").Append(c.DisplayName).Append(c.IsDisposed ? " †" : "")
                    .Append(" | ").Append(c.RenderCount)
                    .Append(" | ").Append(Ms(c.TotalRenderMs))
                    .Append(" | ").Append(Ms(c.TotalRenderMs / c.RenderCount))
                    .Append(" | ").Append(Ms(c.MaxRenderMs))
                    .Append(" | ").Append(ForcedRenders(c))
                    .Append(" | ").Append(c.LastRender?.CauseDetail ?? "")
                    .AppendLine(" |");
            }
        }

        sb.AppendLine();
        var http = session.Http.Snapshot();
        var notable = http.Where(r => r.IsFailed || r.DurationMs >= session.Options.Diagnostics.SlowHttpMs).Reverse().Take(15).ToArray();
        sb.Append("## Failed or slow HTTP requests (").Append(notable.Length).Append(" of ").Append(http.Length).AppendLine(")");
        sb.AppendLine();
        if (notable.Length == 0)
        {
            sb.AppendLine("None.");
        }

        foreach (var r in notable)
        {
            sb.Append("- ").Append(Time(r.StartedAt)).Append(' ').Append(r.Method).Append(' ').Append(r.Url)
                .Append(" → ").Append(r.Error ?? (r.StatusCode?.ToString(CultureInfo.InvariantCulture) ?? "pending"))
                .Append(r.DurationMs is { } d ? " in " + Ms(d) : "")
                .Append(r.TriggerDescription is null ? "" : " (trigger: " + r.TriggerDescription + ")")
                .Append(r.Caller is null ? "" : " (from " + r.Caller + ")")
                .AppendLine();
        }

        sb.AppendLine();
        var endpoints = BevHttpEndpoints.Summarize(http).OrderByDescending(e => e.Requests).Take(10).ToArray();
        if (endpoints.Length > 0)
        {
            sb.AppendLine("## Busiest endpoints");
            sb.AppendLine();
            sb.AppendLine("| Endpoint | Requests | Failed | p50 | p95 |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            foreach (var e in endpoints)
            {
                sb.Append("| `").Append(e.Endpoint.Replace("|", "\\|", StringComparison.Ordinal).Replace("`", "'", StringComparison.Ordinal)).Append("` | ").Append(e.Requests).Append(" | ").Append(e.Failed)
                    .Append(" | ").Append(e.P50Ms is { } p50 ? Ms(p50) : "–").Append(" | ").Append(e.P95Ms is { } p95 ? Ms(p95) : "–").AppendLine(" |");
            }

            sb.AppendLine();
        }

        var events = session.Timeline.Snapshot();
        var tail = events.Skip(Math.Max(0, events.Length - timelineTail)).ToArray();
        sb.Append("## Timeline (last ").Append(tail.Length).Append(" of ").Append(events.Length).AppendLine(" events)");
        sb.AppendLine();
        sb.AppendLine("```");
        foreach (var e in tail)
        {
            sb.Append(Time(e.Timestamp)).Append(' ')
                .Append(e.Kind.ToString().ToLowerInvariant().PadRight(11))
                .Append(e.Severity == BevToolsSeverity.Info ? "  " : e.Severity == BevToolsSeverity.Warning ? "! " : "!!")
                .Append(' ').Append(e.Title);
            if (e.ComponentName is not null && e.Kind != BevToolsEventKind.Render)
            {
                sb.Append(" · ").Append(e.ComponentName);
            }

            if (e.DurationMs is { } ms)
            {
                sb.Append(" (").Append(Ms(ms)).Append(')');
            }

            if (e.ParentEventId is { } parent)
            {
                sb.Append(" ← #").Append(parent);
            }

            sb.AppendLine();
        }

        sb.AppendLine("```");
        return sb.ToString();
    }

    // The rules run at most once a second, and only while something shows their results (or EvaluateWhileClosed), so the
    // last run can be older than what the export holds. Running them here brings the findings up to the export; it
    // cannot recover what a rule's window (10 to 60 seconds back) no longer reaches - that is EvaluateWhileClosed's job.
    // A snapshot that nothing changed since is not re-evaluated.
    private static void EvaluateIfStale(BevToolsSession session)
    {
        if (session.Diagnostics.IsStale)
        {
            session.Diagnostics.Evaluate(force: true);
        }
    }

    private static void WriteSession(Utf8JsonWriter w, BevToolsSession session)
    {
        w.WriteStartObject("session");
        w.WriteNumber("id", session.Id);
        w.WriteNumber("version", session.Version);
        w.WriteString("startedAt", session.StartedAt);
        w.WriteString("platform", session.Platform);
        w.WriteString("valueAccess", session.ValueAccess.ToString());
        w.WriteBoolean("interactive", session.IsInteractive);
        w.WriteString("renderMode", session.RenderModeName);
        w.WriteBoolean("capturing", session.IsCapturing);
        w.WriteString("componentsVersion", typeof(Microsoft.AspNetCore.Components.ComponentBase).Assembly.GetName().Version?.ToString());
        w.WriteString("runtime", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        w.WriteString("bevToolsVersion", typeof(BevSessionExporter).Assembly.GetName().Version?.ToString());
        w.WriteString("enabledReason", session.Registry.EnabledReason);
        w.WriteStartArray("extensions");
        foreach (var name in session.Registry.ExtensionNames)
        {
            w.WriteStringValue(name);
        }

        w.WriteEndArray();
        var (activitySource, metrics) = session.FrameworkInstrumentation;
        w.WriteStartObject("frameworkDiagnostics");
        w.WriteBoolean("activitySourceRegistered", activitySource);
        w.WriteBoolean("metersRegistered", metrics);
        w.WriteBoolean("handleEventObserved", ActivityObserver.HandleEventActivitiesObserved);
        w.WriteBoolean("batchMetricsObserved", ActivityObserver.BatchMetricsObserved);
        w.WriteBoolean("lifecycleMetricsObserved", ActivityObserver.LifecycleMetricsObserved);
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteOptions(Utf8JsonWriter w, BevToolsSession session)
    {
        var o = session.Options;
        w.WriteStartObject("options");
        w.WriteNumber("maxEvents", o.MaxEvents);
        w.WriteNumber("maxRendersPerComponent", o.MaxRendersPerComponent);
        w.WriteNumber("maxTrackedComponents", o.MaxTrackedComponents);
        w.WriteBoolean("trackRenderCauses", o.TrackRenderCauses);
        w.WriteBoolean("recordRenderEvents", o.RecordRenderEvents);
        w.WriteBoolean("trackHttp", o.TrackHttp);
        w.WriteBoolean("captureHttpHeaders", o.CaptureHttpHeaders);
        w.WriteBoolean("captureHttpCallers", o.CaptureHttpCallers);
        w.WriteBoolean("trackJsInterop", o.TrackJsInterop);
        w.WriteBoolean("trackJsModuleReferences", o.TrackJsModuleReferences);
        w.WriteBoolean("enableBrowserBridge", o.EnableBrowserBridge);
        w.WriteBoolean("useFrameworkInstrumentation", o.UseFrameworkInstrumentation);
        w.WriteBoolean("captureComponentStateOnRender", o.CaptureComponentStateOnRender);
        w.WriteString("valueAccess", session.ValueAccess.ToString());
        w.WriteStartObject("diagnostics");
        var d = o.Diagnostics;
        w.WriteNumber("renderStormCount", d.RenderStormCount);
        w.WriteNumber("renderStormWindowMs", d.RenderStormWindowMs);
        w.WriteNumber("parentDrivenRenderCount", d.ParentDrivenRenderCount);
        w.WriteNumber("renderCascadeSize", d.RenderCascadeSize);
        w.WriteNumber("slowRenderMs", d.SlowRenderMs);
        w.WriteNumber("slowEventHandlerMs", d.SlowEventHandlerMs);
        w.WriteNumber("duplicateHttpCount", d.DuplicateHttpCount);
        w.WriteNumber("duplicateHttpWindowMs", d.DuplicateHttpWindowMs);
        w.WriteNumber("slowHttpMs", d.SlowHttpMs);
        w.WriteNumber("slowJsInteropMs", d.SlowJsInteropMs);
        w.WriteNumber("excessiveJsInteropCount", d.ExcessiveJsInteropCount);
        w.WriteNumber("repeatedErrorCount", d.RepeatedErrorCount);
        w.WriteBoolean("evaluateWhileClosed", d.EvaluateWhileClosed);
        w.WriteStartArray("disabledRules");
        foreach (var rule in d.DisabledRules)
        {
            w.WriteStringValue(rule);
        }

        w.WriteEndArray();
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteCapabilities(Utf8JsonWriter w, BevToolsSession session)
    {
        w.WriteStartArray("capabilities");
        foreach (var c in session.Capabilities)
        {
            w.WriteStartObject();
            w.WriteString("area", c.Area);
            w.WriteString("name", c.Name);
            w.WriteString("status", c.Status.ToString());
            w.WriteString("platforms", c.Platforms);
            w.WriteString("explanation", c.Explanation);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteOverhead(Utf8JsonWriter w, BevOverheadMeter o)
    {
        w.WriteStartObject("overhead");
        w.WriteNumber("uptimeSeconds", Round(o.UptimeSeconds));
        w.WriteNumber("totalMs", Round(o.TotalMs));
        w.WriteNumber("instrumentationMs", Round(o.InstrumentationMs));
        w.WriteNumber("instrumentationPerRenderMicroseconds", Round(o.InstrumentationPerRenderMicroseconds));
        w.WriteNumber("bevToolsRenderMs", Round(o.BevToolsRenderMs));
        w.WriteNumber("inspectionMs", Round(o.InspectionMs));
        w.WriteNumber("diagnosticsMs", Round(o.DiagnosticsMs));
        w.WriteNumber("treeRefreshMs", Round(o.TreeRefreshMs));
        w.WriteNumber("pageMappingMs", Round(o.PageMappingMs));
        w.WriteNumber("appRenderCount", o.AppRenderCount);
        w.WriteNumber("bevToolsRenderCount", o.BevToolsRenderCount);
        w.WriteEndObject();
    }

    private static void WriteComponents(Utf8JsonWriter w, BevToolsSession session, bool includeDisposed)
    {
        w.WriteStartObject("components");
        w.WriteBoolean("truncated", session.Components.IsTruncated);
        w.WriteNumber("live", session.Components.LiveCount);
        if (session.Components.BaselineAt is { } baseline)
        {
            w.WriteString("baselineAt", baseline);
        }

        w.WriteStartArray("items");
        foreach (var c in session.Components.All.Where(c => !c.IsBevTools && (includeDisposed || !c.IsDisposed)).OrderBy(c => c.InstanceId))
        {
            w.WriteStartObject();
            w.WriteNumber("instanceId", c.InstanceId);
            w.WriteString("type", c.TypeName);
            w.WriteString("displayName", c.DisplayName);
            if (c.ParentInstanceId is { } parent)
            {
                w.WriteNumber("parentInstanceId", parent);
            }

            if (c.ComponentId is { } componentId)
            {
                w.WriteNumber("rendererComponentId", componentId);
            }

            w.WriteBoolean("hidden", c.IsHidden);
            w.WriteBoolean("instrumented", c.IsInstrumented);
            if (c.RenderModeName is { } renderMode)
            {
                w.WriteString("renderMode", renderMode);
            }
            if (c.InstrumentationNote is not null)
            {
                w.WriteString("instrumentationNote", c.InstrumentationNote);
            }

            w.WriteString("createdAt", c.CreatedAt);
            w.WriteBoolean("disposed", c.IsDisposed);
            if (c.DisposedAt is { } disposedAt)
            {
                w.WriteString("disposedAt", disposedAt);
            }

            w.WriteNumber("renders", c.RenderCount);
            w.WriteNumber("totalRenderMs", Round(c.TotalRenderMs));
            w.WriteNumber("maxRenderMs", Round(c.MaxRenderMs));
            w.WriteNumber("lastRenderMs", Round(c.LastRenderMs));
            if (c.LastRenderAt is { } lastRenderAt)
            {
                w.WriteString("lastRenderAt", lastRenderAt);
            }

            w.WriteNumber("errors", c.ErrorCount);
            w.WriteNumber("parentRendersWithUnchangedParameters", c.ParentRendersWithUnchangedParameters);
            w.WriteNumber("rendersWithComparedParameters", c.RendersWithComparedParameters);
            if (c.SkippedRenders > 0)
            {
                w.WriteNumber("rendersWhilePaused", c.SkippedRenders);
            }

            if (c.BaselineAt is not null)
            {
                w.WriteNumber("rendersSinceBaseline", c.RendersSinceBaseline);
                w.WriteNumber("renderMsSinceBaseline", Round(c.RenderMsSinceBaseline));
            }

            if (c.LastRender is { } last)
            {
                w.WriteString("lastCause", RenderCauseNames.Of(last.Cause));
                w.WriteString("lastCauseDetail", last.CauseDetail);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    /// <summary>Writes the timeline; event data only when an <paramref name="inspector"/> is given to write it through.</summary>
    private static void WriteTimeline(Utf8JsonWriter w, BevToolsEvent[] events, BevObjectInspector? inspector)
    {
        w.WriteStartArray("timeline");
        foreach (var e in events)
        {
            w.WriteStartObject();
            w.WriteNumber("id", e.Id);
            w.WriteString("timestamp", e.Timestamp);
            w.WriteString("kind", e.Kind.ToString());
            w.WriteString("category", e.Category);
            w.WriteString("title", e.Title);
            if (e.Detail is not null)
            {
                w.WriteString("detail", e.Detail);
            }

            if (e.DurationMs is { } ms)
            {
                w.WriteNumber("durationMs", Round(ms));
            }

            w.WriteString("severity", e.Severity.ToString());
            if (e.ParentEventId is { } parent)
            {
                w.WriteNumber("parentEventId", parent);
            }

            if (e.ComponentInstanceId is { } component)
            {
                w.WriteNumber("componentInstanceId", component);
            }

            if (e.ComponentName is not null)
            {
                w.WriteString("componentName", e.ComponentName);
            }

            if (inspector is not null && e.Data is { Count: > 0 })
            {
                w.WriteStartObject("data");
                foreach (var (key, value) in e.Data)
                {
                    WriteValue(w, key, value, inspector);
                }

                w.WriteEndObject();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    /// <summary>Errors that are one problem (<see cref="BevErrorGroups"/>), most occurrences first.</summary>
    private static void WriteErrorGroups(Utf8JsonWriter w, BevErrorRecord[] errors)
    {
        w.WriteStartArray("errorGroups");
        foreach (var g in BevErrorGroups.Summarize(errors).OrderByDescending(g => g.Occurrences).ThenByDescending(g => g.LastSeen))
        {
            w.WriteStartObject();
            w.WriteString("group", g.Group);
            w.WriteString("source", g.Source);
            w.WriteString("exceptionType", g.ExceptionType);
            w.WriteString("messageTemplate", g.MessageTemplate);
            w.WriteNumber("occurrences", g.Occurrences);
            w.WriteNumber("records", g.Records);
            w.WriteNumber("distinctMessages", g.DistinctMessages);
            if (g.SourceLocation is not null)
            {
                w.WriteString("sourceLocation", g.SourceLocation);
            }

            w.WriteStartArray("components");
            foreach (var component in g.Components.Take(20))
            {
                w.WriteStringValue(component);
            }

            w.WriteEndArray();
            w.WriteString("firstSeen", g.FirstAt);
            w.WriteString("lastSeen", g.LastSeen);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteErrors(Utf8JsonWriter w, BevErrorRecord[] errors)
    {
        w.WriteStartArray("errors");
        foreach (var e in errors)
        {
            w.WriteStartObject();
            w.WriteNumber("id", e.Id);
            w.WriteString("firstSeen", e.At);
            w.WriteString("lastSeen", e.LastSeen);
            w.WriteNumber("count", e.Count);
            w.WriteString("source", e.Source);
            w.WriteString("exceptionType", e.ExceptionType);
            w.WriteString("message", e.Message);
            w.WriteString("messageTemplate", e.MessageTemplate);
            w.WriteString("group", e.Group);
            if (e.InnerError is not null)
            {
                w.WriteString("innerError", e.InnerError);
            }

            if (e.SourceLocation is not null)
            {
                w.WriteString("sourceLocation", e.SourceLocation);
            }

            if (e.ComponentInstanceId is { } component)
            {
                w.WriteNumber("componentInstanceId", component);
                w.WriteString("componentName", e.ComponentName);
            }

            if (e.PrecedingEventId is { } preceding)
            {
                w.WriteNumber("precedingEventId", preceding);
            }

            if (e.TimelineEventId > 0)
            {
                w.WriteNumber("timelineEventId", e.TimelineEventId);
            }

            if (e.StackTrace is not null)
            {
                w.WriteString("stackTrace", e.StackTrace);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    /// <summary>The requests in the buffer per endpoint template (ids in the path as {id}), busiest first.</summary>
    private static void WriteHttpEndpoints(Utf8JsonWriter w, BevHttpRecord[] requests)
    {
        w.WriteStartArray("httpEndpoints");
        foreach (var e in BevHttpEndpoints.Summarize(requests).OrderByDescending(e => e.Requests).ThenBy(e => e.Endpoint, StringComparer.Ordinal))
        {
            w.WriteStartObject();
            w.WriteString("endpoint", e.Endpoint);
            w.WriteNumber("requests", e.Requests);
            w.WriteNumber("distinctUrls", e.DistinctUrls);
            w.WriteNumber("failed", e.Failed);
            w.WriteNumber("cancelled", e.Cancelled);
            w.WriteNumber("pending", e.Pending);
            if (e.P50Ms is { } p50)
            {
                w.WriteNumber("p50Ms", Round(p50));
                w.WriteNumber("p95Ms", Round(e.P95Ms!.Value));
                w.WriteNumber("maxMs", Round(e.MaxMs!.Value));
            }

            if (e.ResponseBytes is { } bytes)
            {
                w.WriteNumber("responseBytes", bytes);
            }

            w.WriteString("lastAt", e.LastAt);
            if (e.Callers.Count > 0)
            {
                w.WriteNumber("callerCount", e.CallerCount);
                w.WriteStartArray("callers");
                foreach (var (component, sent) in e.Callers)
                {
                    w.WriteStartObject();
                    w.WriteString("component", component);
                    w.WriteNumber("requests", sent);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteHttp(Utf8JsonWriter w, BevHttpRecord[] requests)
    {
        w.WriteStartArray("http");
        foreach (var r in requests)
        {
            w.WriteStartObject();
            w.WriteNumber("id", r.Id);
            w.WriteString("startedAt", r.StartedAt);
            w.WriteString("method", r.Method);
            w.WriteString("url", r.Url);
            if (r.StatusCode is { } status)
            {
                w.WriteNumber("statusCode", status);
                w.WriteString("reasonPhrase", r.ReasonPhrase);
            }

            if (r.DurationMs is { } ms)
            {
                w.WriteNumber("durationMs", Round(ms));
            }

            if (r.RequestBytes is { } requestBytes)
            {
                w.WriteNumber("requestBytes", requestBytes);
            }

            if (r.ResponseBytes is { } responseBytes)
            {
                w.WriteNumber("responseBytes", responseBytes);
            }

            if (r.ContentType is not null)
            {
                w.WriteString("contentType", r.ContentType);
            }

            if (r.Error is not null)
            {
                w.WriteString("error", r.Error);
            }

            w.WriteBoolean("failed", r.IsFailed);
            w.WriteBoolean("cancelled", r.IsCancelled);
            w.WriteBoolean("pending", r.IsPending);
            if (r.TriggerEventId is { } trigger)
            {
                w.WriteNumber("triggerEventId", trigger);
                w.WriteString("trigger", r.TriggerDescription);
            }

            if (r.Caller is { } caller)
            {
                w.WriteString("caller", caller);
            }
            else if (r.CallerMissing is { } missing)
            {
                w.WriteString("callerMissing", missing);
            }

            if (r.TimelineEventId > 0)
            {
                w.WriteNumber("timelineEventId", r.TimelineEventId);
            }

            WriteHeaders(w, "requestHeaders", r.RequestHeaders);
            WriteHeaders(w, "responseHeaders", r.ResponseHeaders);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteHeaders(Utf8JsonWriter w, string name, IReadOnlyList<KeyValuePair<string, string>>? headers)
    {
        if (headers is null)
        {
            return;
        }

        w.WriteStartArray(name);
        foreach (var (key, value) in headers)
        {
            w.WriteStartObject();
            w.WriteString("name", key);
            w.WriteString("value", value);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteInterop(Utf8JsonWriter w, BevToolsSession session, bool includeCalls)
    {
        w.WriteStartObject("jsInterop");
        w.WriteStartArray("aggregates");
        foreach (var a in session.Interop.Aggregates().OrderByDescending(a => a.TotalMs))
        {
            w.WriteStartObject();
            w.WriteString("identifier", a.Identifier);
            w.WriteString("direction", a.Direction.ToString());
            w.WriteNumber("count", a.Count);
            w.WriteNumber("totalMs", Round(a.TotalMs));
            w.WriteNumber("maxMs", Round(a.MaxMs));
            w.WriteNumber("failures", a.Failures);
            w.WriteEndObject();
        }

        w.WriteEndArray();
        if (includeCalls)
        {
            w.WriteStartArray("calls");
            foreach (var c in session.Interop.Snapshot())
            {
                w.WriteStartObject();
                w.WriteNumber("id", c.Id);
                w.WriteString("at", c.At);
                w.WriteString("direction", c.Direction.ToString());
                w.WriteString("identifier", c.Identifier);
                w.WriteBoolean("sync", c.IsSync);
                w.WriteBoolean("module", c.IsModuleCall);
                w.WriteNumber("argumentCount", c.ArgumentCount);
                if (c.ArgumentTypes is not null)
                {
                    w.WriteString("argumentTypes", c.ArgumentTypes);
                }

                if (c.ResultType is not null)
                {
                    w.WriteString("resultType", c.ResultType);
                }

                if (c.DurationMs is { } ms)
                {
                    w.WriteNumber("durationMs", Round(ms));
                }

                w.WriteBoolean("succeeded", c.Succeeded);
                if (c.Error is not null)
                {
                    w.WriteString("error", c.Error);
                }

                if (c.ComponentInstanceId is { } component)
                {
                    w.WriteNumber("componentInstanceId", component);
                    w.WriteString("componentName", c.ComponentName);
                }

                w.WriteEndObject();
            }

            w.WriteEndArray();
        }

        w.WriteEndObject();
    }

    private static void WriteState(Utf8JsonWriter w, BevToolsSession session, bool includeHistory)
    {
        w.WriteStartArray("state");
        foreach (var entry in session.State.Entries)
        {
            w.WriteStartObject();
            w.WriteString("provider", entry.Name);
            w.WriteString("description", entry.Description);
            w.WriteString("origin", entry.Origin);
            w.WriteNumber("changeCount", entry.ChangeCount);
            if (entry.LastChangedAt is { } lastChanged)
            {
                w.WriteString("lastChangedAt", lastChanged);
            }

            if (entry.Error is not null)
            {
                w.WriteString("error", entry.Error);
            }

            if (includeHistory)
            {
                w.WriteStartArray("changes");
                foreach (var change in entry.Changes)
                {
                    w.WriteStartObject();
                    w.WriteNumber("id", change.Id);
                    w.WriteString("at", change.At);
                    if (change.Action is not null)
                    {
                        w.WriteString("action", change.Action);
                    }

                    if (change.Detail is not null)
                    {
                        w.WriteString("detail", change.Detail);
                    }

                    if (change.TriggerEventId is { } trigger)
                    {
                        w.WriteNumber("triggerEventId", trigger);
                    }

                    w.WriteStartArray("diff");
                    foreach (var d in change.Changes)
                    {
                        w.WriteStartObject();
                        w.WriteString("path", d.Path);
                        w.WriteString("kind", d.Kind);
                        w.WriteString("before", d.Before);
                        w.WriteString("after", d.After);
                        w.WriteEndObject();
                    }

                    w.WriteEndArray();
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteDiagnostics(Utf8JsonWriter w, BevDiagnosticRecord[] findings)
    {
        w.WriteStartArray("diagnostics");
        foreach (var f in findings)
        {
            var d = f.Diagnostic;
            w.WriteStartObject();
            w.WriteString("ruleId", d.RuleId);
            w.WriteString("severity", d.Severity.ToString());
            w.WriteString("title", d.Title);
            w.WriteString("evidence", d.Evidence);
            if (d.Suggestion is not null)
            {
                w.WriteString("suggestion", d.Suggestion);
            }

            w.WriteString("fingerprint", d.EffectiveFingerprint);
            if (d.ComponentInstanceId is { } component)
            {
                w.WriteNumber("componentInstanceId", component);
            }

            w.WriteString("firstSeen", f.FirstSeen);
            w.WriteString("lastSeen", f.LastSeen);
            w.WriteBoolean("dismissed", f.Dismissed);
            if (d.RelatedEventIds is { Count: > 0 })
            {
                w.WriteStartArray("relatedEventIds");
                foreach (var id in d.RelatedEventIds)
                {
                    w.WriteNumberValue(id);
                }

                w.WriteEndArray();
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteBrowser(Utf8JsonWriter w, BevBrowserState b)
    {
        w.WriteStartObject("browser");
        w.WriteBoolean("bridgeAttached", b.BridgeAttached);
        if (b.BridgeError is not null)
        {
            w.WriteString("bridgeError", b.BridgeError);
        }

        if (b.Accessibility is { } accessibility)
        {
            w.WriteStartArray("accessibility");
            foreach (var finding in accessibility)
            {
                w.WriteStartObject();
                w.WriteString("rule", finding.Rule);
                w.WriteString("wcag", finding.Wcag);
                w.WriteString("element", finding.Element);
                w.WriteString("component", finding.Component);
                w.WriteString("message", finding.Message);
                w.WriteEndObject();
            }

            w.WriteEndArray();
        }

        if (b.Online is { } online)
        {
            w.WriteBoolean("online", online);
        }

        w.WriteString("visibility", b.Visibility);
        w.WriteString("userAgent", b.UserAgent);
        w.WriteString("reconnectState", b.ReconnectState);
        w.WriteNumber("reconnectCount", b.ReconnectCount);
        w.WriteNumber("jsErrorCount", b.JsErrorCount);
        w.WriteNumber("jsToDotNetCalls", b.JsToDotNetCalls);
        if (b.JsHeapUsedMb is { } heap)
        {
            w.WriteNumber("jsHeapUsedMb", Round(heap));
        }

        if (b.NavigationLoadMs is { } load)
        {
            w.WriteNumber("navigationLoadMs", Round(load));
        }

        w.WriteNumber("enhancedNavigations", b.EnhancedNavigations);
        w.WriteNumber("resourceLoadErrors", b.ResourceLoadErrors);
        w.WriteNumber("cspViolations", b.CspViolations);
        w.WriteStartArray("sockets");
        foreach (var s in b.Sockets)
        {
            w.WriteStartObject();
            w.WriteNumber("id", s.Id);
            w.WriteString("url", s.Url);
            w.WriteString("kind", s.Kind);
            w.WriteString("protocol", s.Protocol);
            w.WriteBoolean("closed", s.IsClosed);
            if (s.CloseCode is { } code)
            {
                w.WriteNumber("closeCode", code);
            }

            WriteStat(w, "sent", s.Sent);
            WriteStat(w, "received", s.Received);
            WriteStat(w, "bevTools", s.Own);
            w.WriteStartObject("renderBatches");
            w.WriteNumber("count", s.RenderBatches.Count);
            w.WriteNumber("bytes", s.RenderBatches.Bytes);
            w.WriteNumber("maxBytes", s.RenderBatches.MaxBytes);
            w.WriteNumber("applyMs", Round(s.RenderBatches.ApplyMs));
            w.WriteNumber("maxApplyMs", Round(s.RenderBatches.MaxApplyMs));
            w.WriteNumber("bevToolsOnly", s.RenderBatches.BevTools);
            w.WriteEndObject();
            w.WriteStartObject("messages");
            foreach (var (kind, stat) in s.Messages.OrderByDescending(m => m.Value.Bytes))
            {
                WriteStat(w, kind, stat);
            }

            w.WriteEndObject();
            w.WriteEndObject();
        }

        w.WriteEndArray();

        // Storage entries are deliberately omitted: their previews are shown on request in the panel and have no
        // place in a file that gets attached to an issue.
        w.WriteEndObject();
    }

    private static void WriteStat(Utf8JsonWriter w, string name, BevMessageStat stat)
    {
        w.WriteStartObject(name);
        w.WriteNumber("count", stat.Count);
        w.WriteNumber("bytes", stat.Bytes);
        w.WriteNumber("maxBytes", stat.MaxBytes);
        w.WriteEndObject();
    }

    /// <summary>The forms panel: fields with their changes and (redacted) messages, validations, validators; never a value.</summary>
    private static void WriteForms(Utf8JsonWriter w, BevToolsSession session)
    {
        session.Forms.Discover();
        w.WriteStartArray("forms");
        foreach (var form in session.Forms.Forms())
        {
            w.WriteStartObject();
            w.WriteString("model", form.ModelType);
            w.WriteString("owner", form.OwnerName);
            if (form.OwnerInstanceId is { } owner)
            {
                w.WriteNumber("ownerInstanceId", owner);
            }

            WriteStrings(w, "validators", form.Validators);
            WriteStrings(w, "messageDisplays", form.MessageDisplays);
            WriteStrings(w, "validationAttributes", form.ValidationAttributes);
            w.WriteBoolean("modified", form.Modified);
            w.WriteNumber("messages", form.MessageCount);
            w.WriteNumber("validations", form.Validations);
            if (form.LastValidationPassed is { } passed)
            {
                w.WriteBoolean("lastValidationPassed", passed);
            }

            w.WriteStartArray("fields");
            foreach (var field in form.Fields)
            {
                w.WriteStartObject();
                w.WriteString("name", field.Name);
                w.WriteBoolean("modified", field.Modified);
                w.WriteNumber("changes", field.Changes);
                WriteStrings(w, "messages", field.Messages);
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteStrings(Utf8JsonWriter w, string name, IReadOnlyList<string> values)
    {
        w.WriteStartArray(name);
        foreach (var value in values)
        {
            w.WriteStringValue(value);
        }

        w.WriteEndArray();
    }

    private static void WriteRouting(Utf8JsonWriter w, BevToolsSession session)
    {
        BevRoutingSnapshot routing;
        try
        {
            routing = BevRoutingReader.Read(session);
        }
        catch (Exception ex)
        {
            w.WriteString("routing", "«could not be read: " + ex.GetType().Name + "»");
            return;
        }

        w.WriteStartObject("routing");
        var current = routing.Current;
        w.WriteStartObject("current");
        w.WriteString("uri", current.Uri);
        w.WriteString("page", current.PageType);
        w.WriteString("template", current.Template);
        w.WriteBoolean("templateInferred", current.TemplateInferred);
        w.WriteString("layout", current.Layout);
        w.WriteString("source", current.Source);
        WritePairs(w, "routeValues", current.RouteValues);
        WritePairs(w, "query", current.Query);
        w.WriteEndObject();
        w.WriteString("routesSource", routing.RoutesSource);
        w.WriteStartArray("routes");
        foreach (var route in routing.Routes)
        {
            w.WriteStartObject();
            w.WriteStartArray("templates");
            foreach (var template in route.Templates)
            {
                w.WriteStringValue(template);
            }

            w.WriteEndArray();
            w.WriteString("page", route.PageType);
            w.WriteString("assembly", route.Assembly);
            if (route.Authorization.Count > 0)
            {
                w.WriteStartArray("authorization");
                foreach (var a in route.Authorization)
                {
                    w.WriteStringValue(a);
                }

                w.WriteEndArray();
            }

            if (route.AllowAnonymous)
            {
                w.WriteBoolean("allowAnonymous", true);
            }

            if (route.RenderMode is not null)
            {
                w.WriteString("renderMode", route.RenderMode);
            }

            if (route.Layout is not null)
            {
                w.WriteString("layout", route.Layout);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WritePairs(Utf8JsonWriter w, string name, IReadOnlyList<KeyValuePair<string, string>> pairs)
    {
        w.WriteStartObject(name);
        foreach (var (key, value) in pairs)
        {
            w.WriteString(key, value);
        }

        w.WriteEndObject();
    }

    private static void WriteRuntime(Utf8JsonWriter w, BevToolsSession session)
    {
        w.WriteStartArray("runtime");
        foreach (var section in BevRuntimeInfo.Build(session))
        {
            w.WriteStartObject();
            w.WriteString("title", section.Title);
            w.WriteString("scope", section.Scope);
            w.WriteStartArray("facts");
            foreach (var fact in section.Facts)
            {
                w.WriteStartObject();
                w.WriteString("name", fact.Name);
                w.WriteString("value", fact.Value);
                if (fact.Hint is not null)
                {
                    w.WriteString("hint", fact.Hint);
                }

                w.WriteString("severity", fact.Severity.ToString());
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WritePrerender(Utf8JsonWriter w, BevPrerenderSummary? prerender)
    {
        if (prerender is null)
        {
            return;
        }

        w.WritePropertyName("prerender");
        JsonSerializer.Serialize(w, prerender, PrerenderJson);
    }

    private static readonly JsonSerializerOptions PrerenderJson = new(JsonSerializerDefaults.Web) { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Writes an arbitrary <c>Data</c> value without ever throwing: primitives as themselves, everything else as the
    /// display the timeline panel shows for it. Both go through the inspector, so a sensitive key is redacted and an
    /// object with sensitive members is never written through its ToString (a record prints every member).
    /// </summary>
    private static void WriteValue(Utf8JsonWriter w, string key, object? value, BevObjectInspector inspector)
    {
        BevInspectedNode node;
        try
        {
            node = inspector.DescribeMetadata(key, value);
        }
        catch (Exception ex)
        {
            // A value formatter or a Count getter threw; the value is not written rather than written unchecked.
            w.WriteString(key, "«inspection threw " + ex.GetType().Name + "»");
            return;
        }

        if (node.Kind == BevNodeKind.Redacted)
        {
            w.WriteString(key, node.Display == BevRedactor.HiddenValue ? BevRedactor.HiddenValue : BevRedactor.RedactedValue);
            return;
        }

        switch (value)
        {
            case null:
                w.WriteNull(key);
                break;
            case string s:
                w.WriteString(key, s);
                break;
            case bool b:
                w.WriteBoolean(key, b);
                break;
            case int i:
                w.WriteNumber(key, i);
                break;
            case long l:
                w.WriteNumber(key, l);
                break;
            case double d when double.IsFinite(d):
                w.WriteNumber(key, Round(d));
                break;
            case float f when float.IsFinite(f):
                w.WriteNumber(key, Round(f));
                break;
            case decimal m:
                w.WriteNumber(key, m);
                break;
            case DateTimeOffset dto:
                w.WriteString(key, dto);
                break;
            case DateTime dt:
                w.WriteString(key, dt);
                break;
            case IEnumerable<string> strings:
                w.WriteStartArray(key);
                foreach (var item in strings.Take(100))
                {
                    w.WriteStringValue(item is null ? null : BevRedactor.RedactText(item));
                }

                w.WriteEndArray();
                break;
            default:
                var text = node.Display;
                w.WriteString(key, text.Length > 500 ? text[..500] + "…" : text);
                break;
        }
    }

    private static double Round(double value) => Math.Round(value, 3);

    private static string Iso(DateTimeOffset t) => t.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string Time(DateTimeOffset t) => t.ToUniversalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    private static string Ms(double ms) => ms switch
    {
        < 10 => ms.ToString("0.00", CultureInfo.InvariantCulture) + " ms",
        < 1000 => ms.ToString("0.0", CultureInfo.InvariantCulture) + " ms",
        _ => (ms / 1000).ToString("0.00", CultureInfo.InvariantCulture) + " s",
    };
}
