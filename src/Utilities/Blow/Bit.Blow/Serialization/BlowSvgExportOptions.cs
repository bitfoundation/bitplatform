namespace Bit.Blow;

/// <summary>Settings for <see cref="BlowSvgExport.ToSvg"/>.</summary>
public sealed class BlowSvgExportOptions
{
    /// <summary>Space around the graph, in flow units.</summary>
    public double Padding { get; set; } = 20;

    /// <summary>The image's accessible name, written as its <c>&lt;title&gt;</c> and <c>aria-label</c>.</summary>
    public string Title { get; set; } = "Flow diagram";

    /// <summary>The canvas's <c>NodeOrigin</c>: which point of a node its position names.</summary>
    public BlowXYPosition NodeOrigin { get; set; } = BlowXYPosition.Zero;

    /// <summary>Background colour; null leaves the image transparent.</summary>
    public string? Background { get; set; } = "#ffffff";

    /// <summary>Node fill, outline and text colours.</summary>
    public string NodeFill { get; set; } = "#ffffff";

    /// <inheritdoc cref="NodeFill"/>
    public string NodeStroke { get; set; } = "#1a192b";

    /// <inheritdoc cref="NodeFill"/>
    public string TextColor { get; set; } = "#222222";

    /// <summary>Fill and outline of nodes of type <c>group</c>.</summary>
    public string GroupFill { get; set; } = "#f3f3f8";

    /// <inheritdoc cref="GroupFill"/>
    public string GroupStroke { get; set; } = "#9aa0b5";

    /// <summary>Edge colour, for edges that name none of their own.</summary>
    public string EdgeStroke { get; set; } = "#b1b1b7";

    /// <summary>Font family and size of labels, in flow units.</summary>
    public string FontFamily { get; set; } = "sans-serif";

    /// <inheritdoc cref="FontFamily"/>
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// What routes <see cref="BlowEdgeType.Routed"/> edges: a <see cref="BlowRoutingOptions"/> or a router of the app's
    /// own; the built-in router with its defaults when null.
    /// </summary>
    public IBlowEdgeRouter? Routing { get; set; }
}
