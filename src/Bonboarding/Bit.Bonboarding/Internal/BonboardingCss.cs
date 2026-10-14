using System.Globalization;

namespace Bit.Bonboarding;

/// <summary>
/// Formatting helpers for inline styles. Everything here is invariant-culture: a comma decimal
/// separator produces silently broken CSS, and that is the kind of bug that only shows up for
/// users in another locale.
/// </summary>
internal static class BonboardingCss
{
    /// <summary>Formats a length in pixels, rounded to two decimals.</summary>
    public static string Px(double value)
        => Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture) + "px";

    /// <summary>Formats a unitless number, such as an opacity.</summary>
    public static string Number(double value)
        => Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>Formats a duration in milliseconds.</summary>
    public static string Ms(TimeSpan value)
        => ((int)Math.Round(value.TotalMilliseconds)).ToString(CultureInfo.InvariantCulture) + "ms";

    /// <summary>Formats an integer.</summary>
    public static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Joins non-empty class names with a single space.</summary>
    public static string? Classes(params string?[] values)
    {
        var present = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return present.Length == 0 ? null : string.Join(' ', present);
    }
}
