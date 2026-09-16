using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Shared validation helpers for BitMap providers, overlays and the imperative API.
/// Centralized here so every backend uses the same rules (and so they can be unit-tested
/// in one place).
/// <para>
/// Most of these exist to turn a value that would break somewhere far away - a NaN that
/// <c>System.Text.Json</c> refuses to serialize, an empty id that silently shadows another
/// layer - into an exception at the call site that produced it.
/// </para>
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
            // Intentionally do not echo the raw tileUrl back in the message - it can carry
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

    /// <summary>
    /// Validates an identifier used as a dictionary key on both sides of the interop boundary.
    /// Whitespace-only ids are rejected as well as empty ones: they look distinct in source but
    /// are indistinguishable in a UI, so they turn into silent overwrites.
    /// </summary>
    public static void ValidateId(string? id, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"{propertyName} must be a non-empty, non-whitespace value.", propertyName);
        }
    }

    /// <summary>
    /// Validates that a number is finite. NaN and ±Infinity are rejected by
    /// <c>System.Text.Json</c> mid-serialization, which surfaces as an opaque interop failure
    /// far from the call that produced the value.
    /// </summary>
    public static void ValidateFinite(double value, string propertyName)
    {
        if (double.IsFinite(value) is false)
        {
            throw new ArgumentOutOfRangeException(propertyName, value, $"{propertyName} must be a finite number.");
        }
    }

    /// <summary>Validates a zoom level is finite and inside the range every supported backend accepts.</summary>
    public static void ValidateZoom(double zoom, string propertyName)
    {
        ValidateFinite(zoom, propertyName);
        if (zoom is < 0 or > 30)
        {
            throw new ArgumentOutOfRangeException(propertyName, zoom, $"{propertyName} must be between 0 and 30.");
        }
    }

    /// <summary>Validates an optional zoom level; null means "keep the current zoom".</summary>
    public static void ValidateOptionalZoom(double? zoom, string propertyName)
    {
        if (zoom.HasValue) ValidateZoom(zoom.Value, propertyName);
    }

    /// <summary>Validates a padding in pixels is finite and non-negative.</summary>
    public static void ValidatePadding(int paddingPixels, string propertyName)
    {
        if (paddingPixels < 0)
        {
            throw new ArgumentOutOfRangeException(propertyName, paddingPixels, $"{propertyName} cannot be negative.");
        }
    }

    /// <summary>
    /// Validates a circle radius in meters. A non-finite radius would propagate NaN through every
    /// point of the generated ring; a negative one has no meaning.
    /// </summary>
    public static void ValidateRadius(double radiusMeters, string propertyName)
    {
        ValidateFinite(radiusMeters, propertyName);
        if (radiusMeters < 0)
        {
            throw new ArgumentOutOfRangeException(propertyName, radiusMeters, $"{propertyName} cannot be negative.");
        }
    }

    /// <summary>
    /// Validates that a geometry has enough points to be drawable. Fewer than two points is not a
    /// line and fewer than three is not a polygon; the backends handle those inconsistently
    /// (some throw, some render nothing), so reject them here instead.
    /// </summary>
    public static void ValidatePointCount(int count, int minimum, string propertyName, string geometryName)
    {
        if (count < minimum)
        {
            throw new ArgumentException(
                $"{propertyName} must contain at least {minimum} points to form a {geometryName} (got {count}).",
                propertyName);
        }
    }
}
