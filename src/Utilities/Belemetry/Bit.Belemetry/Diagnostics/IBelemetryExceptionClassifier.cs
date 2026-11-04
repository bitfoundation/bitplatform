namespace Bit.Belemetry;

/// <summary>
/// Application-specific exception classification: a category, a grouping override, whether it is critical or to be
/// ignored. Register it as a singleton (<c>services.AddSingleton&lt;IBelemetryExceptionClassifier, MyClassifier&gt;()</c>);
/// the first one that returns a classification wins.
/// </summary>
/// <remarks>It sees the sanitized exception only, and must be fast and must not throw (one that throws is skipped).</remarks>
public interface IBelemetryExceptionClassifier
{
    /// <summary>Classifies an exception; <see langword="null"/> leaves it to the next classifier, or to the default grouping.</summary>
    /// <param name="exception">The sanitized exception (its <see cref="BelemetryDiagnosticException.Fingerprint"/> is the computed one).</param>
    BelemetryExceptionClassification? Classify(BelemetryDiagnosticException exception);
}
