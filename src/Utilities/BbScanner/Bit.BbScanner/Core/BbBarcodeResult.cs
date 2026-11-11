using System.Diagnostics.CodeAnalysis;

namespace Bit.BbScanner;

/// <summary>
/// A single successfully decoded barcode.
/// </summary>
public sealed class BbBarcodeResult
{
    private static readonly BbScanPoint[] NoPoints = [];
    private static readonly object NotGs1 = new();

    // What TryGetGs1 found, cached: the parsed elements, or NotGs1 when the text did not parse.
    private object? _gs1;

    internal BbBarcodeResult(
        string text,
        byte[] rawBytes,
        BbBarcodeFormat format,
        BbScanPoint[]? corners,
        BbBarcodeResultMetadata? metadata,
        DateTimeOffset timestamp)
    {
        Text = text;
        RawBytes = rawBytes;
        Format = format;
        var points = corners ?? NoPoints;
        Corners = points;
        Metadata = metadata;
        Timestamp = timestamp;
        BoundingBox = BbScanRect.FromPoints(points);
    }

    /// <summary>The decoded value, interpreted using the character set declared by the symbol.</summary>
    public string Text { get; }

    /// <summary>The raw payload bytes before character set interpretation.</summary>
    public ReadOnlyMemory<byte> RawBytes { get; }

    /// <summary>The symbology the value was read from.</summary>
    public BbBarcodeFormat Format { get; }

    /// <summary>When the frame that produced this result was decoded.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Where the symbol is, in frame pixel coordinates. Matrix codes report their four outer
    /// corners clockwise from the symbol's own top-left, whichever way up it is held; linear
    /// codes report the two ends of the scanned line. Empty when the decoder could not supply
    /// positions.
    /// </summary>
    public IReadOnlyList<BbScanPoint> Corners { get; }

    /// <summary>The axis aligned bounding box of <see cref="Corners"/>, in frame pixel coordinates.</summary>
    public BbScanRect BoundingBox { get; }

    /// <summary>Symbology specific extras, or <see langword="null"/> when the symbology reports none.</summary>
    public BbBarcodeResultMetadata? Metadata { get; }

    /// <summary>Pixel dimensions of the frame the value was decoded from.</summary>
    public int FrameWidth { get; internal set; }

    /// <inheritdoc cref="FrameWidth" />
    public int FrameHeight { get; internal set; }

    /// <summary>
    /// Parses <see cref="Text"/> as a GS1 element string, for a symbol that declares one
    /// (<see cref="BbBarcodeResultMetadata.IsGs1"/>): GS1-128, GS1 DataMatrix, GS1 QR Code, GS1 Aztec and the GS1
    /// DataBar family. Parsed on the first call; <see cref="Text"/> and <see cref="RawBytes"/> stay as they are.
    /// </summary>
    /// <param name="elements">The elements, or <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="false"/> when the symbol is not a GS1 symbol, or when its content is not a valid element string
    /// - an AI this library does not know, a value of the wrong length or characters, a wrong check digit or an
    /// impossible date.
    /// </returns>
    public bool TryGetGs1([NotNullWhen(true)] out BbGs1ElementString? elements)
    {
        if (Metadata?.IsGs1 != true)
        {
            elements = null;
            return false;
        }

        if (_gs1 is null)
        {
            _gs1 = BbGs1ElementString.TryParse(Text, out var parsed) ? parsed : NotGs1;
        }

        elements = ReferenceEquals(_gs1, NotGs1) ? null : (BbGs1ElementString)_gs1;
        return elements is not null;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"{Format}: {Text}";
}
