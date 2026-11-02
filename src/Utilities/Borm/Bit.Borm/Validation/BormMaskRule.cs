namespace Bit.Borm;

/// <summary>
/// Validates that a value fits a <see cref="BormMask"/>: every slot filled with a character it takes, and
/// nothing left over. An empty value passes; saying an answer is missing is <c>Required()</c>'s job.
/// </summary>
public sealed class BormMaskRule(BormMask mask, string? message = null) : IBormValidationRule
{
    /// <summary>The mask values are judged against.</summary>
    public BormMask Mask { get; } = mask ?? throw new ArgumentNullException(nameof(mask));

    /// <inheritdoc />
    public string Key => BormMessageKeys.Mask;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        var text = ctx.Value as string ?? (ctx.Value is IFormattable f ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : null);
        if (string.IsNullOrEmpty(text) || Mask.TryFormat(text, out _)) return new(BormRuleResult.Success());

        return new(BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.Mask, Mask.Display)));
    }
}
