namespace Bit.Blow;

/// <summary>An element's bounding rectangle as read from the DOM, in client (screen) pixels.</summary>
/// <param name="Left">Distance from the viewport's left edge.</param>
/// <param name="Top">Distance from the viewport's top edge.</param>
/// <param name="Width">Rendered width.</param>
/// <param name="Height">Rendered height.</param>
public sealed record BlowDomRect(double Left, double Top, double Width, double Height);
