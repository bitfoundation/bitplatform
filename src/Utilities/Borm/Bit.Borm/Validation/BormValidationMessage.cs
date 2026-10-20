namespace Bit.Borm;

/// <summary>A single validation message tied to a field path.</summary>
/// <param name="FieldPath">The path of the field the message belongs to.</param>
/// <param name="Message">Human-readable message.</param>
/// <param name="Severity">Severity of the message.</param>
public sealed record BormValidationMessage(string FieldPath, string Message, BormValidationSeverity Severity = BormValidationSeverity.Error);
