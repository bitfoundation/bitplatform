# Bit.Belemetry.Hangfire

Hangfire spans for [Bit.Belemetry](https://www.nuget.org/packages/Bit.Belemetry): a job filter that records a span
for every job execution (`JOB {Type}.{Method}`, kind consumer, with `job.id`, `job.type`, `job.method`, `job.queue`
and `job.created_at`), parented to the trace that enqueued the job - the W3C trace context travels in the job's
parameters - and marked as an error when the job throws. The spans land on the `Bit.Belemetry.Hangfire` activity
source, which Bit.Belemetry collects by default, and show up in Application Insights as requests.

```csharp
services.AddHangfire(configuration => configuration
    .UseSqlServerStorage(connectionString)
    .UseBitBelemetry(o =>
    {
        o.PropagateTraceContext = true;                       // the enqueue's trace is the execution's parent
        o.Filter = job => job.Type != typeof(HeartbeatJob);   // skip the noisy ones
    }));
```

No OpenTelemetry packages are involved: the filter starts plain `System.Diagnostics.Activity` instances, so any
`ActivityListener` - the OpenTelemetry SDK included - can record them too. See the
[Belemetry documentation](https://belemetry.bitplatform.dev) for the rest.
