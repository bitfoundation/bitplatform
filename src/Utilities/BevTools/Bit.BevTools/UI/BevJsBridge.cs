using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BevTools;

public sealed class BevJsErrorPayload
{
    public string? Message { get; set; }

    public string? Source { get; set; }

    public string? Stack { get; set; }

    public string? Type { get; set; }
}

public sealed class BevJsCallEntry
{
    public string? Identifier { get; set; }

    public int Args { get; set; }

    public bool Sync { get; set; }

    public double DurationMs { get; set; }

    public string? Error { get; set; }

    public long At { get; set; }

    /// <summary>How long before the report the call was made; null from a hub that does not say.</summary>
    public double? AgeMs { get; set; }

    /// <summary>Made while a requestAnimationFrame callback ran; false from a hub that does not say.</summary>
    public bool Frame { get; set; }
}

public sealed class BevBrowserSnapshot
{
    public bool? Online { get; set; }

    public string? Visibility { get; set; }

    public string? UserAgent { get; set; }

    public double? JsHeapUsedMb { get; set; }

    public double? JsHeapLimitMb { get; set; }

    public double? NavigationLoadMs { get; set; }

    public double? DomContentLoadedMs { get; set; }

    public int? ResourceCount { get; set; }

    /// <summary>The id the page hub gave the runtime that attached.</summary>
    public string? TargetId { get; set; }

    /// <summary>Which timing entries the browser has; null from a hub that does not say.</summary>
    public BevResponsivenessSupport? Responsiveness { get; set; }
}

/// <summary>
/// Receives browser-side reports from bit-bevtools.js, and answers the protocol requests of external frontends (the
/// companion window, the browser extension). One instance per BevTools panel (per session).
/// </summary>
public sealed class BevJsBridge(BevToolsSession session, Action toggle, Action? openPalette = null, Action<bool>? showRequested = null)
{
    private const int MaxJsErrorTextLength = 4000;
    private const int MaxJsErrorTypeLength = 200;

    private const int MaxResolvedObjects = 512;

    /// <summary>How many seconds' routine events a later report may still add to, at most.</summary>
    private const int MaxOpenSeconds = 256;

    private int _lastLargeMessageReported;
    private double _largeMessagesReportedUntil;

    /// <summary>
    /// What each object id JavaScript called was, once resolved: ids are never reused, and a reference disposed before
    /// its calls are reported can no longer be looked up. Names only; nothing here keeps an application object alive.
    /// </summary>
    private readonly Dictionary<long, (string TypeName, long? ComponentInstanceId, string? ComponentName)> _resolvedObjects = [];

    /// <summary>
    /// The routine events of the latest seconds, by name, second and whether made from animation frames: the calls of
    /// one second that the hub reports in two parts are one event.
    /// </summary>
    private readonly Dictionary<(string Identifier, long Second, bool Frame), JsCallGroup> _routine = [];

    private long _newestSecond;

    [JSInvokable]
    public void OnJsError(BevJsErrorPayload payload)
    {
        if (payload.Message?.Length > MaxJsErrorTextLength || payload.Stack?.Length > MaxJsErrorTextLength || payload.Source?.Length > MaxJsErrorTextLength
            || payload.Type?.Length > MaxJsErrorTypeLength)
        {
            return;
        }

        var source = payload.Type switch
        {
            "ResourceLoadError" => "resource",
            "CspViolation" => "csp",
            _ => "javascript",
        };
        switch (source)
        {
            case "resource":
                session.Browser.ResourceLoadErrors++;
                break;
            case "csp":
                session.Browser.CspViolations++;
                break;
            default:
                session.Browser.JsErrorCount++;
                break;
        }

        // The hub redacts URLs already; a stack or message from page script is redacted here like every other message,
        // and so is the error's name, which page script sets as freely as its message.
        session.Errors.Record(null, source, payload.Message ?? "JavaScript error", exceptionType: payload.Type is { } type ? BevRedactor.RedactText(type) : "Error",
            stackTrace: payload.Stack ?? (payload.Source is { } location ? session.Redactor.RedactUrl(location) : null));
    }

