using System.Collections;

namespace Bit.Borm;

/// <summary>Enforces a maximum string length.</summary>
/// <remarks>
/// On a field that holds several strings rather than one — a tag list — the limit is applied to each
/// of them. Reading a collection as a string and finding it is not one made the rule quietly do
/// nothing, which is how a limit the browser enforces while typing was never enforced on a value that
/// arrived any other way.
/// </remarks>
public sealed class BormMaxLengthRule(int max, string? message = null) : IBormValidationRule
{
    /// <inheritdoc />
    public string Key => "maxLength";

    /// <summary>The configured maximum length.</summary>
    public int Max => max;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        var tooLong = ctx.Value switch
        {
            string s => s.Length > max,
            IEnumerable items when IsMultiValued(ctx) => items.Cast<object?>()
                .Any(i => BormValueConverter.ToInvariantString(i).Length > max),
            _ => false
        };

        return tooLong
            ? new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.MaxLength, max)))
            : new(BormRuleResult.Success());
    }

    /// <summary>
    /// Whether the field holds a collection of strings each of which the limit applies to, as opposed
    /// to a repeater whose length is <see cref="BormCollectionSizeRule"/>'s business.
    /// </summary>
    internal static bool IsMultiValued(BormValidationContext ctx)
        => ctx.Field?.Type == BormFieldType.Tags;
}
