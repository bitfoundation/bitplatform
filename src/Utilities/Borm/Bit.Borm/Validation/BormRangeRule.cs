using System.Globalization;

namespace Bit.Borm;

/// <summary>
/// Enforces a range — inclusive unless <see cref="MinExclusive"/> or <see cref="MaxExclusive"/> says
/// otherwise. Bounds are held as numbers; a date, time or timestamp is compared as
/// the OLE automation number that stands for it, which is the same form the schema's
/// <see cref="BormFieldDefinition.Min"/>/<see cref="BormFieldDefinition.Max"/> and the
/// rendered <c>min</c>/<c>max</c> attributes already use.
/// </summary>
public sealed class BormRangeRule(double? min, double? max, string? message = null) : IBormValidationRule
{
    /// <inheritdoc />
    public string Key => "range";

    /// <summary>The lower bound, if any. Inclusive unless <see cref="MinExclusive"/> is set.</summary>
    public double? Min => min;

    /// <summary>The upper bound, if any. Inclusive unless <see cref="MaxExclusive"/> is set.</summary>
    public double? Max => max;

    /// <summary>
    /// Whether the value must be strictly greater than <see cref="Min"/> — JSON Schema's
    /// <c>exclusiveMinimum</c>, DataAnnotations' <c>MinimumIsExclusive</c>.
    /// </summary>
    /// <remarks>
    /// Carried as a flag rather than approximated by nudging the bound: no nudge is right for every
    /// value ("greater than 0" is not "at least 0.000000001"), the nudged number leaks into the message
    /// the user reads, and it exports as an inclusive bound, so the exclusive meaning is lost for good.
    /// </remarks>
    public bool MinExclusive { get; init; }

    /// <summary>Whether the value must be strictly less than <see cref="Max"/>.</summary>
    public bool MaxExclusive { get; init; }

    public ValueTask<BormRuleResult> ValidateAsync(BormValidationContext ctx)
    {
        if (ctx.Value is null) return new(BormRuleResult.Success());

        // A date is not a number, but the bound it is judged against is stored as one — so a value the
        // numeric widening rejects gets a second chance as a date before the rule gives up. Without
        // that, `[Range(typeof(DateTime), "2020-01-01", "2030-01-01")]` was accepted by the scanner,
        // rendered as the input's min/max, and then never enforced at all: every date passed.
        if (!BormNumber.TryToDouble(ctx.Value, out var d)
            && !BormNumber.TryToOADate(ctx.Value, out d))
            return new(BormRuleResult.Success());

        // NaN compares false against everything, so without this it sat inside every range there is.
        if (double.IsNaN(d)) return new(BormRuleResult.Fail(message ?? RangeMessage(ctx, belowMin: true)));

        var belowMin = min is { } lo && (MinExclusive ? d <= lo : d < lo);
        var aboveMax = max is { } hi && (MaxExclusive ? d >= hi : d > hi);
        if (belowMin || aboveMax)
            return new(BormRuleResult.Fail(message ?? RangeMessage(ctx, belowMin)));
        return new(BormRuleResult.Success());
    }

    private string RangeMessage(BormValidationContext ctx, bool belowMin)
    {
        var type = ctx.Field?.Type;

        // "Between" reads as inclusive, so it is only used when both ends are; otherwise the message
        // names the end that was crossed, in words that say whether the bound itself is allowed.
        if (min is { } lo && max is { } hi && !MinExclusive && !MaxExclusive)
            return ctx.Message(BormMessageKeys.RangeBetween, Format(lo, type), Format(hi, type));

        if (min is { } low && (belowMin || max is null))
            return MinExclusive
                ? ctx.Message(BormMessageKeys.RangeAbove, Format(low, type))
                : ctx.Message(BormMessageKeys.RangeMin, Format(low, type));

        if (max is { } high)
            return MaxExclusive
                ? ctx.Message(BormMessageKeys.RangeBelow, Format(high, type))
                : ctx.Message(BormMessageKeys.RangeMax, Format(high, type));

        return ctx.Message(BormMessageKeys.RangeBetween);
    }

    /// <summary>
    /// Renders a bound the way the user reads the field. "45292" is not a date anyone can act on, so on
    /// a date field the number is turned back into the date it stands for; everywhere else it is a
    /// number and stays one. Bounds are shown to the user, so they follow the ambient culture rather
    /// than the invariant formatting used for HTML attributes.
    /// </summary>
    private static string Format(double value, BormFieldType? type) => type switch
    {
        BormFieldType.Date => FromOADate(value)?.ToString("d", CultureInfo.CurrentCulture) ?? Number(value),
        BormFieldType.Time => FromOADate(value)?.ToString("t", CultureInfo.CurrentCulture) ?? Number(value),
        BormFieldType.DateTime => FromOADate(value)?.ToString("g", CultureInfo.CurrentCulture) ?? Number(value),
        _ => Number(value)
    };

    private static DateTime? FromOADate(double value)
    {
        try
        {
            return DateTime.FromOADate(value);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string Number(double value) => value.ToString("0.############", CultureInfo.CurrentCulture);
}
