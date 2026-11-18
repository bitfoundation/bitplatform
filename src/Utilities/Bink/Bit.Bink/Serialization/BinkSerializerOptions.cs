
namespace Bit.Bink;

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

    /// <summary>
    /// Whether an embedded image shown by more than one element is written once, in the document's asset table, and
    /// referred to from each element; on load those elements share one image again. On by default. A document that
    /// uses the table is written as format version 2, which earlier versions of Bink refuse to open rather than
    /// silently losing its pictures; a document without shared images is still written as version 1. Turn it off
    /// while documents must open in a Bink version from before the table.
    /// </summary>
    public bool ShareImageData { get; init; } = true;

    /// <summary>Brush kinds used to reconstruct custom brushes on load.</summary>
    public BinkBrushRegistry BrushRegistry { get; init; } = BinkBrushRegistry.Default;

    /// <summary>Converters for custom element types. A custom element without one cannot be written.</summary>
    public BinkElementRegistry ElementRegistry { get; init; } = BinkElementRegistry.Default;

    /// <summary>
    /// Whether the order keys and move clocks of layers and elements are written (see <see cref="BinkOrderKey"/>), so
    /// that a document read back has exactly the same ones. Off by default: a file's stacking order is the order it
    /// lists things in, and keys are made from that order when it is read. A collaboration session turns it on for
    /// the snapshots it sends, so a joining participant continues with the keys everyone else has. Readers that
    /// predate the keys ignore them.
    /// </summary>
    public bool IncludeOrderKeys { get; init; }
}
