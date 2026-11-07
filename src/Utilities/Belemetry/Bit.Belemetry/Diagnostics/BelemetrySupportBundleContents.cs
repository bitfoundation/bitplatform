namespace Bit.Belemetry;

/// <summary>What <see cref="BelemetrySupportBundle.ReadAsync"/> found in a support bundle, and whether it is valid.</summary>
public sealed class BelemetrySupportBundleContents
{
    /// <summary>Whether the bundle is a valid, intact bundle of a supported schema version.</summary>
    public bool IsValid { get; init; }

    /// <summary>What makes it invalid.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>What was ignored (files the format does not define).</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>The bundle's schema version.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>The capture's id.</summary>
    public string? CaptureId { get; init; }

    /// <summary>When the capture was made.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>What wrote it (<c>Bit.Belemetry 11.0.0</c>).</summary>
    public string? Generator { get; init; }

    /// <summary>The issue report.</summary>
    public BelemetryIssueReport? Report { get; init; }

    /// <summary>The issue report as Markdown, as written.</summary>
    public string? ReportMarkdown { get; init; }

    /// <summary>The timeline.</summary>
    public IReadOnlyList<BelemetryDiagnosticEvent> Events { get; init; } = [];

    /// <summary>The exception groups.</summary>
    public IReadOnlyList<BelemetryExceptionGroup> ExceptionGroups { get; init; } = [];

    /// <summary>The missing evidence the manifest lists.</summary>
    public IReadOnlyList<string> MissingEvidence { get; init; } = [];

    /// <summary>The files read, with their sizes.</summary>
    public IReadOnlyDictionary<string, long> Files { get; init; } = new Dictionary<string, long>();

    internal static BelemetrySupportBundleContents Invalid(string error) => new() { Errors = [error] };

    internal static BelemetrySupportBundleContents Invalid(List<string> errors) => new() { Errors = errors };
}
