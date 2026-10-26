using System.Globalization;

namespace Bit.Blow;

/// <summary>Inline styles shared by the built-in edge and <see cref="BlowBaseEdge"/>.</summary>
internal static class BlowEdgeStyles
{
    /// <summary>
    /// Holds an edge's invisible hit path at <paramref name="width"/> screen pixels whatever the zoom. The edge
    /// layer is scaled with the graph, so a width in flow units shrinks as the user zooms out - at 20% a 14px hit
    /// path is under 3px wide on screen. Dividing by the zoom the canvas publishes as a custom property undoes
    /// that in CSS, without the canvas re-rendering every edge on every zoom step.
    /// </summary>
    public static string InteractionWidth(double width) => string.Create(CultureInfo.InvariantCulture,
        $"stroke-width:calc({width}px / var(--bit-blow-zoom, 1));");
}
