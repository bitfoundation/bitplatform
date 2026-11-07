using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

[Authorize(Policy = AppFeatures.System.DevMcp)]
public partial class DevMcpTelemetryTools
{
    private const int MaxTraceSpans = 200;
    private const int MaxTraceLogs = 200;

    private const string SamplingNote = "Outside Development the server sends the dashboard about 5% of its ordinary logs and fast successful spans, and every unexpected error and every span slower than a second, though not always the rest of its trace, so a missing line proves nothing. The dashboard keeps 100,000 logs and 100,000 traces at most and drops the oldest first.";

    [AutoInject] private DevMcpTelemetryReader reader = default!;
    [AutoInject] private ServerApiSettings settings = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private ILogger<DevMcpTelemetryTools> logger = default!;

    private Guid CallerId => httpContextAccessor.HttpContext!.User.GetUserId();

    [McpServerTool(Name = nameof(ListLogs))]
    [Description("Lists the newest structured logs that the Aspire dashboard running next to this deployment keeps, newest first. The apps of the Docker Compose project send it their logs, traces and metrics over OTLP, and it keeps them across restarts and deployments. Each entry has its time, level, service, category, message, trace and span ids, attributes and exception. Filters combine: resource (one service name), traceId, severity (that level and above) and search. " + SamplingNote + " Values are returned verbatim and are not redacted: a log line carries whatever the code logged, which is why the whole endpoint is global-admin only and every call is logged. Answers that no dashboard is configured when AspireDashboard:Url is empty, as it is outside a Docker Compose deployment. Read-only.")]
    public async Task<string> ListLogs(
        [Description("One service name, such as Boilerplate.Server.Web or Boilerplate.Server.Api; every instance of it, across restarts, is read")] string? resource = null,
        [Description("Only the logs of this trace: its id, or the start of it")] string? traceId = null,
        [Description("The lowest level to return: Trace, Debug, Information, Warning, Error or Critical")] string? severity = null,
        [Description("Text matched against the message, category and attribute values. Qualifiers such as category:Orders or message:failed narrow it to one field")] string? search = null,
        [Description("How many of the newest matching logs, capped at 100")] int take = DevMcpLimits.DefaultTake,
        CancellationToken cancellationToken = default)
    {
        var level = DevMcpTelemetryReader.Severities.FirstOrDefault(name => name.Equals(severity?.Trim(), StringComparison.OrdinalIgnoreCase));

        var invalid = Invalid(resource, traceId, search)
            ?? (severity is not null && level is null ? $"severity is one of: {string.Join(", ", DevMcpTelemetryReader.Severities)}." : null);
        if (invalid is not null)
            return Error(invalid);

        take = Math.Clamp(take, 1, DevMcpLimits.MaxTake);

        return await Read(async () =>
        {
            var answer = await reader.Get($"api/telemetry/logs?limit={take}{Query("traceId", traceId?.ToLowerInvariant())}{Query("severity", level)}{Query("search", Search(resource, search))}", cancellationToken);
            if (answer is null)
                return Error("The Aspire dashboard has no telemetry API: Dashboard:Api:Disabled is on.");

            var (logs, truncated) = DevMcpTelemetryReader.Fit(DevMcpTelemetryReader.ReadLogs(answer["data"]).OrderByDescending(log => log.Timestamp), DevMcpLimits.MaxPayloadBytes);

            logger.LogInformation("Dev MCP read {Count} logs from the Aspire dashboard for {UserId}. Resource: {Resource}. TraceId: {TraceId}. Severity: {Severity}. Search: {Search}.",
                logs.Length, CallerId, resource, traceId, level, search);

            return DevMcpJson.Serialize(new
            {
                TotalCount = answer["totalCount"]?.GetValue<int>(),
                Count = logs.Length,
                Truncated = truncated ? true : (bool?)null,
                KnownServices = logs.Length is 0 && resource is not null ? await reader.GetServiceNames(cancellationToken) : null,
                Logs = logs,
                Note = SamplingNote
            });
        }, cancellationToken);
    }

