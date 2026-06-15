using System.Globalization;

namespace Bit.Bmotion;
/// <summary>
/// Centralised, culture-invariant number↔CSS conversion helpers.
/// <para>
/// Every numeric value that ends up in a CSS string (transforms, opacity, colours,
/// dimensions, dash arrays, …) MUST be formatted through here. Using the default
/// <see cref="object.ToString()"/> would honour the current culture and emit a comma
/// decimal separator in locales such as <c>de-DE</c> or <c>fr-FR</c>, producing invalid
/// CSS like <c>translate(1,5px,0px)</c>. All formatting and parsing here pins
/// <see cref="CultureInfo.InvariantCulture"/>.
/// </para>
/// </summary>
internal static class BmotionCssFormat
{
    /// <summary>
    /// Formats a double as an invariant-culture string using compact <c>"G6"</c> formatting
    /// (up to 6 significant digits). This is lossy, not full round-trip precision, but keeps
    /// emitted CSS short while staying visually accurate for animation values.
    /// </summary>
    public static string Num(double value)
        => value.ToString("G6", CultureInfo.InvariantCulture);

    /// <summary>Formats a double as an invariant-culture string using the given numeric format.</summary>
    public static string Num(double value, string format)
        => value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Parses a double using invariant culture. Returns <c>false</c> on failure.</summary>
    public static bool TryParse(string? text, out double value)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    /// <summary>Parses a double using invariant culture, throwing on failure.</summary>
    public static double Parse(string text)
        => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
}
