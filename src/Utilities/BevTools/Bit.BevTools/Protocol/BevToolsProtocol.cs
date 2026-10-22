using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Bit.BevTools;

/// <summary>
/// The protocol external frontends - the companion window, the browser extension - speak to a BevTools runtime through
/// the page hub (<c>window.__BIT_BEVTOOLS__.request(target, method, params)</c>). Requests and answers are JSON. Every
/// answer is built from what the panels read, through the same inspection and redaction, so a frontend never sees more
/// than the in-app panel would: the protocol is another way to look, not another way in.
/// <list type="table">
/// <item><term>hello</term><description>who this runtime is: session, platform, render mode, value access, versions.</description></item>
/// <item><term>snapshot</term><description><c>{ since?, timelineAfter?, sections? }</c>: the session as <c>bit-bevtools/1</c>, or <c>{ unchanged: true }</c> when nothing changed since <c>since</c>.</description></item>
/// <item><term>inspect</term><description><c>{ instanceId }</c>: one component's parameters, cascading values, state, services, bindings, lifecycle and renders.</description></item>
/// <item><term>expand</term><description><c>{ instanceId, section, name, path }</c>: the children of one inspected value.</description></item>
/// <item><term>command</term><description><c>{ name, argument? }</c>: pause, resume, clear, baseline, evaluate, dismiss.</description></item>
/// <item><term>export</term><description><c>{ format: json | markdown | trace, since? }</c>: a file, as the About panel offers it.</description></item>
/// </list>
/// </summary>
public static class BevToolsProtocol
{
    /// <summary>The protocol version; a frontend that speaks another one says so instead of misreading answers.</summary>
    public const int Version = 1;

    private const int RecentRenders = 30;