    [McpServerTool(Name = nameof(ListTraces))]
    [Description("Lists the newest traces that the Aspire dashboard running next to this deployment keeps, newest first, one entry each: the root span's name, the services the trace crossed, its start, duration and span count, and whether a span failed, with the failed spans' names. hasError=true keeps the failed traces only. Pass a trace id from here, from an error or from the diagnostic modal to GetTrace for its spans and logs. " + SamplingNote + " Answers that no dashboard is configured when AspireDashboard:Url is empty, as it is outside a Docker Compose deployment. Read-only.")]
    public async Task<string> ListTraces(
        [Description("One service name, such as Boilerplate.Server.Web or Boilerplate.Server.Api; every instance of it, across restarts, is read")] string? resource = null,
        [Description("true for the traces with a failed span only, false for those without one")] bool? hasError = null,
        [Description("Text matched against span names and attribute values. Qualifiers such as name:Order or status:Error narrow it to one field")] string? search = null,
        [Description("How many of the newest matching traces, capped at 100")] int take = DevMcpLimits.DefaultTake,
        CancellationToken cancellationToken = default)
    {
        if (Invalid(resource, traceId: null, search) is { } invalid)
            return Error(invalid);

        take = Math.Clamp(take, 1, DevMcpLimits.MaxTake);

        return await Read(async () =>
        {
            var answer = await reader.Get($"api/telemetry/traces?limit={take}{Query("hasError", hasError?.ToString().ToLowerInvariant())}{Query("search", Search(resource, search))}", cancellationToken);
            if (answer is null)
                return Error("The Aspire dashboard has no telemetry API: Dashboard:Api:Disabled is on.");

            var (traces, truncated) = DevMcpTelemetryReader.Fit(DevMcpTelemetryReader.SummarizeTraces(DevMcpTelemetryReader.ReadSpans(answer["data"])), DevMcpLimits.MaxPayloadBytes);

            logger.LogInformation("Dev MCP read {Count} traces from the Aspire dashboard for {UserId}. Resource: {Resource}. HasError: {HasError}. Search: {Search}.",
                traces.Length, CallerId, resource, hasError, search);

            return DevMcpJson.Serialize(new
            {
                TotalCount = answer["totalCount"]?.GetValue<int>(),
                Count = traces.Length,
                Truncated = truncated ? true : (bool?)null,
                KnownServices = traces.Length is 0 && resource is not null ? await reader.GetServiceNames(cancellationToken) : null,
                Traces = traces,
                Note = SamplingNote
            });
        }, cancellationToken);
    }

