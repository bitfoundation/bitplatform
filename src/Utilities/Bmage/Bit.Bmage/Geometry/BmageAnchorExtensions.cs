using System.Drawing;

namespace Bit.Bmage;

/// <summary>Helpers for placing a size inside an area with an <see cref="BmageAnchor"/>.</summary>
public static class BmageAnchorExtensions
{
    /// <summary>Positions <paramref name="content"/> inside <paramref name="area"/> according to the anchor.</summary>
    public static Rectangle Place(this BmageAnchor anchor, Size content, Rectangle area)
    {
        var x = anchor switch
        {
            BmageAnchor.Left or BmageAnchor.TopLeft or BmageAnchor.BottomLeft => area.X,
            BmageAnchor.Right or BmageAnchor.TopRight or BmageAnchor.BottomRight => area.Right - content.Width,
            _ => area.X + (area.Width - content.Width) / 2,
        };
        var y = anchor switch
        {
            BmageAnchor.Top or BmageAnchor.TopLeft or BmageAnchor.TopRight => area.Y,
            BmageAnchor.Bottom or BmageAnchor.BottomLeft or BmageAnchor.BottomRight => area.Bottom - content.Height,
            _ => area.Y + (area.Height - content.Height) / 2,
        };
        return new Rectangle(x, y, content.Width, content.Height);
    }

    /// <summary>Positions <paramref name="content"/> inside an area of size <paramref name="area"/> at the origin.</summary>
    public static Rectangle Place(this BmageAnchor anchor, Size content, Size area) => anchor.Place(content, new Rectangle(Point.Empty, area));
}
