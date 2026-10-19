namespace Bit.Borm;

/// <summary>An asynchronous custom rule backed by a delegate (e.g. remote uniqueness checks).</summary>
public sealed class BormAsyncDelegateRule(Func<BormValidationContext, ValueTask<BormRuleResult>> validate) : IBormValidationRule
{
    public bool IsAsync => true;
    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx) => validate(ctx);
}
