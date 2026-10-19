using System.Collections;

namespace Bit.Borm;

/// <summary>Fails when the value is null, empty or whitespace (or an empty collection).</summary>
public sealed class BormRequiredRule(string? message = null) : IBormValidationRule
{
    /// <inheritdoc />
    public string Key => "required";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        var empty = ctx.Value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            // A required checkbox means "you must tick this" — the semantics of HTML's own `required`
            // on a checkbox, and what "I accept the terms" always needs. Elsewhere `false` is a value.
            bool b => !b && ctx.Field?.Type == BormFieldType.Checkbox,
            // A [Flags] value is a set of choices, and with nothing ticked it is the empty set — the
            // same answer an empty List<TEnum> gives, which already failed here. Its zero member is
            // never offered as a choice (see BormEnumOptions), so it cannot be a deliberate one.
            Enum e => BormEnumOptions.IsEmptyFlags(e),
            ICollection c => c.Count == 0,
            IEnumerable e and not string => !e.Cast<object?>().Any(),
            _ => false
        };
        return new(empty
            ? BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.Required))
            : BormRuleResult.Success());
    }
}
