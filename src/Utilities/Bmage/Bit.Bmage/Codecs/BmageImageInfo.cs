using System.Drawing;

namespace Bit.Bmage;

/// <summary>Basic facts about an encoded image obtained without a full decode.</summary>
public sealed record BmageImageInfo(int Width, int Height, BmageImageFormat Format, BmageOrientation Orientation = BmageOrientation.Normal, bool? HasAlpha = null)
{
    /// <summary>The size as it will be displayed once orientation is applied.</summary>
    public Size DisplaySize => Orientation.Transform(new Size(Width, Height));
    public string MimeType => Format.GetMimeType();
}
