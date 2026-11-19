namespace Bit.BevTools;

/// <summary>A finding as a session export recorded it.</summary>
public sealed record BevToolsCheckedFinding
{
    /// <summary>The rule that reported it (<c>render.storm</c>).</summary>
    public required string RuleId { get; init; }

    /// <summary>As the rule rated it.</summary>
    public required BevToolsDiagnosticSeverity Severity { get; init; }

    /// <summary>What the rule found.</summary>
    public required string Title { get; init; }

    /// <summary>What it was found from.</summary>
    public required string Evidence { get; init; }

    /// <summary>What the rule suggests trying, when it does.</summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// What identifies the finding across evaluations (and dismissals), as the export recorded it; null in an export
    /// that has none. Stable for the same problem, so a report can tell a finding it already knows from a new one.
    /// </summary>
    public string? Fingerprint { get; init; }

    /// <summary>The component instance the finding is about, when the rule named one.</summary>
    public long? ComponentInstanceId { get; init; }

    /// <summary>Whether it fails the check under the options it was checked with.</summary>
    public bool Fails { get; init; }

    /// <summary>Whether someone dismissed it in the session; a dismissed finding never fails.</summary>
    public bool Dismissed { get; init; }
}