    /// <summary>The hub's latest view of the page's WebSockets; notable changes become timeline events.</summary>
    [JSInvokable]
    public void ReportTraffic(BevSocketStats[] sockets)
    {
        var browser = session.Browser;
        var previous = browser.Sockets;
        browser.Sockets = sockets;
        browser.SocketsReportedAt = DateTimeOffset.UtcNow;
        if (session.IsCapturing)
        {
            foreach (var socket in sockets)
            {
                var before = previous.FirstOrDefault(p => p.Id == socket.Id);
                if (before is null && socket.OpenedAt is not null)
                {
                    Record(socket, "WebSocket opened: " + socket.Url, null, BevToolsSeverity.Info);
                }

                if (socket.IsClosed && before?.IsClosed != true)
                {
                    var abnormal = socket.CloseCode is not (1000 or 1001);
                    Record(socket, "WebSocket closed: " + socket.Url, "code " + socket.CloseCode + (socket.CloseReason is { Length: > 0 } reason ? ", " + reason : ""), abnormal ? BevToolsSeverity.Warning : BevToolsSeverity.Info);
                }

                foreach (var large in socket.Largest.Where(l => l.At > _largeMessagesReportedUntil).OrderBy(l => l.At))
                {
                    // Bounded per report: a stream of large batches is one finding, not a flood of rows.
                    if (_lastLargeMessageReported++ > 20)
                    {
                        break;
                    }

                    Record(socket, "Large message " + large.Direction + " " + large.Kind + ": " + BevFmt.Bytes(large.Bytes), socket.Url, BevToolsSeverity.Warning);
                }
            }
        }

        _largeMessagesReportedUntil = Math.Max(_largeMessagesReportedUntil, sockets.SelectMany(s => s.Largest).Select(l => l.At).DefaultIfEmpty(0).Max());
        _lastLargeMessageReported = 0;
        session.Touch();
    }

    private void Record(BevSocketStats socket, string title, string? detail, BevToolsSeverity severity) => session.Timeline.Record(new BevToolsEvent
    {
        Kind = socket.Kind == "blazor-circuit" ? BevToolsEventKind.Circuit : BevToolsEventKind.Browser,
        Category = "websocket",
        Title = title,
        Detail = detail,
        Severity = severity,
        Data = new Dictionary<string, object?> { ["socketId"] = socket.Id, ["socketKind"] = socket.Kind },
    });

    /// <summary>The page's long frames and interactions since the last report, each dated by its age.</summary>
    [JSInvokable]
    public void ReportResponsiveness(BevLongFramePayload[]? frames, BevInteractionPayload[]? interactions)
    {
        if (!session.IsEnabled || !session.Options.TrackResponsiveness)
        {
            return;
        }

        BevResponsiveness.Record(session, frames, interactions);
    }

    [JSInvokable]
    public void OnTargetsChanged(BevTargetInfo[] targets)
    {
        session.Browser.Targets = targets;
        session.Ui.Touch();
    }

    /// <summary>The panel header's runtime switcher chose this runtime (true) or another one (false).</summary>
    [JSInvokable]
    public void OnShowRequested() => showRequested?.Invoke(true);

    [JSInvokable]
    public void OnHideRequested() => showRequested?.Invoke(false);

    /// <summary>One request of the protocol external frontends speak; see <see cref="BevToolsProtocol"/>.</summary>
    [JSInvokable]
    public string Protocol(string method, string? arguments) => BevToolsProtocol.Handle(session, method, arguments);

    [JSInvokable]
    public void OnBrowserEvent(string type, string? detail)
    {
        switch (type)
        {
            case "online":
                session.Browser.Online = true;
                break;
            case "offline":
                session.Browser.Online = false;
                break;
            case "visibility":
                session.Browser.Visibility = detail;
                break;
            case "enhancedload":
                session.Browser.EnhancedNavigations++;
                break;
        }

        session.Timeline.Record(new BevToolsEvent
        {
            Kind = type == "enhancedload" ? BevToolsEventKind.Navigation : BevToolsEventKind.Browser,
            Category = type == "enhancedload" ? "enhanced-navigation" : "browser",
            Title = type switch
            {
                "visibility" => "Page " + detail,
                "enhancedload" => "Enhanced navigation to " + (detail is null ? "?" : session.Redactor.RedactUrl(detail)),
                _ => "Browser " + type,
            },
            Detail = type == "enhancedload" ? "Blazor updated the page from a server-rendered response without a reload" : null,
            Severity = type == "offline" ? BevToolsSeverity.Warning : BevToolsSeverity.Info,
        });
    }

