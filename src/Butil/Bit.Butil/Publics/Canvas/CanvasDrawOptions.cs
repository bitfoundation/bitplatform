namespace Bit.Butil;

/// <summary>
/// Which part of the source to draw, and where on the canvas to put it - the nine-argument form of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/drawImage">drawImage</see>.
/// </summary>
/// <remarks>
/// Everything is optional: left alone, the whole source is stretched to fill the whole canvas.
/// Source coordinates are in the source's own intrinsic pixels, not in the size CSS displays it at.
/// </remarks>
public class CanvasDrawOptions
{
    /// <summary>Left edge of the region to take from the source. Defaults to 0.</summary>
    public double? SourceX { get; set; }

    /// <summary>Top edge of the region to take from the source. Defaults to 0.</summary>
    public double? SourceY { get; set; }

    /// <summary>Width of the region to take. Defaults to the source's full intrinsic width.</summary>
    public double? SourceWidth { get; set; }

    /// <summary>Height of the region to take. Defaults to the source's full intrinsic height.</summary>
    public double? SourceHeight { get; set; }

    /// <summary>Left edge on the canvas. Defaults to 0.</summary>
    public double? DestinationX { get; set; }

    /// <summary>Top edge on the canvas. Defaults to 0.</summary>
    public double? DestinationY { get; set; }

    /// <summary>Width to draw at. Defaults to the canvas's full width, which stretches the source to fit.</summary>
    public double? DestinationWidth { get; set; }

    /// <summary>Height to draw at. Defaults to the canvas's full height.</summary>
    public double? DestinationHeight { get; set; }
}
