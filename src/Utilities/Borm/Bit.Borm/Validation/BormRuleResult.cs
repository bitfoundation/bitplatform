namespace Bit.Borm;

/// <summary>The result of running a single validation rule.</summary>
public readonly struct BormRuleResult
{
    private BormRuleResult(bool isValid, string? message, BormValidationSeverity severity, string? fieldPath)
    {
        IsValid = isValid;
        Message = message;
        Severity = severity;
        FieldPath = fieldPath;
    }

    public bool IsValid { get; }
    public string? Message { get; }
    public BormValidationSeverity Severity { get; }

    /// <summary>
    /// The path the message should be attached to. Null (the usual case) means the field being
    /// validated; a cross-field or form-level rule sets it to point the user at the field to fix.
    /// </summary>
    public string? FieldPath { get; }

    public static BormRuleResult Success() => new(true, null, BormValidationSeverity.Error, null);

    public static BormRuleResult Fail(string message, BormValidationSeverity severity = BormValidationSeverity.Error)
        => new(false, message, severity, null);

    /// <summary>Fails and attaches the message to a specific field path rather than the field being validated.</summary>
    public static BormRuleResult FailFor(string fieldPath, string message,
        BormValidationSeverity severity = BormValidationSeverity.Error)
        => new(false, message, severity, fieldPath);
}