    /// <summary>
    /// One of Blazor's reconnect states. <paramref name="ageMs"/> is how long ago the browser saw it: the states of a
    /// dropped connection ("show", "retrying") reach a circuit only once it is back, together with "hide".
    /// </summary>
    [JSInvokable]
    public void OnReconnectState(string state, int attempt, int secondsToNext, double ageMs)
    {
        var age = TimeSpan.FromMilliseconds(Math.Max(0, ageMs));
        var at = DateTimeOffset.UtcNow - age;
        var browser = session.Browser;
        browser.ReconnectState = state;
        browser.ReconnectAttempt = attempt;
        browser.ReconnectStateChangedAt = at;
        if (state == "show")
        {
            browser.ReconnectCount++;
        }

        session.Timeline.Record(new BevToolsEvent
        {
            Timestamp = at,
            StartTicks = System.Diagnostics.Stopwatch.GetTimestamp() - (long)(age.TotalSeconds * System.Diagnostics.Stopwatch.Frequency),
            Kind = BevToolsEventKind.Circuit,
            Category = "reconnect",
            Title = "Reconnect " + state + (attempt > 0 ? " (attempt " + attempt + ")" : ""),
            Detail = secondsToNext > 0 ? "next attempt in " + secondsToNext + " s" : null,
            Severity = state is "failed" or "rejected" or "resume-failed" ? BevToolsSeverity.Error : state is "show" or "retrying" or "paused" ? BevToolsSeverity.Warning : BevToolsSeverity.Info,
        });
    }

    /// <summary>
    /// The calls page script made to .NET since the last report. Each is a record in the JS interop store; the timeline
    /// gets one event per name per second (a requestAnimationFrame loop calls .NET sixty times a second, which would fill
    /// the timeline in about a minute), except for a call that failed or was slow, which keeps an event of its own. The
    /// hub reports every 500 ms, so a second's calls can arrive in two reports: the event of the first is replaced by
    /// one that holds both.
    /// </summary>
    [JSInvokable]
    public void ReportJsToDotNet(BevJsCallEntry[] batch)
    {
        if (!session.IsCapturing)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var nowTicks = Stopwatch.GetTimestamp();
        IDictionary? references = null;
        var referencesRead = false;
        var calls = new List<JsCall>(batch.Length);
        foreach (var entry in batch)
        {
            session.Browser.JsToDotNetCalls++;
            var error = entry.Error is null ? null : BevRedactor.RedactText(entry.Error);
            var age = TimeSpan.FromMilliseconds(Math.Max(0, entry.AgeMs ?? (now - DateTimeOffset.FromUnixTimeMilliseconds(entry.At)).TotalMilliseconds));
            var (identifier, objectId, target) = Resolve(entry.Identifier ?? "?", ref references, ref referencesRead);
            var record = new BevJsInteropRecord
            {
                Id = session.Interop.NextId(),
                At = now - age,
                StartTicks = nowTicks - (long)(age.TotalSeconds * Stopwatch.Frequency),
                Direction = BevJsInteropDirection.JsToDotNet,
                Identifier = identifier,
                ArgumentCount = entry.Args,
                IsSync = entry.Sync,
                ComponentInstanceId = target?.ComponentInstanceId,
                ComponentName = target?.ComponentName,
            };
            session.Interop.Add(record);
            session.Interop.Complete(record, entry.DurationMs, error is null, error);
            calls.Add(new JsCall(entry, record, error, objectId, target?.TypeName));
        }

        var slowMs = session.Options.Diagnostics.SlowJsInteropMs;
        // Calls made from animation frames apart from the others: an event says which it holds, for the interop rule.
        foreach (var group in calls.GroupBy(c => (c.Record.Identifier, Second: c.Entry.At / 1000, c.Entry.Frame)))
        {
            List<JsCall>? routine = null;
            foreach (var call in group)
            {
                if (call.Error is not null || call.Entry.DurationMs >= slowMs)
                {
                    Link([call], session.Timeline.Record(Event(new JsCallGroup(call))));
                }
                else
                {
                    (routine ??= []).Add(call);
                }
            }

            if (routine is not null)
            {
                Routine(group.Key, routine);
            }
        }

        // Only the seconds a later report can still add to are kept: the hub reports every 500 ms.
        if (calls.Count > 0)
        {
            _newestSecond = Math.Max(_newestSecond, calls.Max(c => c.Entry.At / 1000));
            foreach (var key in _routine.Keys.Where(k => k.Second < _newestSecond - 2).ToList())
            {
                _routine.Remove(key);
            }

            if (_routine.Count > MaxOpenSeconds)
            {
                _routine.Clear();
            }
        }
    }

