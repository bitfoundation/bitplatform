# Bit.Belemetry.Hangfire

Hangfire spans and metrics for [Bit.Belemetry](https://www.nuget.org/packages/Bit.Belemetry): a job filter that records a span
for every job execution (`JOB {Type}.{Method}`, kind consumer, with `job.id`, `job.type`, `job.method`, `job.queue`
and `job.created_at`), parented to the trace that enqueued the job - the W3C trace context travels in the job's
parameters - and marked as an error when the job throws. The spans land on the `Bit.Belemetry.Hangfire` activity
source, which Bit.Belemetry collects by default, and show up in Application Insights as requests.

Hangfire.Core has no meter of its own, so the package measures the jobs too, on the `Bit.Belemetry.Hangfire` meter
(collected by default as well):

| Instrument | Kind | Attributes |
| --- | --- | --- |
| `hangfire.job.executions` | counter, `{execution}` | `job.type`, `job.method`, `job.outcome` (`succeeded`/`failed`/`canceled`), `error.type` (unless it succeeded) |
| `hangfire.job.duration` | histogram, `s` | the same |
| `hangfire.jobs.enqueued`, `.processing`, `.scheduled`, `.failed`, `.retries` | gauges, `{job}` | none |

The execution metrics count every execution the filter keeps, whether or not its trace is sampled. The gauges read
`JobStorage.Current.GetMonitoringApi().GetStatistics()` once per collection (cached for a second, so the five share one
query), and report nothing when the query fails. They describe the whole storage, so every instance reports the same
values: a dashboard takes their maximum, not their sum.

```csharp
using Bit.Belemetry;   // UseBitBelemetry

services.AddHangfire(configuration => configuration
    .UseSqlServerStorage(connectionString)
    .UseBitBelemetry(o =>
    {
        o.PropagateTraceContext = true;                       // the enqueue's trace is the execution's parent
        o.Filter = job => job.Type != typeof(HeartbeatJob);   // skip the noisy ones (no span, not counted)
        o.StorageMetrics = true;                              // the hangfire.jobs.* gauges
    }));
```

No OpenTelemetry packages are involved: the filter starts plain `System.Diagnostics.Activity` instances and records
on a plain `System.Diagnostics.Metrics.Meter`, so any `ActivityListener` or `MeterListener` - the OpenTelemetry SDK
included - can record them too. See the [Belemetry documentation](https://belemetry.bitplatform.dev) for the rest.
