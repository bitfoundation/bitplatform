
namespace Bit.BevTools;

/// <summary>
/// A rule that inspects recent activity and reports evidence-backed findings. Built-in rules cover render storms,
/// render cascades, slow renders, duplicate/slow HTTP requests, slow/excessive JS interop and repeated errors.
/// </summary>
public interface IBevToolsDiagnosticRule
{
    string Id { get; }

    string Title { get; }

    string Description { get; }

    IEnumerable<BevToolsDiagnostic> Evaluate(IBevToolsDiagnosticContext context);
}
