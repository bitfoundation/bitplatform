namespace Bit.Borm;

/// <summary>
/// Wraps another rule so it only runs when <paramref name="condition"/> holds.
/// Used for conditional validation (e.g. "required when country == US").
/// </summary>
public sealed class BormConditionalRule(IBormValidationRule inner, Func<BormValidationContext, bool> condition) : IBormValidationRule
{
    public bool IsAsync => inner.IsAsync;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
        => condition(ctx) ? inner.ValidateAsync(ctx) : new(BormRuleResult.Success());
}
