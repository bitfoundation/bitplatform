using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Shared validation helpers for BitMap providers and overlays. Centralized here so
/// every backend uses the same rules (and so they can be unit-tested in one place).
/// </summary>
internal static class BitMapValidation
{
    // Conservative ECMAScript identifier shape: letter/underscore/$ followed by letters/digits/underscore/$.
    // This is intentionally narrower than the spec because <see cref="IBitMapProvider.JsObjectName"/> is
    // interpolated into a JS call site (e.g. BitBlazorUI.{name}.init), so we want to reject anything
    // that could break out of the property lookup such as quotes, brackets, dots, or whitespace.
    private static readonly Regex _jsIdentifier = new(
        "^[A-Za-z_$][A-Za-z0-9_$]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates an XYZ tile URL template. The value must be non-empty and contain the
    /// <c>{z}</c>, <c>{x}</c> and <c>{y}</c> placeholders that every supported tile
    /// backend expects. Throws <see cref="ArgumentException"/> on failure so configuration
    /// mistakes surface in .NET rather than as opaque JS errors.
    /// </summary>
    public static void ValidateTileUrl(string? tileUrl, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(tileUrl))
        {
            throw new ArgumentException($"{propertyName} must be a non-empty XYZ tile URL template.", propertyName);
        }

        if (tileUrl.Contains("{z}", StringComparison.Ordinal) is false
            || tileUrl.Contains("{x}", StringComparison.Ordinal) is false
            || tileUrl.Contains("{y}", StringComparison.Ordinal) is false)
        {
            // Intentionally do not echo the raw tileUrl back in the message — it can carry
            // API keys or other sensitive query parameters and would land in logs/stack traces.
            throw new ArgumentException(
                $"{propertyName} must contain the {{z}}, {{x}} and {{y}} placeholders. The optional {{s}} placeholder is also supported.",
                propertyName);
        }
    }

    /// <summary>
    /// Validates a tile max-zoom value is within the broadly supported XYZ range (0–30).
    /// </summary>
    public static void ValidateTileMaxZoom(int tileMaxZoom, string propertyName)
    {
        if (tileMaxZoom is < 0 or > 30)
        {
            throw new ArgumentOutOfRangeException(
                propertyName,
                tileMaxZoom,
                $"{propertyName} must be between 0 and 30.");
        }
    }

    /// <summary>
    /// Validates that <paramref name="jsObjectName"/> is a safe ECMAScript identifier.
    /// Used to defend against JS-side identifier injection when third-party providers
    /// supply their own <see cref="IBitMapProvider.JsObjectName"/>.
    /// </summary>
    public static void ValidateJsObjectName(string? jsObjectName)
    {
        if (string.IsNullOrEmpty(jsObjectName) || _jsIdentifier.IsMatch(jsObjectName) is false)
        {
            throw new InvalidOperationException(
                "IBitMapProvider.JsObjectName must be a non-empty JavaScript identifier (letters, digits, '_' and '$' only, not starting with a digit).");
        }
    }
}