    /// <summary>The routine calls of one name in one second: added to the event an earlier report recorded for them while it is still there.</summary>
    private void Routine((string Identifier, long Second, bool Frame) key, List<JsCall> calls)
    {
        if (_routine.TryGetValue(key, out var open))
        {
            foreach (var call in calls)
            {
                open.Add(call);
            }

            if (session.Timeline.Replace(open.EventId, Event(open)))
            {
                Link(calls, open.EventId);
                return;
            }
        }

        // The first report of this second, or its event is gone (the timeline was cleared or wrapped).
        var group = new JsCallGroup(calls[0]);
        foreach (var call in calls.Skip(1))
        {
            group.Add(call);
        }

        group.EventId = session.Timeline.Record(Event(group));
        Link(calls, group.EventId);
        _routine[key] = group;
    }

    private static void Link(List<JsCall> calls, long eventId)
    {
        foreach (var call in calls)
        {
            call.Record.TimelineEventId = eventId;
        }
    }

    private static BevToolsEvent Event(JsCallGroup group)
    {
        var first = group.First;
        var on = group.Objects.Count == 0 ? "" : ", on " + (first.TargetType is null ? "" : "a " + first.TargetType + " ") + "(" + string.Join(", ", group.Objects.Take(3)) + (group.Objects.Count > 3 ? ", …" : "") + ")";
        var data = new Dictionary<string, object?> { ["callId"] = first.Record.Id, ["second"] = first.Entry.At / 1000 };
        if (first.TargetType is not null)
        {
            data["targetType"] = first.TargetType;
        }

        if (first.Entry.Frame)
        {
            data["animationFrame"] = true;
        }

        string detail;
        if (group.Count == 1)
        {
            detail = (first.Entry.Sync ? "sync" : "async") + ", " + first.Entry.Args + " args" + on + (first.Error is null ? "" : ", failed: " + first.Error);
        }
        else
        {
            data["calls"] = group.Count;
            data["totalMs"] = group.TotalMs;
            detail = group.Count + " calls in this second" + on + ", " + BevFmt.Ms(group.TotalMs) + " in all, the longest " + BevFmt.Ms(group.MaxMs);
        }

        return new BevToolsEvent
        {
            Kind = BevToolsEventKind.JsInterop,
            Category = "js-interop",
            Title = "← .NET " + first.Record.Identifier,
            Detail = detail,
            Timestamp = first.Record.At,
            StartTicks = first.Record.StartTicks,
            // The longest call, not the second the calls spread over: what the slow-call rule reads.
            DurationMs = group.MaxMs,
            Severity = first.Error is null ? BevToolsSeverity.Info : BevToolsSeverity.Error,
            // Calls on several instances of one component share a name: the event is then none of theirs.
            ComponentInstanceId = group.OneComponent ? first.Record.ComponentInstanceId : null,
            ComponentName = group.OneComponent ? first.Record.ComponentName : null,
            Data = data,
        };
    }

    private readonly record struct JsCall(BevJsCallEntry Entry, BevJsInteropRecord Record, string? Error, string? ObjectId, string? TargetType);

