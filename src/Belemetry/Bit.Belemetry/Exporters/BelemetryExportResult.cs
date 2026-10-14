namespace Bit.Belemetry;

/// <summary>The outcome of an export.</summary>
public enum BelemetryExportResult
{
    /// <summary>The batch was delivered (or deliberately discarded, e.g. because it was empty).</summary>
    Success,

    /// <summary>The batch could not be delivered.</summary>
    Failure,
}
