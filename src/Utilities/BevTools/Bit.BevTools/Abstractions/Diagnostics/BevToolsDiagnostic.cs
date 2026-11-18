
namespace Bit.BevTools;

/// <summary>A finding produced by a rule. <see cref="Fingerprint"/> deduplicates repeated findings across evaluations.</summary>
public sealed record BevToolsDiagnostic(
    string RuleId,
    BevToolsDiagnosticSeverity Severity,
    string Title,
    string Evidence,
    string? Suggestion = null,
    string? Fingerprint = null,
    long? ComponentInstanceId = null,
    IReadOnlyList<long>? RelatedEventIds = null)
{
    public string EffectiveFingerprint => Fingerprint ?? $"{RuleId}:{Title}";
}
