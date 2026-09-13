namespace Bit.Butil;

/// <summary>
/// One code found in a frame, mirroring the useful half of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/BarcodeDetector/detect">BarcodeDetector.detect()</see>'s
/// result.
/// </summary>
/// <remarks>
/// The bounding box is flattened into four numbers rather than a nested object so the whole result
/// serializes as a flat record. The corner-points array the underlying API also returns is left
/// out: it is only useful for drawing an exact outline, and it would be four more objects per
/// result on a hot scan loop.
/// </remarks>
public class DetectedBarcode
{
    /// <summary>The decoded contents - a URL, a product number, whatever was encoded.</summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>The symbology, e.g. <c>"qr_code"</c>, <c>"ean_13"</c>, <c>"code_128"</c>.</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>The bounding box's left edge, in the source element's pixel coordinates.</summary>
    public double X { get; set; }

    /// <summary>The bounding box's top edge, in the source element's pixel coordinates.</summary>
    public double Y { get; set; }

    /// <summary>The bounding box's width, in the source element's pixel coordinates.</summary>
    public double Width { get; set; }

    /// <summary>The bounding box's height, in the source element's pixel coordinates.</summary>
    public double Height { get; set; }
}
