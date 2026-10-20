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
/// against an ordinary currency step. Decimal holds both of those numbers exactly, and a value read from
/// a double is taken at the fifteen digits a double can vouch for, which is where 0.3 is still 0.3.
/// </remarks>
public sealed class BormMultipleOfRule(double factor, string? message = null) : IBormValidationRule
{
    // Decimal's range; beyond it the check falls back to double arithmetic.
    private const double DecimalLimit = 7.9e28;

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

        if (TryToDecimal(value, d, out var exact) && TryToDecimal(factor, factor, out var step) && step != 0)
        {
            try
            {
                return exact % step == 0;
            }
            catch (OverflowException)
            {
                // Falls through to the floating-point check.
            }
        }

        var quotient = d / factor;
        return Math.Abs(quotient - Math.Round(quotient)) <= Math.Max(1e-9, Math.Abs(quotient) * 1e-12);
    }

    private static bool TryToDecimal(object value, double asDouble, out decimal result)
    {
        if (value is decimal m)
        {
            result = m;
            return true;
        }

        if (Math.Abs(asDouble) < DecimalLimit)
        {
            result = BormNumber.FromDouble(asDouble);
            // Too small for decimal's scale to hold: not something to judge by its rounding to zero.
            return result != 0 || asDouble == 0;
        }

        result = 0;
        return false;
    }
}
