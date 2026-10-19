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
}

/// <summary>Receives browser-side reports from bit-bevtools.js. One instance per BevTools panel (per session).</summary>
public sealed class BevJsBridge(BevToolsSession session, Action toggle, Action? openPalette = null)
{
    [JSInvokable]
    public void OnJsError(BevJsErrorPayload payload)
    {
        session.Browser.JsErrorCount++;
        session.Errors.Record(null, "javascript", payload.Message ?? "JavaScript error", exceptionType: payload.Type ?? "Error", stackTrace: payload.Stack ?? payload.Source);
    }

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
        }

        session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Browser,
            Category = "browser",
            Title = type == "visibility" ? "Page " + detail : "Browser " + type,
            Severity = type == "offline" ? BevToolsSeverity.Warning : BevToolsSeverity.Info,
        });
    }

    [JSInvokable]
    public void OnReconnectState(string state, int attempt, int secondsToNext)
    {
        var browser = session.Browser;
        browser.ReconnectState = state;
        browser.ReconnectAttempt = attempt;
        browser.ReconnectStateChangedAt = DateTimeOffset.UtcNow;
        if (state == "show")
        {
            browser.ReconnectCount++;
        }

        session.Timeline.Record(new BevToolsEvent
        {
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

    [JSInvokable]
    public void OnToggleShortcut() => toggle();

    [JSInvokable]
    public void OnPaletteShortcut() => openPalette?.Invoke();
}
