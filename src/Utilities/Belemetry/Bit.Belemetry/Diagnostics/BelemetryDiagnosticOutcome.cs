namespace Bit.Belemetry;

/// <summary>How the activity an event describes ended, when it says.</summary>
public enum BelemetryDiagnosticOutcome
{
    /// <summary>Not known, or not applicable (a log entry).</summary>
    Unknown,

    /// <summary>It succeeded.</summary>
    Success,

    /// <summary>It failed.</summary>
    Failure,

    /// <summary>It was cancelled or aborted.</summary>
    Cancelled,
}
