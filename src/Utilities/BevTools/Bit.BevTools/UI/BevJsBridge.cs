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
}

/// <summary>
/// Receives browser-side reports from bit-bevtools.js, and answers the protocol requests of external frontends (the
/// companion window, the browser extension). One instance per BevTools panel (per session).
/// </summary>
public sealed class BevJsBridge(BevToolsSession session, Action toggle, Action? openPalette = null, Action<bool>? showRequested = null)
{
    private int _lastLargeMessageReported;
    private double _largeMessagesReportedUntil;

    [JSInvokable]
    public void OnJsError(BevJsErrorPayload payload)
    {
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

        // The hub redacts URLs already; a stack or message from page script is redacted here like every other message.
        session.Errors.Record(null, source, payload.Message ?? "JavaScript error", exceptionType: payload.Type ?? "Error",
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

    [JSInvokable]
    public void ReportJsToDotNet(BevJsCallEntry[] batch)
    {
        if (!session.IsCapturing)
        {
            return;
        }

        foreach (var entry in batch)
        {
            session.Browser.JsToDotNetCalls++;
            var record = new BevJsInteropRecord
            {
                Id = session.Interop.NextId(),
                At = DateTimeOffset.FromUnixTimeMilliseconds(entry.At),
                Direction = BevJsInteropDirection.JsToDotNet,
                Identifier = entry.Identifier ?? "?",
                ArgumentCount = entry.Args,
                IsSync = entry.Sync,
            };
            record.TimelineEventId = session.Timeline.Record(new BevToolsEvent
            {
                Kind = BevToolsEventKind.JsInterop,
                Category = "js-interop",
                Title = "← .NET " + record.Identifier,
                Detail = (entry.Sync ? "sync" : "async") + ", " + entry.Args + " args" + (entry.Error is null ? "" : ", failed: " + entry.Error),
                Timestamp = record.At,
                DurationMs = entry.DurationMs,
                Severity = entry.Error is null ? BevToolsSeverity.Info : BevToolsSeverity.Error,
                Data = new Dictionary<string, object?> { ["callId"] = record.Id },
            });
            session.Interop.Add(record);
            session.Interop.Complete(record, entry.DurationMs, entry.Error is null, entry.Error);
        }
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