    private static readonly JsonWriterOptions Compact = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Answers one request. Never throws: a failure is an answer with an <c>error</c>.</summary>
    public static string Handle(BevToolsSession session, string method, string? arguments)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsEnabled || session.IsDisposed)
        {
            return Error("BevTools is not running in this session.");
        }

        JsonElement args = default;
        try
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                args = JsonDocument.Parse(arguments).RootElement.Clone();
            }

            return method switch
            {
                "hello" => Hello(session),
                "snapshot" => Snapshot(session, args),
                "inspect" => Inspect(session, args),
                "expand" => Expand(session, args),
                "command" => Command(session, args),
                "export" => Export(session, args),
                _ => Error("Unknown method '" + method + "'."),
            };
        }
        catch (Exception ex)
        {
            return Error(ex.GetType().Name + ": " + BevRedactor.RedactText(ex.Message));
        }
    }

    private static string Hello(BevToolsSession session) => Write(w =>
    {
        w.WriteNumber("protocol", Version);
        w.WriteNumber("sessionId", session.Id);
        w.WriteNumber("version", session.Version);
        w.WriteString("platform", session.Platform);
        w.WriteString("renderMode", session.RenderModeName);
        w.WriteBoolean("interactive", session.IsInteractive);
        w.WriteBoolean("capturing", session.IsCapturing);
        w.WriteString("valueAccess", session.ValueAccess.ToString());
        w.WriteString("startedAt", session.StartedAt);
        w.WriteString("bevToolsVersion", typeof(BevToolsProtocol).Assembly.GetName().Version?.ToString());
        w.WriteString("runtime", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        w.WriteString("exportFormat", BevSessionExporter.Format);
    });

    private static string Snapshot(BevToolsSession session, JsonElement args)
    {
        var since = Long(args, "since");
        // Closing the batch and refreshing the tree is what the in-app panel does before it reads; the frontend reads
        // the same state, or a click that just happened would show up one poll late.
        session.RenderTracker.CloseBatch();
        session.Components.Refresh();
        session.Diagnostics.Evaluate();
        if (since is { } known && known == session.Version)
        {
            return Write(w =>
            {
                w.WriteBoolean("unchanged", true);
                w.WriteNumber("version", session.Version);
            });
        }

        HashSet<string>? sections = null;
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("sections", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            sections = list.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);
        }

        var export = BevSessionExporter.ToJson(session, new BevSessionExportOptions
        {
            Indented = false,
            TimelineAfterEventId = Long(args, "timelineAfter"),
            Sections = sections,
        });
        var version = session.Version;
        return Write(w =>
        {
            w.WriteNumber("version", version);
            w.WritePropertyName("session");
            w.WriteRawValue(export, skipInputValidation: true);
        });
    }

    private static string Inspect(BevToolsSession session, JsonElement args)
    {
        if (Long(args, "instanceId") is not { } id || session.Components.Get(id) is not { IsBevTools: false } record)
        {
            return Error("No such component.");
        }

        session.InspectExternally(record);
        var component = record.Component;
        var (parameters, cascading, injected) = BevComponentMembers.Of(record.Type);
        var graph = session.Registry.ServiceGraph;
        var byComponentId = session.Components.All.Where(c => c.ComponentId is not null && !c.IsDisposed).GroupBy(c => c.ComponentId!.Value).ToDictionary(g => g.Key, g => g.Last());
        var wiring = BevComponentWiringReader.Read(record, cid => byComponentId.GetValueOrDefault(cid));
        return Write(w =>
        {
            w.WriteNumber("instanceId", record.InstanceId);
            w.WriteString("type", record.TypeName);
            w.WriteString("displayName", record.DisplayName);
            w.WriteString("assembly", record.Type.Assembly.GetName().Name);
            w.WriteString("source", BevFmt.ComponentFile(record.Type));
            if (record.ParentInstanceId is { } parent)
            {
                w.WriteNumber("parentInstanceId", parent);
            }

            w.WriteString("renderMode", record.RenderModeName);
            for (var ancestor = record.Parent; ancestor is not null && record.RenderModeName is null; ancestor = ancestor.Parent)
            {
                if (ancestor.RenderModeName is { } inherited)
                {
                    w.WriteString("inheritedRenderMode", inherited);
                    w.WriteNumber("inheritedFrom", ancestor.InstanceId);
                    break;
                }
            }

            w.WriteBoolean("disposed", record.IsDisposed);
            w.WriteBoolean("instrumented", record.IsInstrumented);
            w.WriteString("instrumentationNote", record.InstrumentationNote);
            w.WriteNumber("renders", record.RenderCount);
            w.WriteNumber("totalRenderMs", Math.Round(record.TotalRenderMs, 3));
            w.WriteNumber("maxRenderMs", Math.Round(record.MaxRenderMs, 3));
            w.WriteNumber("lastRenderMs", Math.Round(record.LastRenderMs, 3));
            w.WriteNumber("forcedRenders", record.ParentRendersWithUnchangedParameters);
            w.WriteNumber("comparedRenders", record.RendersWithComparedParameters);
            w.WriteString("createdAt", record.CreatedAt);
            WriteTime(w, "firstRenderAt", record.FirstRenderAt);
            WriteTime(w, "lastRenderAt", record.LastRenderAt);
            WriteTime(w, "disposedAt", record.DisposedAt);

            w.WriteStartObject("causes");
            foreach (var cause in Enum.GetValues<BevRenderCause>())
            {
                if (record.RendersByCause(cause) is > 0 and var count)
                {
                    w.WriteNumber(RenderCauseNames.Of(cause), count);
                }
            }

            w.WriteEndObject();
            w.WriteStartArray("recentRenders");
            foreach (var r in record.Renders.Reverse().Take(RecentRenders))
            {
                w.WriteStartObject();
                w.WriteString("at", r.At);
                w.WriteNumber("durationMs", Math.Round(r.DurationMs, 3));
                w.WriteString("cause", RenderCauseNames.Of(r.Cause));
                w.WriteString("detail", r.CauseDetail);
                w.WriteNumber("batch", r.BatchId);
                w.WriteNumber("eventId", r.EventId);
                w.WriteBoolean("forced", r.IsWastedRender);
                w.WriteBoolean("failed", r.Failed);
                WriteStrings(w, "changedParameters", r.ChangedParameters);
                WriteStrings(w, "changedState", r.ChangedState);
                w.WriteEndObject();
            }

            w.WriteEndArray();
            WriteNodes(w, "parameters", parameters.Select(p => session.Inspector.Describe(p.Name, BevComponentMembers.SafeGet(p, component), "", p)));
            WriteNodes(w, "cascading", cascading.Select(p => session.Inspector.Describe(p.Name, BevComponentMembers.SafeGet(p, component), "", p)));
            WriteNodes(w, "state", ComponentStateReader.Fields(record.Type).Select(f => session.Inspector.Describe(f.Name, BevComponentMembers.SafeGet(f.Field, component), "", f.Member)));

            w.WriteStartArray("services");
            foreach (var p in injected)
            {
                var service = graph?.Services.LastOrDefault(s => s.ServiceType == p.PropertyType && !s.IsKeyed);
                var value = BevComponentMembers.SafeGet(p, component);
                w.WriteStartObject();
                w.WriteString("name", p.Name);
                w.WriteString("service", TypeNames.Short(p.PropertyType));
                w.WriteString("lifetime", service is null ? null : BevFmt.Lifetime(service.Lifetime));
                // The implementation's type, never its value: services hold configuration and other users' data.
                w.WriteString("implementation", value is BevTrackingJSRuntime tracking ? TypeNames.Short(tracking.Inner.GetType()) + " (tracked by BevTools)" : value is null ? service?.ImplementationName : TypeNames.Short(value.GetType()));
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteStartObject("wiring");
            w.WriteNumber("total", wiring.TotalBindings);
            w.WriteString("unavailable", wiring.Unavailable);
            w.WriteStartArray("bindings");
            foreach (var b in wiring.Bindings)
            {
                w.WriteStartObject();
                w.WriteString("target", b.Target);
                w.WriteString("event", b.Event);
                w.WriteString("handler", b.Handler);
                w.WriteString("kind", b.Kind.ToString());
                if (b.ChildInstanceId is { } child)
                {
                    w.WriteNumber("childInstanceId", child);
                }

                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();

            w.WriteStartArray("children");
            foreach (var child in session.Components.All.Where(c => !c.IsDisposed && !c.IsBevTools && c.Parent == record).OrderBy(c => c.ComponentId))
            {
                w.WriteNumberValue(child.InstanceId);
            }

            w.WriteEndArray();
            w.WriteStartArray("errors");
            foreach (var e in session.Errors.Snapshot().Where(e => e.ComponentInstanceId == record.InstanceId).Reverse().Take(20))
            {
                w.WriteStartObject();
                w.WriteNumber("id", e.Id);
                w.WriteString("type", ErrorCenter.ShortTypeName(e.ExceptionType));
                w.WriteString("message", e.Message);
                w.WriteNumber("count", e.Count);
                w.WriteEndObject();
            }

            w.WriteEndArray();
        });
    }

    private static string Expand(BevToolsSession session, JsonElement args)
    {
        if (Long(args, "instanceId") is not { } id || session.Components.Get(id) is not { IsBevTools: false } record)
        {
            return Error("No such component.");
        }

        var section = String(args, "section") ?? "";
        var name = String(args, "name") ?? "";
        var (value, member, found) = BevComponentMembers.Read(record, section, name);
        if (!found)
        {
            return Error("No such member.");
        }

        // The root's own name and member decide its redaction, exactly as when it was described; a redacted root has no children.
        if (session.Inspector.Describe(name, value, "", member) is not { IsExpandable: true })
        {
            return Write(w => WriteNodes(w, "children", []));
        }

        var children = session.Inspector.Children(value, String(args, "path") ?? "");
        return Write(w => WriteNodes(w, "children", children));
    }

    private static string Command(BevToolsSession session, JsonElement args)
    {
        var name = String(args, "name");
        var argument = String(args, "argument");
        string done;
        switch (name)
        {
            case "pause":
                session.PauseCapture();
                done = "Capture paused";
                break;
            case "resume":
                session.ResumeCapture();
                done = "Capture resumed";
                break;
            case "clear":
                session.ClearAll();
                done = "Captured event histories cleared";
                break;
            case "baseline":
                session.Components.MarkBaseline();
                done = "Profiler baseline reset";
                break;
            case "evaluate":
                session.Diagnostics.Evaluate(force: true);
                done = "Diagnostics evaluated";
                break;
            case "dismiss" when argument is not null:
                session.Diagnostics.Dismiss(argument);
                done = "Finding dismissed";
                break;
            default:
                return Error("Unknown command '" + name + "'.");
        }

        session.Ui.Touch();
        return Write(w =>
        {
            w.WriteString("done", done);
            w.WriteNumber("version", session.Version);
        });
    }

    private static string Export(BevToolsSession session, JsonElement args)
    {
        var format = String(args, "format") ?? "json";
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var (fileName, contentType, content) = format switch
        {
            "markdown" => ($"bit-bevtools-report-{session.Id}-{stamp}.md", "text/markdown", BevSessionExporter.ToMarkdownReport(session)),
            "trace" => ($"bit-bevtools-trace-{session.Id}-{stamp}.json", "application/json", BevTraceExporter.ToChromeTrace(session, new BevTraceExportOptions
            {
                Since = String(args, "since") is { } since && DateTimeOffset.TryParse(since, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var at) ? at : null,
            })),
            _ => ($"bit-bevtools-session-{session.Id}-{stamp}.json", "application/json", BevSessionExporter.ToJson(session)),
        };

        return Write(w =>
        {
            w.WriteString("fileName", fileName);
            w.WriteString("contentType", contentType);
            w.WriteString("content", content);
        });
    }

    private static void WriteNodes(Utf8JsonWriter w, string name, IEnumerable<BevInspectedNode> nodes)
    {
        w.WriteStartArray(name);
        foreach (var node in nodes)
        {
            w.WriteStartObject();
            w.WriteString("name", node.Name);
            w.WriteString("type", node.TypeName);
            w.WriteString("display", node.Display);
            w.WriteString("kind", node.Kind.ToString());
            w.WriteString("path", node.Path);
            w.WriteBoolean("expandable", node.IsExpandable);
            if (node.Count is { } count)
            {
                w.WriteNumber("count", count);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    private static void WriteStrings(Utf8JsonWriter w, string name, IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return;
        }

        w.WriteStartArray(name);
        foreach (var value in values)
        {
            w.WriteStringValue(value);
        }

        w.WriteEndArray();
    }

    private static void WriteTime(Utf8JsonWriter w, string name, DateTimeOffset? value)
    {
        if (value is { } at)
        {
            w.WriteString(name, at);
        }
    }

    private static long? Long(JsonElement args, string name) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

    private static string? String(JsonElement args, string name) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string Error(string message) => Write(w => w.WriteString("error", message));

    private static string Write(Action<Utf8JsonWriter> body)
    {
        var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, Compact))
        {
            w.WriteStartObject();
            body(w);
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }
}
