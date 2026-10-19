namespace Bit.Borm;

/// <summary>
/// Requires the value to be one of the field's declared options — the rule that makes a combobox a
/// closed choice rather than a text box with hints beside it.
/// </summary>
/// <remarks>
/// <para>
/// A combobox lets the user type, so it can be answered with something that is on no list. That is a
/// legitimate design (a "city" box that accepts anywhere), which is why this is opt-in rather than
/// implied by the field type — and why it reports the answer instead of quietly discarding it, exactly
/// as a value the model cannot accept is reported rather than swallowed.
/// </para>
/// <para>
/// A field whose choices come from an <see cref="BormFieldDefinition.OptionsProvider"/> is not
/// checked: the list is loaded into the form's runtime state, not into the schema, so at rule time
/// there is nothing here to compare against and failing every answer would be worse than checking
/// none. Validate those on the server, where the same lookup lives.
/// </para>
/// </remarks>
public sealed class BormOneOfRule(string? message = null) : IBormValidationRule
{
    /// <inheritdoc />
    public string Key => "oneOf";

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        // An empty answer is Required's business, not this rule's.
        if (ctx.Value is null || (ctx.Value is string s && s.Length == 0))
            return new(BormRuleResult.Success());

        if (ctx.Field is not { } field || field.OptionsProvider is not null || field.Options.Count == 0)
            return new(BormRuleResult.Success());

        var text = BormValueConverter.ToInvariantString(ctx.Value);
        foreach (var option in field.Options)
            if (string.Equals(option.Value, text, StringComparison.OrdinalIgnoreCase))
                return new(BormRuleResult.Success());

        return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.OneOf)));
    }
}
