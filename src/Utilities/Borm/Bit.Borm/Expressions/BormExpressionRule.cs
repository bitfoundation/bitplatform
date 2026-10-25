namespace Bit.Borm;

/// <summary>
/// A validation rule written as an expression that must hold — <c>{End} &gt;= {Start}</c>,
/// <c>len({Code}) = 6</c>, <c>sum({Shares[].Percent}) = 100</c>. On a field, the message lands on that
/// field; on the form, it lands on <see cref="FieldPath"/>, or on the form as a whole when that is null.
/// </summary>
/// <remarks>
/// The rule fails only when the expression comes out <em>false</em>. When a value it compares is
/// missing it passes, so a rule comparing two dates says nothing until both have been given; whether
/// they have to be given at all is <c>Required()</c>'s business, and one rule doing both jobs would
/// tell the user the same thing twice.
/// </remarks>
public sealed class BormExpressionRule : IBormValidationRule
{
    /// <param name="expression">What must hold.</param>
    /// <param name="message">
    /// What the user is told when it does not. Null uses <see cref="BormMessageKeys.Rule"/> from the
    /// registered message provider — a generic sentence, so a rule worth breaking is worth a message.
    /// </param>
    /// <param name="severity">Whether a failure blocks the submit or only advises.</param>
    /// <param name="fieldPath">For a form-level rule, the field the message belongs on.</param>
    public BormExpressionRule(BormExpression expression, string? message,
        BormValidationSeverity severity = BormValidationSeverity.Error, string? fieldPath = null)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Message = string.IsNullOrEmpty(message) ? null : message;
        Severity = severity;
        FieldPath = string.IsNullOrEmpty(fieldPath) ? null : fieldPath;
    }

    /// <summary>Parses <paramref name="expression"/> into a rule.</summary>
    /// <exception cref="BormExpressionException">The text is not a valid expression.</exception>
    public BormExpressionRule(string expression, string? message,
        BormValidationSeverity severity = BormValidationSeverity.Error, string? fieldPath = null)
        : this(BormExpression.Parse(expression), message, severity, fieldPath) { }

    /// <summary>The expression that must hold.</summary>
    public BormExpression Expression { get; }

    /// <summary>What the user is told when it does not, or null for the message provider's generic text.</summary>
    public string? Message { get; }

    /// <summary>Whether a failure blocks the submit or only advises.</summary>
    public BormValidationSeverity Severity { get; }

    /// <summary>
    /// For a form-level rule, the field the message belongs on — the one the user should go and fix.
    /// Null puts it on the field being validated, or on the form as a whole for a form-level rule.
    /// </summary>
    public string? FieldPath { get; }

    /// <inheritdoc />
    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext context)
    {
        if (Expression.Test(context.Data) != false)
            return new(BormRuleResult.Success());

        var message = Message ?? context.Message(BormMessageKeys.Rule);
        return new(FieldPath is null
            ? BormRuleResult.Fail(message, Severity)
            : BormRuleResult.FailFor(FieldPath, message, Severity));
    }

    public override string ToString() => Expression.Text;
}
