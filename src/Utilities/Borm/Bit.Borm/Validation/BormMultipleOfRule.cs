namespace Bit.Borm;

/// <summary>
/// Enforces JSON Schema's <c>multipleOf</c>: the value must be an exact multiple of
/// <paramref name="factor"/>. Binary floating point representation must not reject otherwise-valid
/// input such as 0.3 against a step of 0.1, so the check is made in decimal arithmetic wherever the
/// numbers fit, and with a tolerance that scales with the size of the quotient where they do not.
/// </summary>
/// <remarks>
/// The tolerance used to be a fixed <c>1e-9</c> on the quotient. A double carries about sixteen
/// significant digits, so the larger the quotient the more of its fraction is noise: 1234567.89 against
/// a factor of 0.01 came out about 1.5e-8 away from a whole number and was refused — an ordinary price
/// against an ordinary currency step. Decimal holds both of those numbers exactly, an integer is read
/// exactly, and a double is read as the number it prints as, which is where 0.3 is still 0.3.
/// </remarks>
public sealed class BormMultipleOfRule(double factor, string? message = null) : IBormValidationRule
{
    // The factor in decimal, read once; null where decimal cannot hold it and the check uses double arithmetic.
    private readonly decimal? _step = BormNumber.TryToDecimal(factor, out var step) && step != 0 ? step : null;

    /// <inheritdoc />
    public string Key => "multipleOf";

    /// <summary>The value every accepted number must be a whole multiple of.</summary>
    public double Factor => factor;

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        if (factor == 0 || ctx.Value is null) return new(BormRuleResult.Success());
        if (!BormNumber.TryToDouble(ctx.Value, out var d)) return new(BormRuleResult.Success());

        return new(IsMultiple(ctx.Value, d)
            ? BormRuleResult.Success()
            : BormRuleResult.Fail(message ?? ctx.Message(BormMessageKeys.MultipleOf, factor)));
    }

    private bool IsMultiple(object value, double d)
    {
        // NaN and infinity are multiples of nothing.
        if (!double.IsFinite(d) || !double.IsFinite(factor)) return false;

        // Text is read through the double it parsed as. A number beyond decimal's range, or too small for
        // its scale to hold, is left to the double arithmetic below.
        if (_step is { } step && BormNumber.TryToDecimal(BormNumber.IsNumber(value) ? value : d, out var exact))
        {
            try
            {
                if (exact % step == 0) return true;

                // Binary arithmetic leaves noise past a fraction's fifteenth digit — 0.1 + 0.2 is
                // 0.30000000000000004 — so a fractional double that misses is judged again at the fifteen
                // digits a double can vouch for. A whole double is exact to its last digit.
                return value is double && !double.IsInteger(d)
                    && BormNumber.TryToDecimalAtFifteenDigits(d, out var rounded) && rounded % step == 0;
            }
            catch (OverflowException)
            {
                // Falls through to the floating-point check.
            }
        }

        var quotient = d / factor;
        return Math.Abs(quotient - Math.Round(quotient)) <= Math.Max(1e-9, Math.Abs(quotient) * 1e-12);
    }
}