    /// <summary>What one timeline event says about its calls, kept as totals: a second of a busy loop holds hundreds.</summary>
    private sealed class JsCallGroup(JsCall first)
    {
        public JsCall First { get; } = first;

        public int Count { get; private set; } = 1;

        public double TotalMs { get; private set; } = first.Entry.DurationMs;

        public double MaxMs { get; private set; } = first.Entry.DurationMs;

        /// <summary>The first four objects called: three are named, and a fourth says there were more.</summary>
        public List<string> Objects { get; } = first.ObjectId is null ? [] : [first.ObjectId];

        /// <summary>Whether every call was on the first call's component instance (or none was on a component).</summary>
        public bool OneComponent { get; private set; } = true;

        public long EventId { get; set; }

        public void Add(JsCall call)
        {
            Count++;
            TotalMs += call.Entry.DurationMs;
            MaxMs = Math.Max(MaxMs, call.Entry.DurationMs);
            if (call.ObjectId is not null && Objects.Count < 4 && !Objects.Contains(call.ObjectId))
            {
                Objects.Add(call.ObjectId);
            }

            OneComponent &= call.Record.ComponentInstanceId == First.Record.ComponentInstanceId;
        }
    }

    /// <summary>
    /// "object#26.ComputeFrame" names the <see cref="DotNetObjectReference"/> JavaScript called by the id the JS runtime
    /// gave it, which says nothing; the runtime's table says what it wraps. A call on a component is that component's,
    /// any other is named by its type ("BmotionFrameLoop.ComputeFrame"), so calls on every instance of it count together.
    /// A reference disposed before the report, or a table that cannot be read, keeps the id.
    /// </summary>
    private (string Identifier, string? ObjectId, (string TypeName, long? ComponentInstanceId, string? ComponentName)? Target) Resolve(string identifier, ref IDictionary? references, ref bool referencesRead)
    {
        const string prefix = "object#";
        var dot = identifier.IndexOf('.', StringComparison.Ordinal);
        if (!identifier.StartsWith(prefix, StringComparison.Ordinal) || dot < 0
            || !long.TryParse(identifier.AsSpan(prefix.Length, dot - prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return (identifier, null, null);
        }

        if (!_resolvedObjects.TryGetValue(id, out var target))
        {
            if (!referencesRead)
            {
                references = BevJSObjectReferences.Table(session);
                referencesRead = true;
            }

            if (BevJSObjectReferences.Target(references, id) is not { } value)
            {
                return (identifier, null, null);
            }

            target = value is IComponent component && session.Components.TryGet(component, out var record)
                ? (record.DisplayName, record.InstanceId, record.DisplayName)
                : (TypeNames.Short(value.GetType()), null, null);
            if (_resolvedObjects.Count >= MaxResolvedObjects)
            {
                _resolvedObjects.Clear();
            }

            _resolvedObjects[id] = target;
        }

        return (target.TypeName + identifier[dot..], "object #" + id, target);
    }

    /// <summary>What a component picked in the page does in the in-app panel: select it.</summary>
    internal Action<long>? Picked { get; init; }

    /// <summary>The page hub's pick mode ended on an element this runtime rendered; the hub resolved its component through <see cref="Protocol"/>.</summary>
    [JSInvokable]
    public void OnComponentPicked(long instanceId) => Picked?.Invoke(instanceId);

    /// <summary>What the in-app panel does when picking ends without a component: cancelled (null), or why it failed.</summary>
    internal Action<string?>? PickEnded { get; init; }

    [JSInvokable]
    public void OnPickEnded(string? error) => PickEnded?.Invoke(error);

    [JSInvokable]
    public void OnToggleShortcut() => toggle();

    [JSInvokable]
    public void OnPaletteShortcut() => openPalette?.Invoke();
}

/// <summary>What the page hub says after outlining a component.</summary>
internal sealed class BevHighlightResult
{
    public bool Ok { get; set; }

    /// <summary>False when the component renders nothing with a size: no element, or only empty ones.</summary>
    public bool Visible { get; set; }

    public string? Error { get; set; }
}
