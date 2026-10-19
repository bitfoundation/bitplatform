using System.Collections;

namespace Bit.Borm;

/// <summary>Enforces a minimum string length.</summary>
/// <remarks>
/// On a field that holds several strings rather than one — a tag list — the limit is applied to each
/// of them. An empty value is <see cref="BormRequiredRule"/>'s business either way.
/// </remarks>
public sealed class BormMinLengthRule(int min, string? message = null) : IBormValidationRule
{
    /// <inheritdoc />
    public string Key => "minLength";

    /// <summary>The configured minimum length.</summary>
    public int Min => min;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        var tooShort = ctx.Value switch
        {
            string s => s.Length > 0 && s.Length < min,
            IEnumerable items when BormMaxLengthRule.IsMultiValued(ctx) => items.Cast<object?>()
                .Select(BormValueConverter.ToInvariantString)
                .Any(s => s.Length > 0 && s.Length < min),
            _ => false
        };

        return tooShort
            ? new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.MinLength, min)))
            : new(BormRuleResult.Success());
    }
}
