using System.ComponentModel;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using ModelContextProtocol.Server;

namespace Boilerplate.Server.Api.Infrastructure.DevMcp;

[Authorize(Policy = AppFeatures.System.DevMcp)]
public sealed class DevMcpHangfireTools(JobStorage jobStorage, ServerApiSettings settings)
{
    [McpServerTool(Name = nameof(GetHangfireStats))]
    [Description("Returns Hangfire job counts by state via JobStorage.GetMonitoringApi, not by querying Hangfire tables. Correct whether this deployment stores jobs in the shared database (jobs schema) or in an isolated SQLite file (Hangfire.UseIsolatedStorage). Recurring-job count is included. This is read-only: it cannot enqueue, retry, delete or trigger jobs.")]
    public string GetHangfireStats()
    {
        var monitoring = jobStorage.GetMonitoringApi();
        var stats = monitoring.GetStatistics();
        var queues = monitoring.Queues().Select(queue => new
        {
            queue.Name,
            queue.Length,
            Fetched = queue.Fetched ?? 0
        });

        return DevMcpJson.Serialize(new
        {
            stats.Enqueued,
            stats.Failed,
            stats.Processing,
            stats.Scheduled,
            stats.Succeeded,
            stats.Deleted,
            stats.Recurring,
            stats.Servers,
            stats.Retries,
            Queues = queues,
            IsolatedStorage = settings.Hangfire?.UseIsolatedStorage is true,
            JobExpiration = settings.Hangfire?.JobExpiration.ToString()
        });
    }

    [McpServerTool(Name = nameof(ListHangfireJobs))]
    [Description("Lists Hangfire jobs in one state with paging, via IMonitoringApi. State must be one of: succeeded, failed, scheduled, processing, enqueued, deleted, any. Optional argumentContains matches any argument as text. Optional fromUtc/toUtc filter on the state's timestamp. A succeeded job disappears after Hangfire.JobExpiration (see GetEffectiveConfiguration). Failed jobs do not expire. Does not enqueue, retry, delete or trigger jobs. Default queue is used when listing enqueued jobs if queue is omitted.")]
    public string ListHangfireJobs(
        [Required, Description("succeeded | failed | scheduled | processing | enqueued | deleted | any")] string state,
        [Description("0-based offset")] int from = 0,
        [Description("Page size, capped at 50")] int take = 25,
        [Description("Substring matched against every job argument")] string? argumentContains = null,
        [Description("Inclusive lower bound on the state's timestamp, UTC")] DateTimeOffset? fromUtc = null,
        [Description("Inclusive upper bound on the state's timestamp, UTC")] DateTimeOffset? toUtc = null,
        [Description("Hangfire queue name; only used for enqueued")] string? queue = null)
    {
        take = Math.Clamp(take, 1, DevMcpLimits.HangfireMaxTake);
        from = Math.Max(from, 0);

        try
        {
            var monitoring = jobStorage.GetMonitoringApi();
            var jobs = DevMcpHangfireReader.ReadJobs(monitoring, state, queue)
                .Where(job => DevMcpHangfireReader.Matches(job, argumentContains, fromUtc, toUtc))
                .OrderByDescending(job => job.At ?? DateTime.MinValue)
                // state "any" reads six snapshots, so a job caught mid-transition is in two of them; the newest wins.
                .DistinctBy(job => job.Id)
                .Skip(from)
                .Take(take)
                .Select(job => new
                {
                    job.Id,
                    job.State,
                    Method = job.Job is null ? null : $"{job.Job.Type.FullName}.{job.Job.Method.Name}",
                    Arguments = job.Job?.Args.Select(DevMcpHangfireReader.FormatArgument).ToArray(),
                    job.At,
                    job.Exception
                })
                .ToArray();

            return DevMcpJson.Serialize(new
            {
                State = state.Trim(),
                From = from,
                Take = take,
                Count = jobs.Length,
                Jobs = jobs,
                Note = "Succeeded and deleted jobs expire after Hangfire.JobExpiration and then cannot be retrieved."
            });
        }
        catch (InvalidOperationException exception)
        {
            return DevMcpJson.Serialize(new { Error = exception.Message });
        }
    }

    [McpServerTool(Name = nameof(GetHangfireJob))]
    [Description("Fetches one Hangfire job by id, including method, arguments, state history and, for a failed job, the exception. Returns not-found if the job has expired (Hangfire.JobExpiration) or never existed. Read-only.")]
    public string GetHangfireJob([Required, Description("Hangfire job id")] string jobId)
    {
        JobDetailsDto? details;
        try
        {
            details = jobStorage.GetMonitoringApi().JobDetails(jobId);
        }
        catch (Exception exception)
        {
            return DevMcpJson.Serialize(new { Found = false, jobId, Error = exception.Message });
        }

        if (details is null)
            return DevMcpJson.Serialize(new { Found = false, jobId });

        string? method = null;
        IEnumerable<string>? arguments = null;
        try
        {
            if (details.Job is not null)
            {
                method = $"{details.Job.Type.FullName}.{details.Job.Method.Name}";
                arguments = details.Job.Args.Select(DevMcpHangfireReader.FormatArgument).ToArray();
            }
        }
        catch (Exception exception)
        {
            method ??= exception.Message;
        }

        return DevMcpJson.Serialize(new
        {
            Found = true,
            jobId,
            details.CreatedAt,
            details.ExpireAt,
            Method = method,
            Arguments = arguments,
            LoadException = details.LoadException?.InnerException?.ToString() ?? details.LoadException?.ToString(),
            History = (details.History ?? []).Select(entry => new
            {
                entry.StateName,
                entry.CreatedAt,
                entry.Reason,
                entry.Data
            })
        });
    }

    [McpServerTool(Name = nameof(ListHangfireRecurringJobs))]
    [Description("Lists Hangfire recurring jobs with cron, last and next execution, last job id and last error. Read through JobStorage.GetConnection().GetRecurringJobs, not the jobs tables. Does not add, update or trigger recurring jobs.")]
    public string ListHangfireRecurringJobs()
    {
        using var connection = jobStorage.GetConnection();
        var recurring = connection.GetRecurringJobs();

        return DevMcpJson.Serialize(recurring.Select(job => new
        {
            job.Id,
            job.Cron,
            job.TimeZoneId,
            job.Queue,
            Method = job.Job is null ? null : $"{job.Job.Type.FullName}.{job.Job.Method.Name}",
            job.LastJobId,
            job.LastJobState,
            job.LastExecution,
            job.NextExecution,
            job.Error,
            job.CreatedAt,
            job.RetryAttempt
        }));
    }
}
