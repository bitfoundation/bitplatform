namespace Bit.Borm;

/// <summary>
/// Wraps another rule so it only runs when <paramref name="condition"/> holds.
/// Used for conditional validation (e.g. "required when country == US").
/// </summary>
/// <remarks>
/// It has no <see cref="IBormValidationRule.Key"/> of its own, deliberately: keys decide which rule a
/// newly added one replaces, and an unconditional <c>Required()</c> must not quietly replace a
/// conditional one. The messages it produces still carry the wrapped rule's key as their
/// <see cref="BormValidationMessage.Code"/>.
/// </remarks>
public sealed class BormConditionalRule(IBormValidationRule inner, Func<BormValidationContext, bool> condition) : IBormValidationRule
{
    /// <summary>The rule that runs while the condition holds.</summary>
    public IBormValidationRule Inner { get; } = inner;

    public bool IsAsync => Inner.IsAsync;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
        => condition(ctx) ? Inner.ValidateAsync(ctx) : new(BormRuleResult.Success());
}
