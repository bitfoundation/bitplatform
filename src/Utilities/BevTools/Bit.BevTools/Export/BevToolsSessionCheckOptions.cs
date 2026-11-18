namespace Bit.BevTools;

/// <summary>What fails a <see cref="BevToolsSessionCheck"/>.</summary>
public sealed class BevToolsSessionCheckOptions
{
    /// <summary>
    /// The least severe finding that fails the check; <see langword="null"/> lists findings without failing on any.
    /// <see cref="BevToolsDiagnosticSeverity.Warning"/> by default: an <see cref="BevToolsDiagnosticSeverity.Info"/> finding is a
    /// hint, not a problem.
    /// </summary>
    public BevToolsDiagnosticSeverity? FailOn { get; set; } = BevToolsDiagnosticSeverity.Warning;

    /// <summary>Rules whose findings are listed but never fail the check, by id (<c>render.storm</c>).</summary>
    public ISet<string> IgnoredRules { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Whether any recorded error fails the check, whether or not a rule reported it. Off by default: a run that
    /// exercises error handling records errors on purpose.
    /// </summary>
    public bool FailOnErrors { get; set; }
}
