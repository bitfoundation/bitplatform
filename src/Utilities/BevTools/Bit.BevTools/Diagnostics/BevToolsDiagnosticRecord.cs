namespace Bit.BevTools;

public sealed class BevToolsDiagnosticRecord
{
    public required BevToolsDiagnostic Diagnostic { get; set; }

    public DateTimeOffset FirstSeen { get; init; }

    public DateTimeOffset LastSeen { get; set; }

    public bool Dismissed { get; set; }

    /// <summary>The timeline row of the finding's latest report (first sighting or escalation); 0 when capture was paused.</summary>
    public long TimelineEventId { get; set; }

    /// <summary>The highest severity this finding has been reported with. Only a rise above it is reported again.</summary>
    internal BevToolsDiagnosticSeverity HighestSeverity { get; set; }
}
