namespace Bit.Blow;

/// <summary>
/// The pan/zoom transform applied to the flow's viewport layer.
/// Screen = Flow * Zoom + (X, Y).
/// </summary>
public readonly record struct BlowViewport(double X, double Y, double Zoom)
{
    /// <summary>No pan, no zoom: flow space and screen space coincide.</summary>
    public static readonly BlowViewport Identity = new(0, 0, 1);

    /// <summary>Converts a screen-space point (relative to the pane) to flow space.</summary>
    public BlowXYPosition ScreenToFlow(double screenX, double screenY) =>
        new((screenX - X) / Zoom, (screenY - Y) / Zoom);

    /// <summary>Converts a flow-space point to screen space (relative to the pane).</summary>
    public BlowXYPosition FlowToScreen(BlowXYPosition flow) =>
        new(flow.X * Zoom + X, flow.Y * Zoom + Y);

    /// <summary>The CSS <c>transform</c> value that applies this viewport to the flow's node layer.</summary>
    public string ToTransform() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"translate({X}px, {Y}px) scale({Zoom})");
}
