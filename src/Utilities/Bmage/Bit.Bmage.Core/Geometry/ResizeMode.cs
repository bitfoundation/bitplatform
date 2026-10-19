using System.Drawing;

namespace Bit.Bmage.Geometry;

/// <summary>Controls how an image is fitted into a target size when resizing.</summary>
public enum ResizeMode
{
    /// <summary>Scale to exactly the target size, ignoring the aspect ratio.</summary>
    Stretch = 0,

    /// <summary>
    /// Scale proportionally so the whole image fits inside the target. The output is at most the target size
    /// and typically smaller in one dimension. This is the default.
    /// </summary>
    Fit,

    /// <summary>Like <see cref="Fit"/>, but the output is padded to exactly the target size (letterbox / pillarbox).</summary>
    Contain,

    /// <summary>Scale proportionally so the image completely covers the target, then crop the overflow. The output is exactly the target size.</summary>
    Cover,

    /// <summary>Like <see cref="Fit"/>, but never enlarges an image that already fits within the target.</summary>
    Max,

    /// <summary>Scale proportionally so both dimensions are at least the target size. Never shrinks below the target.</summary>
    Min,
}

/// <summary>Placement of content within a larger area.</summary>
public enum Anchor
{
    Center = 0,
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>Interpolation used for resampling.</summary>
public enum ResamplingFilter
{
    /// <summary>Choose automatically: area-averaging for downscaling, bicubic for upscaling.</summary>
    Auto = 0,
    /// <summary>Nearest neighbour. Fast, blocky; useful for pixel art.</summary>
    NearestNeighbor,
    /// <summary>Bilinear interpolation. Fast with moderate quality.</summary>
    Bilinear,
    /// <summary>Bicubic (Catmull-Rom style) interpolation. Good general purpose quality.</summary>
    Bicubic,
    /// <summary>Lanczos-3 windowed sinc. Highest quality, slowest.</summary>
    Lanczos3,
    /// <summary>Box (area averaging). Best for downscaling by large factors.</summary>
    Box,
}

/// <summary>Helpers for placing a size inside an area with an <see cref="Anchor"/>.</summary>
public static class AnchorExtensions
{
    /// <summary>Positions <paramref name="content"/> inside <paramref name="area"/> according to the anchor.</summary>
    public static Rectangle Place(this Anchor anchor, Size content, Rectangle area)
    {
        var x = anchor switch
        {
            Anchor.Left or Anchor.TopLeft or Anchor.BottomLeft => area.X,
            Anchor.Right or Anchor.TopRight or Anchor.BottomRight => area.Right - content.Width,
            _ => area.X + (area.Width - content.Width) / 2,
        };
        var y = anchor switch
        {
            Anchor.Top or Anchor.TopLeft or Anchor.TopRight => area.Y,
            Anchor.Bottom or Anchor.BottomLeft or Anchor.BottomRight => area.Bottom - content.Height,
            _ => area.Y + (area.Height - content.Height) / 2,
        };
        return new Rectangle(x, y, content.Width, content.Height);
    }

    /// <summary>Positions <paramref name="content"/> inside an area of size <paramref name="area"/> at the origin.</summary>
    public static Rectangle Place(this Anchor anchor, Size content, Size area) => anchor.Place(content, new Rectangle(Point.Empty, area));
}
