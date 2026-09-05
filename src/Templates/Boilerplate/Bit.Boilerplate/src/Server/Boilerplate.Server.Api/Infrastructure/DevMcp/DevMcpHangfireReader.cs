using System.Net;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

internal static class DevMcpHangfireReader
{
    public static readonly string[] AllStates = ["succeeded", "failed", "scheduled", "processing", "enqueued", "deleted"];

    public static IEnumerable<HangfireJobRow> ReadJobs(IMonitoringApi monitoring, string state, string? queue)
    {
        var scan = DevMcpLimits.HangfireFilterScanCap;
        return state.Trim().ToLowerInvariant() switch
        {
            "succeeded" => monitoring.SucceededJobs(0, scan).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, pair.Value.SucceededAt, null, "succeeded")),
            "failed" => monitoring.FailedJobs(0, scan).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, pair.Value.FailedAt, pair.Value.ExceptionMessage, "failed")),
            "scheduled" => monitoring.ScheduledJobs(0, scan).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, pair.Value.ScheduledAt, null, "scheduled")),
            "processing" => monitoring.ProcessingJobs(0, scan).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, pair.Value.StartedAt, null, "processing")),
            "deleted" => monitoring.DeletedJobs(0, scan).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, pair.Value.DeletedAt, null, "deleted")),
            "enqueued" => monitoring.EnqueuedJobs(string.IsNullOrWhiteSpace(queue) ? "default" : queue, 0, scan)
                .Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, pair.Value.EnqueuedAt, null, "enqueued")),
            "any" => AllStates.SelectMany(one => ReadJobs(monitoring, one, queue)),
            _ => throw new InvalidOperationException("State must be succeeded, failed, scheduled, processing, enqueued, deleted or any.")
        };
    }

    public static bool Matches(HangfireJobRow job, string? argumentContains, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        if (fromUtc is not null && (job.At is null || job.At < fromUtc.Value.UtcDateTime))
            return false;
        if (toUtc is not null && (job.At is null || job.At > toUtc.Value.UtcDateTime))
            return false;
        if (string.IsNullOrWhiteSpace(argumentContains))
            return true;
        if (job.Job?.Args is null)
            return false;
        return job.Job.Args.Select(FormatArgument).Any(argument => argument.Contains(argumentContains, StringComparison.OrdinalIgnoreCase));
    }

    public static string FormatArgument(object? argument)
    {
        if (argument is null)
            return "null";
        if (argument is string text)
            return WebUtility.HtmlDecode(text);
        if (argument is CancellationToken)
            return "<CancellationToken>";
        if (argument.GetType().Name is "PerformContext")
            return "<PerformContext>";
        try
        {
            return DevMcpJson.Serialize(argument);
        }
        catch
        {
            return argument.ToString() ?? argument.GetType().Name;
        }
    }

    public sealed record HangfireJobRow(string Id, Hangfire.Common.Job? Job, DateTime? At, string? Exception, string State);
}
