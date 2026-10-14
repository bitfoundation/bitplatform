namespace Bit.Belemetry;

/// <summary>Batching of the .NET pipeline.</summary>
public sealed class BelemetryBatchOptions
{
    /// <summary>The maximum number of spans and of log records held in memory each; the oldest are dropped past it. Defaults to 2048.</summary>
    public int MaxQueueSize { get; set; } = 2048;

    /// <summary>The maximum number of records per export request; reaching it triggers an export early. Defaults to 512.</summary>
    public int MaxBatchSize { get; set; } = 512;

    /// <summary>How often spans and log records are exported. Defaults to 5 seconds.</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);
}
