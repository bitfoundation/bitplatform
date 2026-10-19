using System.Collections;

namespace Bit.Borm;

/// <summary>Enforces minimum/maximum item counts on array/collection fields.</summary>
public sealed class BormCollectionSizeRule(int? min, int? max, string? message = null) : IBormValidationRule
{
    /// <inheritdoc />
    public string Key => "items";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        var count = ctx.Value switch
        {
            null => 0,
            ICollection c => c.Count,
            IEnumerable e and not string => e.Cast<object?>().Count(),
            _ => 0
        };
        if (min.HasValue && count < min.Value)
            return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.MinItems, min)));
        if (max.HasValue && count > max.Value)
            return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.MaxItems, max)));
        return new(BormRuleResult.Success());
    }
}
