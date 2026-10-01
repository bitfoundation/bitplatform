using System.Net;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

internal static class DevMcpHangfireReader
{
    public static readonly string[] AllStates = ["succeeded", "failed", "scheduled", "processing", "enqueued", "deleted"];

    /// <summary>
    /// One page of one state, through the storage's own paging - so a caller that needs no scan reads its page and
    /// nothing more. Reading a page costs the deserialization of every job in it, which is why the count matters.
    /// </summary>
    public static IEnumerable<HangfireJobRow> ReadJobs(IMonitoringApi monitoring, string state, string? queue, int from, int count)
    {
        return state.Trim().ToLowerInvariant() switch
        {
            "succeeded" => monitoring.SucceededJobs(from, count).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, Utc(pair.Value.SucceededAt?.Ticks), null, "succeeded")),
            "failed" => monitoring.FailedJobs(from, count).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, Utc(pair.Value.FailedAt?.Ticks), pair.Value.ExceptionMessage, "failed")),
            "scheduled" => monitoring.ScheduledJobs(from, count).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, Utc(pair.Value.ScheduledAt?.Ticks), null, "scheduled")),
            "processing" => monitoring.ProcessingJobs(from, count).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, Utc(pair.Value.StartedAt?.Ticks), null, "processing")),
            "deleted" => monitoring.DeletedJobs(from, count).Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, Utc(pair.Value.DeletedAt?.Ticks), null, "deleted")),
            "enqueued" => monitoring.EnqueuedJobs(string.IsNullOrWhiteSpace(queue) ? "default" : queue, from, count)
                .Select(pair => new HangfireJobRow(pair.Key, pair.Value.Job, Utc(pair.Value.EnqueuedAt?.Ticks), null, "enqueued")),
            _ => throw new InvalidOperationException("State must be succeeded, failed, scheduled, processing, enqueued, deleted or any.")
        };
    }

    public static bool IsAnyState(string state) => state.Trim().Equals("any", StringComparison.OrdinalIgnoreCase);

    /// <summary>The lists a state names: six for <c>any</c>, which is the only case that has to merge more than one.</summary>
    public static IReadOnlyList<string> StatesOf(string state) => IsAnyState(state) ? AllStates : [state];

    public static bool Matches(HangfireJobRow job, string? argumentContains, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        if (fromUtc is not null && (job.At is null || job.At < fromUtc))
            return false;
        if (toUtc is not null && (job.At is null || job.At > toUtc))
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

    /// <summary>
    /// Hangfire keeps every timestamp in UTC, but storages disagree on the kind they hand it back with: EF Core's (SQLite)
    /// says Unspecified, which an implicit conversion to <see cref="DateTimeOffset"/> would read as local time.
    /// </summary>
    private static DateTimeOffset? Utc(long? ticks) => ticks is null ? null : new DateTimeOffset(ticks.Value, TimeSpan.Zero);

    public sealed record HangfireJobRow(string Id, Hangfire.Common.Job? Job, DateTimeOffset? At, string? Exception, string State);
}
