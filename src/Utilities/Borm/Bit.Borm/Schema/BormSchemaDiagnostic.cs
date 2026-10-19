namespace Bit.Borm;

/// <summary>How serious a <see cref="BormSchemaDiagnostic"/> is.</summary>
public enum BormSchemaDiagnosticSeverity
{
    /// <summary>The schema will render, but probably not the way its author meant.</summary>
    Warning,

    /// <summary>The schema is broken: something will throw, bind wrongly, or silently lose data.</summary>
    Error
}

/// <summary>
/// One problem found by <see cref="BormDefinition.Validate"/>.
/// </summary>
/// <param name="Path">Where in the schema the problem is, as a field path.</param>
/// <param name="Message">What is wrong, in terms of the fix.</param>
/// <param name="Severity">Whether the schema is merely suspect or actually broken.</param>
public sealed record BormSchemaDiagnostic(
    string Path,
    string Message,
    BormSchemaDiagnosticSeverity Severity = BormSchemaDiagnosticSeverity.Error)
{
    public override string ToString() => $"[{Severity}] {(Path.Length == 0 ? "(form)" : Path)}: {Message}";
}
