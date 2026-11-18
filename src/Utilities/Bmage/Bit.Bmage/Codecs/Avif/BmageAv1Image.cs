namespace Bit.Bmage;

/// <summary>A decoded AV1 frame: its planes cropped to the frame size in use, and the colour description to read them by.</summary>
internal sealed class BmageAv1Image : IDisposable
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int BitDepth { get; init; }
    public required int SubX { get; init; }
    public required int SubY { get; init; }
    public required bool MonoChrome { get; init; }
    public required BmageAv1SequenceHeader Seq { get; init; }
    public required BmageAv1Plane[] Planes { get; init; }

    /// <summary>The frame's current_frame_id, by which a stream that numbers its frames names it to show it again.</summary>
    public int FrameId { get; init; }

    /// <summary>True when frames after this one were left undecoded because they use inter prediction.</summary>
    public bool SkippedFrames { get; set; }

    public void Dispose()
    {
        foreach (var plane in Planes) plane.Dispose();
    }
}
