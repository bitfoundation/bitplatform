using System.Globalization;

namespace Bit.Belemetry;

/// <summary>
/// Builds a capture's issue report from what its export holds. Observed statements read the evidence directly (the
/// exception, a request's status, a trace shared with a component's event); inferred ones explain it (closeness in
/// time, a component named by a stack frame) and say so.
/// </summary>
internal static class BelemetryIssueReportBuilder
{
    // How close in time an unrelated failure has to be to be offered as a possible cause.
    private static readonly long _nearby = TimeSpan.FromSeconds(5).Ticks * 100;

    public static BelemetryIssueReport Build(BelemetryDiagnosticCapture capture, BelemetryCaptureContent content)
    {
        var events = capture.Events;
        var subject = capture.Subject;
        var exception = subject?.Exception;
        var release = capture.Release;
        var findings = new List<BelemetryIssueFinding>();
        var group = exception is null ? null : capture.ExceptionGroups.FirstOrDefault(g => g.Fingerprint == exception.Fingerprint);
        var subjectTime = subject?.TimeUnixNano ?? long.MaxValue;

        // The route: the subject's own, else the last navigation before it.
        var lastNavigation = events.LastOrDefault(e => e.Category == BelemetryDiagnosticCategories.Navigation && e.Route is not null && e.TimeUnixNano <= subjectTime);
        var route = subject?.Route ?? lastNavigation?.Route;
        var routeObserved = (subject?.Route is not null ? subject.RouteSource : lastNavigation?.RouteSource) == "framework";

        // The UI event whose trace the failure happened in: observed, not inferred.
        var handler = subject?.TraceId is { } trace
            ? events.LastOrDefault(e => e.Category == BelemetryDiagnosticCategories.Component && e.Name == "handle_event" && e.TraceId == trace)
            : null;

        string? action = null;
        if (handler is not null)
        {
            // Every attribute through the capture: a key the user redacted in the preview is redacted here too.
            var method = capture.Attribute(handler, BelemetryDiagnosticEventFactory.MethodTag);
            var attribute = capture.Attribute(handler, BelemetryDiagnosticEventFactory.AttributeNameTag);
            action = $"{attribute ?? "event"} on {handler.ComponentType}{(method is null ? string.Empty : "." + method)}";
            findings.Add(new(BelemetryIssueFindingKind.Observed, $"It happened while {handler.ComponentType} handled {attribute ?? "a UI event"}{(method is null ? string.Empty : $" in {method}")} (same trace {handler.TraceId}).", [Id(subject!), Id(handler)]));
        }
        else if (subject is not null && events.LastOrDefault(e => e.Category == BelemetryDiagnosticCategories.Browser && e.Name == "interaction" && e.TimeUnixNano <= subjectTime && subjectTime - e.TimeUnixNano <= _nearby) is { } interaction)
        {
            var type = capture.Attribute(interaction, "event.type") ?? "interaction";
            var element = capture.Attribute(interaction, "target.element");
            action = $"{type}{(element is null ? string.Empty : " on " + element)}";
            findings.Add(new(BelemetryIssueFindingKind.Inferred, $"The user's last {type}{(element is null ? string.Empty : $" on a {element}")} came {Ms(subjectTime - interaction.TimeUnixNano)} before the failure; it may have triggered it.", [Id(interaction)]));
        }

        if (exception is not null)
        {
            findings.Insert(0, new(BelemetryIssueFindingKind.Observed,
                $"{exception.Type} was {OriginText(exception.Origin)}{(route is null ? string.Empty : $" on {route}")}{(release?.ServiceVersion is null ? string.Empty : $" in release {release.ServiceVersion}")}{(release?.Environment is null ? string.Empty : $" ({release.Environment})")}.",
                [Id(subject!)]));

            if (group is not null && group.Count > 1)
            {
                findings.Add(new(BelemetryIssueFindingKind.Observed,
                    $"This failure (fingerprint {group.Fingerprint}) happened {group.Count.ToString(CultureInfo.InvariantCulture)} times between {group.FirstSeen.ToUniversalTime():u} and {group.LastSeen.ToUniversalTime():u}{(group.Releases.Count > 0 ? $", in releases {string.Join(", ", group.Releases)}" : string.Empty)}.", []));
            }

            if (subject!.ComponentSource == "stack" && subject.ComponentType is { } inferred && inferred != handler?.ComponentType)
            {
                findings.Add(new(BelemetryIssueFindingKind.Inferred, $"The first application frame ({exception.Anchor}) suggests it was thrown in {inferred}.", [Id(subject)]));
            }

            if (route is not null && routeObserved is false)
            {
                findings.Add(new(BelemetryIssueFindingKind.Inferred, $"The route {route} was templated from the page's URL (values replaced), not reported by the router.", lastNavigation is null ? [] : [Id(lastNavigation)]));
            }

            foreach (var inner in exception.InnerExceptions)
            {
                findings.Add(new(BelemetryIssueFindingKind.Observed, $"Its inner exception is {inner.Type}{(inner.Message is null ? string.Empty : $": {inner.Message}")}.", [Id(subject)]));
            }
        }

        // Failed requests: in the subject's trace (observed), or just before it (a possible cause, inferred).
        // The page's fetch span and the server's span of that request - its child - are one failed request, told once,
        // from the caller's side (it has the URL and the duration the user waited). Other requests of one trace (a
        // handler's two calls) are told apart.
        var failedHttp = events.Where(e => e.Category == BelemetryDiagnosticCategories.Http && e.Outcome == BelemetryDiagnosticOutcome.Failure).ToList();
        var browserSpans = failedHttp.Where(e => e.Source == BelemetryDiagnosticSource.Browser && e.TraceId is not null && e.SpanId is not null)
            .Select(e => (e.TraceId!, e.SpanId!)).ToHashSet();
        var failedRequests = failedHttp
            .Where(e => e.Source == BelemetryDiagnosticSource.Browser || e.TraceId is null || e.ParentSpanId is null || browserSpans.Contains((e.TraceId, e.ParentSpanId)) is false)
            .GroupBy(e => e.TraceId is not null && e.SpanId is not null ? e.TraceId + "/" + e.SpanId : BelemetryDiagnosticJson.EventId(e), StringComparer.Ordinal)
            .Select(g => g.OrderBy(e => e.Source == BelemetryDiagnosticSource.Browser ? 0 : 1).ThenBy(e => e.TimeUnixNano).First())
            .OrderBy(e => e.TimeUnixNano)
            .ToList();
        foreach (var request in failedRequests)
        {
            if (subject is null) break;
            if (request.TraceId is not null && request.TraceId == subject.TraceId)
            {
                findings.Add(new(BelemetryIssueFindingKind.Observed, $"The request {RequestText(capture, request)} failed in the same trace.", [Id(request), Id(subject)]));
            }
            else if (request.TimeUnixNano <= subjectTime && subjectTime - request.TimeUnixNano <= _nearby)
            {
                findings.Add(new(BelemetryIssueFindingKind.Inferred, $"The request {RequestText(capture, request)} failed {Ms(subjectTime - request.TimeUnixNano)} before the failure; it may be the cause.", [Id(request)]));
            }
        }

        var circuit = events.Where(e => e.Category == BelemetryDiagnosticCategories.Circuit).ToList();
        foreach (var transition in circuit.Where(e => e.Outcome == BelemetryDiagnosticOutcome.Failure || e.Name.StartsWith("reconnect", StringComparison.Ordinal)))
        {
            if (subject is null || Math.Abs(subjectTime - transition.TimeUnixNano) > _nearby * 2) continue;
            findings.Add(new(BelemetryIssueFindingKind.Inferred, $"The connection to the server changed ({transition.Name}) around the failure; it may be related.", [Id(transition)]));
        }

        var browserErrors = events.Where(e => e.Source == BelemetryDiagnosticSource.Browser && e.Exception is not null && e != subject).ToList();
        if (browserErrors.Count > 0)
        {
            findings.Add(new(BelemetryIssueFindingKind.Observed, $"The page reported {browserErrors.Count.ToString(CultureInfo.InvariantCulture)} JavaScript error(s), the last a {browserErrors[^1].Exception!.Type}.", browserErrors.TakeLast(3).Select(Id).ToList()));
        }

        if (capture.Description is { } description)
        {
            findings.Add(new(BelemetryIssueFindingKind.UserProvided, description, []));
        }

        var traceIds = new List<string>();
        void AddTrace(string? id)
        {
            if (id is { Length: 32 } && traceIds.Contains(id) is false && traceIds.Count < 20) traceIds.Add(id);
        }
        AddTrace(subject?.TraceId);
        foreach (var e in failedRequests) AddTrace(e.TraceId);
        foreach (var e in events.Where(e => e.IsFailure)) AddTrace(e.TraceId);

        var service = release?.ServiceName ?? content.Release?.ServiceName ?? "the app";
        var where = handler?.ComponentType ?? subject?.ComponentType ?? route;
        var title = exception is not null
            ? $"{ShortType(exception.Type)}{(where is null ? string.Empty : " in " + ShortType(where))}"
            : $"{(string.IsNullOrEmpty(capture.Trigger.Reason) ? "Diagnostic capture" : capture.Trigger.Reason)} - {service}";

        var summary = exception is not null
            ? $"{exception.Type}{(exception.Message is null ? string.Empty : $": {exception.Message}")}. {failedRequests.Count.ToString(CultureInfo.InvariantCulture)} failed request(s), {events.Count(e => e.IsFailure).ToString(CultureInfo.InvariantCulture)} failure event(s) and {events.Count.ToString(CultureInfo.InvariantCulture)} event(s) in total were captured."
            : $"No exception is the subject of this capture. {events.Count(e => e.IsFailure).ToString(CultureInfo.InvariantCulture)} failure event(s) and {events.Count.ToString(CultureInfo.InvariantCulture)} event(s) in total were captured.";

        return new BelemetryIssueReport
        {
            Title = title,
            Summary = summary,
            CaptureId = capture.Id,
            CreatedAt = capture.CreatedAt,
            Trigger = capture.Trigger.Reason ?? (capture.Trigger.Kind == BelemetryCaptureTriggerKind.Manual ? "manual" : "automatic"),
            ExceptionType = exception?.Type,
            ExceptionMessage = exception?.Message,
            Fingerprint = exception?.Fingerprint,
            ExceptionOrigin = exception?.Origin,
            Occurrences = group?.Count,
            Service = release?.ServiceName,
            Release = release?.ServiceVersion,
            Environment = release?.Environment,
            Commit = release?.Commit,
            Runtime = release is null ? null : $"{release.Host}, {release.Runtime}",
            Route = route,
            TriggeringAction = action,
            Navigation = events.Where(e => e.Category == BelemetryDiagnosticCategories.Navigation && e.Route is not null).TakeLast(20)
                               .Select(e => Step(e, e.Route! + (e.RouteSource == "framework" ? string.Empty : " (inferred)"))).ToList(),
            FailedRequests = failedRequests.TakeLast(20).Select(e => Step(e, RequestText(capture, e))).ToList(),
            InteropFailures = events.Where(e => e.Category == BelemetryDiagnosticCategories.JsInterop).TakeLast(20)
                                    .Select(e => Step(e, e.Exception is { } x ? $"{x.Type}{(x.Message is null ? string.Empty : ": " + x.Message)}" : e.Message ?? e.Name)).ToList(),
            CircuitTransitions = circuit.TakeLast(20).Select(e => Step(e, e.Name + (e.Message is null ? string.Empty : ": " + e.Message))).ToList(),
            Components = capture.Components.Select(c => $"{c.Type} ({c.Association})").ToList(),
            TraceIds = traceIds,
            Prerequisites = capture.Snapshots.Where(s => s.Status == BelemetrySnapshotStatus.Captured)
                                   .Select(s => $"Snapshot {s.Name}{(s.Label is null ? string.Empty : $" ({s.Label})")}: {s.Data?.Count ?? 0} values").ToList(),
            MissingEvidence = capture.MissingEvidence,
            Findings = findings,
            UserDescription = capture.Description,
        };
    }

