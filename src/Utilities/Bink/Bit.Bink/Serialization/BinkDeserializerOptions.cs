
namespace Bit.Bink;

/// <summary>Options controlling document deserialization, including the limits enforced on untrusted input.</summary>
public sealed record BinkDeserializerOptions
{
    /// <summary>Safe defaults for untrusted input.</summary>
    public static readonly BinkDeserializerOptions Default = new();

    /// <summary>Limits applied to the loaded document.</summary>
    public BinkDocumentLimits Limits { get; init; } = BinkDocumentLimits.Default;

    /// <summary>Brush kinds available for reconstruction. Unknown kinds fall back to a pen with the stored properties.</summary>
    public BinkBrushRegistry BrushRegistry { get; init; } = BinkBrushRegistry.Default;

    /// <summary>
    /// Converters for custom element types. An element whose type has none is kept as a
    /// <see cref="BinkUnknownElement"/>, or dropped when <see cref="PreserveUnknownElements"/> is off.
    /// </summary>
    public BinkElementRegistry ElementRegistry { get; init; } = BinkElementRegistry.Default;

    /// <summary>
    /// Whether elements of unknown types are preserved as <see cref="BinkUnknownElement"/> so a round trip through an
    /// older version of the library does not lose data. When false they are dropped.
    /// </summary>
    public bool PreserveUnknownElements { get; init; } = true;

    /// <summary>Whether <c>image/svg+xml</c> data URIs are accepted for images. Off by default.</summary>
    public bool AllowSvgImages { get; init; }

    /// <summary>
    /// Whether remote (<c>http:</c>/<c>https:</c>) image URLs are accepted. Loading them causes the browser to make
    /// requests to third parties, so applications handling untrusted documents may want to refuse them.
    /// </summary>
    public bool AllowRemoteImages { get; init; } = true;

    /// <summary>Maximum accepted input size in bytes.</summary>
    public int MaxInputBytes { get; init; } = 256 * 1024 * 1024;
}
