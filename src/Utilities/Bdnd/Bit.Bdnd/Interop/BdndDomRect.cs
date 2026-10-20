namespace Bit.Bdnd;

/// <summary>A DOMRect projected from JS.</summary>
public sealed class BdndDomRect
{
    /// <summary>The left edge, in CSS pixels relative to the viewport.</summary>
    public double Left { get; set; }

    /// <summary>The top edge, in CSS pixels relative to the viewport.</summary>
    public double Top { get; set; }

    /// <summary>The width, in CSS pixels.</summary>
    public double Width { get; set; }

    /// <summary>The height, in CSS pixels.</summary>
    public double Height { get; set; }
}
