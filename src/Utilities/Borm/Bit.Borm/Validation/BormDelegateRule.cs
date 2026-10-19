namespace Bit.Borm;

/// <summary>A synchronous custom rule backed by a delegate.</summary>
public sealed class BormDelegateRule(Func<BormValidationContext, BormRuleResult> validate) : IBormValidationRule
{
    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx) => new(validate(ctx));
}
