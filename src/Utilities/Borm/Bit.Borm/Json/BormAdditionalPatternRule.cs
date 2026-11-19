namespace Bit.Borm;

/// <summary>
/// A further <c>pattern</c> the value must also match — what an <c>allOf</c> of two patterns means.
/// Keyless, so it sits beside the field's own <see cref="BormPatternRule"/> rather than
/// replacing it, and checks exactly what that rule checks (each tag of a tag list included).
/// </summary>
internal sealed class BormAdditionalPatternRule(string pattern) : IBormValidationRule
{
    private readonly BormPatternRule _rule = new(pattern);

    /// <summary>The pattern.</summary>
    public string Pattern => pattern;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx) => _rule.ValidateAsync(ctx);
}
