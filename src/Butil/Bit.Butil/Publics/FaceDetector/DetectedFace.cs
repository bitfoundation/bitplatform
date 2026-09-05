namespace Bit.Butil;

/// <summary>
/// One face found in a frame, mirroring the useful half of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/FaceDetector/detect">FaceDetector.detect()</see>'s
/// result.
/// </summary>
/// <remarks>
/// Detection, not recognition: this says where a face is, never whose it is. Nothing identifying is
/// computed or returned.
/// <br/>
/// The bounding box is flattened into four numbers rather than a nested object so the whole result
/// serializes as a flat record, the same way <see cref="DetectedBarcode"/> does.
/// </remarks>
public class DetectedFace
{
    /// <summary>The bounding box's left edge, in the source element's pixel coordinates.</summary>
    public double X { get; set; }

    /// <summary>The bounding box's top edge, in the source element's pixel coordinates.</summary>
    public double Y { get; set; }

    /// <summary>The bounding box's width, in the source element's pixel coordinates.</summary>
    public double Width { get; set; }

    /// <summary>The bounding box's height, in the source element's pixel coordinates.</summary>
    public double Height { get; set; }

    /// <summary>
    /// The features the platform located within the face - eyes, nose, mouth. Often empty: which
    /// landmarks are reported (if any) is the platform's decision, not the browser's, so this differs
    /// between Android, macOS and Windows on the same browser version.
    /// </summary>
    public FaceLandmark[] Landmarks { get; set; } = [];
}
