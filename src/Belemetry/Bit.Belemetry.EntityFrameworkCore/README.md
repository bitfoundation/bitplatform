# Bit.Belemetry.EntityFrameworkCore

Entity Framework Core spans for [Bit.Belemetry](https://www.nuget.org/packages/Bit.Belemetry): a `DbCommandInterceptor`
that records a client span for every command a `DbContext` executes, with the OpenTelemetry database semantic
conventions (`db.system.name`, `db.namespace`, `server.address`, `server.port`, `error.type`, optionally
`db.query.text`). The spans land on the `Bit.Belemetry.EntityFrameworkCore` activity source, which Bit.Belemetry
collects by default, and show up in Application Insights as database dependencies.

```csharp
services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(connectionString)
    .UseBitBelemetry(o =>
    {
        o.CaptureQueryText = false;                                   // db.query.text (off by default)
        o.Filter = (command, data) => data.Context is not JobDbContext;  // skip a job store's own commands
    }));
```

No OpenTelemetry packages are involved: the interceptor starts plain `System.Diagnostics.Activity` instances, so any
`ActivityListener` - the OpenTelemetry SDK included - can record them too. See the
[Belemetry documentation](https://belemetry.bitplatform.dev) for the rest.
