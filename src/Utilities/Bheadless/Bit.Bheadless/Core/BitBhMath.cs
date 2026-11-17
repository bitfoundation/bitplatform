using System.Globalization;

namespace Bit.Bheadless;

/// <summary>Numeric helpers shared by the components.</summary>
internal static class BitBhMath
{
    /// <summary>
    /// <see cref="Math.Clamp(double, double, double)"/> that does not throw while the bounds are crossed (a bound
    /// Min / Max can be, for a render): <paramref name="min"/> wins.
    /// </summary>
    internal static double Clamp(double value, double min, double max) => Math.Clamp(value, min, Math.Max(min, max));

    /// <summary>
    /// Snaps <paramref name="value"/> to the nearest <paramref name="origin"/> + n × <paramref name="step"/>, a midpoint
    /// rounding away from zero. When the step and the origin are short decimals (0.1, 0.125, 2.5 - the steps people
    /// write) the multiple is computed in <see cref="decimal"/>, so it lands on the number written (0.3, not
    /// 0.30000000000000004); any other step (1.0 / 3, 1e-30) and any value out of decimal's range is computed in
    /// <see cref="double"/>, where n × step still reaches the end of a range that is a multiple of it.
    /// </summary>
    /// <param name="value">The value to snap.</param>
    /// <param name="origin">Where the steps start (a slider's Min).</param>
    /// <param name="step">A positive, finite step.</param>
    internal static double Snap(double value, double origin, double step)
        => AtStep(origin, step, WholeSteps(value, origin, step, MidpointRounding.AwayFromZero));

    /// <summary>
    /// The whole number of steps from <paramref name="origin"/> to <paramref name="value"/>, rounded by
    /// <paramref name="mode"/> (<see cref="MidpointRounding.ToNegativeInfinity"/> counts the steps that fit). Short
    /// decimals are divided in <see cref="decimal"/>: 0.3 / 0.1 is 3 there, 2.9999999999999996 in double.
    /// </summary>
    internal static double WholeSteps(double value, double origin, double step, MidpointRounding mode)
    {
        var steps = (value - origin) / step;

        // A value written with at most 15 significant digits can tell an exact midpoint from one a binary
        // approximation leaves just under it (0.15 / 0.1 is 1.4999999999999998 in double).
        if (Math.Abs(steps) < MaxExactSteps
            && TryToDecimalExactly(value, out var decimalValue)
            && TryToDecimalExactly(origin, out var decimalOrigin)
            && TryToDecimalExactly(step, out var decimalStep))
            return (double)Math.Round((decimalValue - decimalOrigin) / decimalStep, mode);

        return Math.Round(steps, mode);
    }

    /// <summary>
    /// <paramref name="origin"/> + <paramref name="steps"/> × <paramref name="step"/> for a whole number of steps, in
    /// <see cref="decimal"/> when the step and the origin are short decimals and the result fits in it (3 × 0.1 is 0.3,
    /// not 0.30000000000000004), in <see cref="double"/> otherwise.
    /// </summary>
    internal static double AtStep(double origin, double step, double steps)
    {
        if (Math.Abs(steps) < MaxExactSteps
            && Math.Abs(steps * step) < MaxDecimalOperand
            && TryToDecimalExactly(step, out var decimalStep)
            && TryToDecimalExactly(origin, out var decimalOrigin))
            return (double)((decimal)steps * decimalStep + decimalOrigin);

        return steps * step + origin;
    }

    // Under 1e15 a whole number of steps converts to decimal exactly, and with the product and the origin each under
    // 1e27 their sum cannot leave decimal's range (about 7.9e28) - a product is checked, as 1e10 steps of 1e20 is not.
    private const double MaxExactSteps = 1e15;
    private const double MaxDecimalOperand = 1e27;

    /// <summary>
    /// Converts <paramref name="value"/> to the decimal it reads as - false for one that takes more than 15
    /// significant digits to write (1.0 / 3, 0.1 + 0.2: a decimal copy of its binary approximation would carry the
    /// error along), one under decimal's precision, and one decimal cannot hold (out of range, NaN, an infinity).
    /// Checks instead of catching, as it runs on every pointer move of a drag.
    /// </summary>
    private static bool TryToDecimalExactly(double value, out decimal result)
    {
        Span<char> text = stackalloc char[32];
        if (double.IsFinite(value) && Math.Abs(value) < MaxDecimalOperand
            && value.TryFormat(text, out var length, "G15", CultureInfo.InvariantCulture)
            && decimal.TryParse(text[..length], NumberStyles.Float, CultureInfo.InvariantCulture, out result)
            && (double)result == value)
            return true;
        result = 0;
        return false;
    }
}
