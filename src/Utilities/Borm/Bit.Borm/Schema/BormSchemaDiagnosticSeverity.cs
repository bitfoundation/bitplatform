namespace Bit.Borm;

/// <summary>How serious a <see cref="BormSchemaDiagnostic"/> is.</summary>
public enum BormSchemaDiagnosticSeverity
{
    /// <summary>The schema will render, but probably not the way its author meant.</summary>
    Warning,

    /// <summary>The schema is broken: something will throw, bind wrongly, or silently lose data.</summary>
    Error
}