    [McpServerTool(Name = nameof(GetTrace))]
    [Description("Returns one trace that the Aspire dashboard running next to this deployment keeps: every span in start order, with its service, name, kind, parent, start, duration, status, attributes and events, and the logs written inside the trace, oldest first, as ListLogs returns them. The start of a trace id is enough when no other trace shares it. Spans and logs are capped at 200 each. Returns not-found when the dashboard never received the trace, because sampling left it out, or has dropped it. Values are returned verbatim and are not redacted, which is why the whole endpoint is global-admin only and every call is logged. Read-only.")]
    public async Task<string> GetTrace(
        [Required, Description("The trace id, or the start of it")] string traceId,
        CancellationToken cancellationToken = default)
    {
        if (Invalid(resource: null, traceId, search: null) is { } invalid)
            return Error(invalid);

        return await Read(async () =>
        {
            var requestedId = traceId.Trim().ToLowerInvariant();
            var trace = await reader.Get($"api/telemetry/traces/{requestedId}", cancellationToken);
            if (trace is null)
                return DevMcpJson.Serialize(new { Found = false, TraceId = requestedId });

            var spans = DevMcpTelemetryReader.ReadSpans(trace["data"]).OrderBy(span => span.Start).ToArray();
            var id = spans.FirstOrDefault()?.TraceId ?? requestedId;
            var start = spans.Min(span => span.Start);
            var end = spans.Max(span => span.End);

            var logsAnswer = await reader.Get($"api/telemetry/logs?limit={MaxTraceLogs}{Query("traceId", id)}", cancellationToken);
            var logs = DevMcpTelemetryReader.ReadLogs(logsAnswer?["data"]).OrderBy(log => log.Timestamp).ToArray();
            var logCount = Math.Max(logsAnswer?["totalCount"]?.GetValue<int>() ?? 0, logs.Length);

            var (spanItems, _) = DevMcpTelemetryReader.Fit(spans.Take(MaxTraceSpans).Select(span => new
            {
                span.SpanId,
                span.ParentSpanId,
                span.Name,
                span.Kind,
                Service = span.Resource,
                span.Scope,
                span.Start,
                span.DurationMs,
                span.Status,
                span.StatusMessage,
                span.Attributes,
                Events = span.Events.Length is 0 ? null : span.Events
            }), DevMcpLimits.MaxPayloadBytes / 2);
            var (logItems, _) = DevMcpTelemetryReader.Fit(logs, DevMcpLimits.MaxPayloadBytes / 2);

            logger.LogInformation("Dev MCP read trace {TraceId} from the Aspire dashboard for {UserId}.", id, CallerId);

            return DevMcpJson.Serialize(new
            {
                Found = true,
                TraceId = id,
                Start = start,
                DurationMs = start is null || end is null ? (double?)null : (end.Value - start.Value).TotalMilliseconds,
                SpanCount = spans.Length,
                Spans = spanItems,
                SpansOmitted = spans.Length > spanItems.Length ? spans.Length - spanItems.Length : (int?)null,
                Logs = logItems,
                LogsOmitted = logCount > logItems.Length ? logCount - logItems.Length : (int?)null
            });
        }, cancellationToken);
    }

    private async Task<string> Read(Func<Task<string>> read, CancellationToken cancellationToken)
    {
        if (reader.Configured is false)
            return Error("No Aspire dashboard is configured here: AspireDashboard:Url is empty. The app host's Docker Compose publish sets it, with the dashboard's key.");

        try
        {
            return await read();
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested is false)
        {
            logger.LogWarning(exception, "Dev MCP could not read the Aspire dashboard at {Url}.", settings.AspireDashboard?.Url);
            return Error(exception is InvalidOperationException ? exception.Message : $"The Aspire dashboard at {settings.AspireDashboard?.Url} could not be read: {exception.Message}");
        }
    }

    private static string? Invalid(string? resource, string? traceId, string? search)
    {
        if (resource is not null && ServiceName().IsMatch(resource.Trim()) is false)
            return "resource is one service name, such as Boilerplate.Server.Web, with no spaces or colons.";
        if (traceId is not null && TraceId().IsMatch(traceId.Trim()) is false)
            return "traceId is up to 32 hexadecimal characters.";
        if (search?.Length > DevMcpLimits.MaxFilterLength)
            return $"search is at most {DevMcpLimits.MaxFilterLength} characters.";
        return null;
    }

    private static string? Search(string? resource, string? search)
    {
        return string.Join(' ', new[] { resource is null ? null : $"resource:{resource.Trim()}", search?.Trim() }.Where(part => string.IsNullOrEmpty(part) is false)) is { Length: > 0 } text ? text : null;
    }

    private static string Query(string name, string? value) => string.IsNullOrWhiteSpace(value) ? "" : $"&{name}={Uri.EscapeDataString(value.Trim())}";

    private static string Error(string message) => DevMcpJson.Serialize(new { Error = message });

    [GeneratedRegex("^[A-Za-z0-9._-]{1,200}$")]
    private static partial Regex ServiceName();

    [GeneratedRegex("^[0-9A-Fa-f]{1,32}$")]
    private static partial Regex TraceId();
}
