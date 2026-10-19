
namespace Bit.Bink;

/// <summary>How stroke points are written.</summary>
public enum BinkPointEncoding : byte
{
    /// <summary>Quantized binary, base64 encoded. Compact and deterministic; the default.</summary>
    Binary = 0,
    /// <summary>Plain JSON number arrays. Larger, but readable and easy to consume from other languages.</summary>
    Json = 1,
}

/// <summary>Options controlling document serialization.</summary>
public sealed record BinkSerializerOptions
{
    /// <summary>Compact binary points, no indentation.</summary>
    public static readonly BinkSerializerOptions Default = new();

    /// <summary>Readable output: JSON points and indentation. Useful for diffing and debugging.</summary>
    public static readonly BinkSerializerOptions Readable = new() { PointEncoding = BinkPointEncoding.Json, Indented = true };

    /// <summary>How points are written.</summary>
    public BinkPointEncoding PointEncoding { get; init; } = BinkPointEncoding.Binary;

    /// <summary>Quantization steps per document unit for binary points.</summary>
    public int Quantization { get; init; } = BinkPointCodec.DefaultQuantization;

    /// <summary>Decimals kept for JSON-encoded points and geometry.</summary>
    public int Decimals { get; init; } = 3;

    /// <summary>Whether the JSON is indented.</summary>
    public bool Indented { get; init; }

    /// <summary>
    /// Whether embedded image data is written. When false, images are written as a reference without their
    /// <c>data:</c> payload, which keeps documents small when the application stores images separately. Such an
    /// image loads with <see cref="BinkImageSource.Omitted"/> as its source and draws nothing until the
    /// application replaces the element with one that carries the stored image.
    /// </summary>
    public bool IncludeEmbeddedImages { get; init; } = true;

    /// <summary>Brush kinds used to reconstruct custom brushes on load.</summary>
    public BinkBrushRegistry BrushRegistry { get; init; } = BinkBrushRegistry.Default;
}

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
