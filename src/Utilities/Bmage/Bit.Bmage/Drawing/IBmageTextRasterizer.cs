using System.Numerics;

namespace Bit.Bmage;

/// <summary>Turns text into vector outlines. Implementations differ in how they resolve fonts.</summary>
public interface IBmageTextRasterizer
{
    /// <summary>Measures the text without drawing it.</summary>
    BmageTextMetrics Measure(string text, BmageTextStyle style);

    /// <summary>Builds the outline of the text positioned at <paramref name="origin"/>, or null when nothing can be drawn.</summary>
    BmageVectorPath? GetTextPath(string text, Vector2 origin, BmageTextStyle style);
}
