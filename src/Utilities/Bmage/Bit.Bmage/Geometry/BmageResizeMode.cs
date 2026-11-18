using System.Drawing;

namespace Bit.Bmage;

/// <summary>Controls how an image is fitted into a target size when resizing.</summary>
public enum BmageResizeMode
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
