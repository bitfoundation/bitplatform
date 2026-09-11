namespace Bit.Butil;

/// <summary>
/// One block of text found in a frame, mirroring the useful half of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/TextDetector/detect">TextDetector.detect()</see>'s
/// result.
/// </summary>
/// <remarks>
/// What counts as one block is the platform's decision - usually a line, sometimes a paragraph. The
/// bounding box is flattened into four numbers rather than a nested object so the whole result
/// serializes as a flat record, the same way <see cref="DetectedBarcode"/> does.
/// </remarks>
public class DetectedText
{
    /// <summary>The recognized text.</summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>The bounding box's left edge, in the source element's pixel coordinates.</summary>
    public double X { get; set; }

    /// <summary>The bounding box's top edge, in the source element's pixel coordinates.</summary>
    public double Y { get; set; }

    /// <summary>The bounding box's width, in the source element's pixel coordinates.</summary>
    public double Width { get; set; }

    /// <summary>The bounding box's height, in the source element's pixel coordinates.</summary>
    public double Height { get; set; }
}
