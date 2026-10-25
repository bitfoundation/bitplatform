namespace Bit.Blow;

/// <summary>
/// The names of the cascading values a <c>BlowCanvas</c> provides to what is rendered inside it, for add-ons of your
/// own that want to re-render only when something they show has changed.
/// </summary>
/// <remarks>
/// <para>
/// The canvas cascades itself twice. Unnamed, as an <see cref="IBlowContext"/> that every subscriber is re-rendered
/// with on every render of the canvas - simple, and fine for one or two add-ons. And under <see cref="Context"/>, as
/// a fixed value that re-renders nothing by itself; a component taking it pairs it with whichever of the version
/// numbers below describe what it shows. Each is an <c>int</c> that changes exactly when that part of the canvas
/// does, and Blazor re-renders a subscriber to a cascading <c>int</c> only when its value changes.
/// </para>
/// <code>
/// [CascadingParameter(Name = BlowCascadingNames.Context)] private IBlowContext? Flow { get; set; }
/// [CascadingParameter(Name = BlowCascadingNames.ViewportVersion)] private int ViewportVersion { get; set; }
/// </code>
/// <para>
/// This is what keeps a pan of a large graph cheap: the viewport moves, <see cref="ViewportVersion"/> changes, the
/// background, minimap and controls re-render, and no node, edge or handle does.
/// </para>
/// </remarks>
public static class BlowCascadingNames
{
    /// <summary>The canvas, as a fixed <see cref="IBlowContext"/>.</summary>
    public const string Context = "Bit.Blow.Context";

    /// <summary>Changes whenever the viewport (pan or zoom) or the pane's size changes.</summary>
    public const string ViewportVersion = "Bit.Blow.ViewportVersion";

    /// <summary>
    /// Changes whenever nodes may have moved or changed size - every frame of a drag or a resize - and whenever
    /// <see cref="GraphVersion"/> does.
    /// </summary>
    public const string GeometryVersion = "Bit.Blow.GeometryVersion";

    /// <summary>
    /// Changes on every render that may have changed anything other than the viewport and the nodes' geometry:
    /// selection, the node and edge lists, the canvas's parameters, a render of the component that hosts the canvas.
    /// </summary>
    public const string GraphVersion = "Bit.Blow.GraphVersion";

    /// <summary>Changes when a connection gesture starts or ends.</summary>
    public const string ConnectionVersion = "Bit.Blow.ConnectionVersion";
}