    private static BelemetryIssueStep Step(BelemetryDiagnosticEvent e, string text) => new(BelemetryTime.ToDateTime(e.TimeUnixNano), text, e.TraceId, Id(e));

    private static string Id(BelemetryDiagnosticEvent e) => BelemetryDiagnosticJson.EventId(e);

    private static string RequestText(BelemetryDiagnosticCapture capture, BelemetryDiagnosticEvent e)
    {
        var url = capture.Attribute(e, "url.template") ?? e.Route;
        var status = capture.Attribute(e, BelemetrySemanticConventions.HttpResponseStatusCode);
        var error = capture.Attribute(e, BelemetrySemanticConventions.ErrorType);
        return $"{e.Name} {url ?? "(url unknown)"} {status ?? error ?? "failed"}{(e.DurationMilliseconds is { } d ? $" ({d.ToString("0", CultureInfo.InvariantCulture)} ms)" : string.Empty)}";
    }

    private static string OriginText(string origin) => origin switch
    {
        "unhandled" => "not handled (the renderer, the circuit or the runtime reported it)",
        "error-boundary" => "caught by an error boundary",
        "js-interop" => "thrown by a JS interop call",
        "circuit" => "reported by the circuit",
        "browser" => "thrown in the page (JavaScript)",
        "tracked" => "handled and reported by the app",
        _ => "logged",
    };

    private static string Ms(long nanoseconds) => $"{(nanoseconds / 1_000_000d).ToString("0", CultureInfo.InvariantCulture)} ms";

    private static string ShortType(string type)
    {
        var dot = type.LastIndexOf('.');
        return dot >= 0 && dot < type.Length - 1 ? type[(dot + 1)..] : type;
    }
}
