
namespace Bit.BevTools;

public enum BevDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>Snapshot of a component instance made available to diagnostics rules.</summary>
public sealed record BevComponentSnapshot(
    long InstanceId,
    string TypeName,
    string DisplayName,
    long? ParentInstanceId,
    int Depth,
    int RenderCount,
    double TotalRenderMs,
    double LastRenderMs,
    double MaxRenderMs,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastRenderAt,
    bool IsDisposed,
    int ErrorCount,
    bool IsFrameworkInternal = false,
    int ParentRendersWithUnchangedParameters = 0)
{
    public double AverageRenderMs => RenderCount == 0 ? 0 : TotalRenderMs / RenderCount;
}

/// <summary>Everything a rule may look at. Rules must not mutate state and should be cheap (they run on the UI refresh cadence).</summary>
public interface IBevDiagnosticContext
{
    DateTimeOffset Now { get; }

    IReadOnlyList<BevComponentSnapshot> Components { get; }

    /// <summary>True when the component is a framework or library internal the application cannot change.</summary>
    bool IsFrameworkInternal(long instanceId);

    /// <summary>Recent timeline events, oldest first.</summary>
    IReadOnlyList<BevToolsEvent> Events { get; }

    /// <summary>Events of a kind within the trailing window.</summary>
    IEnumerable<BevToolsEvent> EventsInWindow(BevToolsEventKind kind, TimeSpan window);

    /// <summary>Access to configured thresholds. Returns null when the option type is unknown.</summary>
    T? GetOptions<T>() where T : class;

    /// <summary>Platform name reported by the renderer ("Server", "WebAssembly", "Static", "WebView") or null when unknown.</summary>
    string? Platform { get; }
}

/// <summary>A finding produced by a rule. <see cref="Fingerprint"/> deduplicates repeated findings across evaluations.</summary>
public sealed record BevDiagnostic(
    string RuleId,
    BevDiagnosticSeverity Severity,
    string Title,
    string Evidence,
    string? Suggestion = null,
    string? Fingerprint = null,
    long? ComponentInstanceId = null,
    IReadOnlyList<long>? RelatedEventIds = null)
{
    public string EffectiveFingerprint => Fingerprint ?? $"{RuleId}:{Title}";
}

/// <summary>
/// A rule that inspects recent activity and reports evidence-backed findings. Built-in rules cover render storms,
/// render cascades, slow renders, duplicate/slow HTTP requests, slow/excessive JS interop and repeated errors.
/// </summary>
public interface IBevDiagnosticRule
{
    string Id { get; }

    string Title { get; }

    string Description { get; }

    IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context);
}
