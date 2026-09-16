namespace Bit.BlazorUI;

/// <summary>
/// Marker clustering settings for <see cref="BitMap{TMapProvider}.Clustering"/>.
/// <para>
/// Clustering is done by BitMap itself, in screen space, above whichever provider is active - so
/// it behaves the same on all seven backends and needs no per-provider plugin. Markers that fall
/// within <see cref="RadiusPixels"/> of one another are replaced by a single bubble showing how
/// many they stand for; clicking one zooms to fit its members.
/// </para>
/// <para>
/// Reach for it once a map carries more than a few hundred markers. Past that, individual pins
/// stop being readable, every one is its own DOM node, and - if they are keyboard-reachable, as
/// they are by default - every one is also a tab stop.
/// </para>
/// </summary>
public sealed class BitMapClustering
{
    /// <summary>
    /// Grid size in screen pixels. Markers landing in the same cell become one cluster, so a
    /// larger radius means fewer, denser bubbles.
    /// </summary>
    public int RadiusPixels { get; set; } = 60;

    /// <summary>
    /// Zoom level at and above which every marker is drawn individually. Set it to the point where
    /// the markers stop overlapping in your data.
    /// </summary>
    public double MaxZoom { get; set; } = 16;

    /// <summary>
    /// Fewest markers a cell needs before it is drawn as a bubble. Below this the markers are drawn
    /// as themselves - a "cluster of 2" is rarely worth the extra click. Values below 2 are treated as 2.
    /// </summary>
    public int MinPoints { get; set; } = 2;

    /// <summary>Fill colour of the cluster bubble. Any CSS colour.</summary>
    public string Color { get; set; } = "#3388ff";

    /// <summary>Colour of the count drawn inside the bubble. Pick one that meets 4.5:1 against <see cref="Color"/>.</summary>
    public string TextColor { get; set; } = "#ffffff";

    /// <summary>
    /// Skip markers outside the current viewport entirely rather than handing them to the provider.
    /// This is most of the performance win on a large set; turn it off only if you need every marker
    /// present in the DOM regardless of where the map is looking.
    /// </summary>
    public bool CullOffscreen { get; set; } = true;

    /// <summary>
    /// Ceiling on how many individual markers may be drawn at once, applied past
    /// <see cref="MaxZoom"/> where clustering no longer thins the set. It is a guard against a
    /// pathological dataset locking up the browser, not a display choice.
    /// </summary>
    public int MaxRenderedMarkers { get; set; } = 2000;

    /// <summary>Padding in pixels applied when a cluster click zooms to fit its members.</summary>
    public int ExpandPaddingPixels { get; set; } = 48;

    /// <summary>
    /// Accessible name of a cluster bubble. <c>{0}</c> is replaced by how many markers it stands
    /// for.
    /// <para>
    /// A bubble is reachable by keyboard and drawn as a marker, so it needs a name of its own -
    /// and that name is user-facing text, which means it has to be translatable like every other
    /// label on this component.
    /// </para>
    /// </summary>
    public string AriaLabelFormat { get; set; } = "Cluster of {0} markers";

    /// <summary>
    /// Whether clicking a cluster zooms the map to fit the markers it stands for. Turn it off to
    /// handle the click yourself through <see cref="BitMap{TMapProvider}.OnClusterClick"/>.
    /// </summary>
    public bool ZoomOnClick { get; set; } = true;
}

/// <summary>Payload for <see cref="BitMap{TMapProvider}.OnClusterClick"/>.</summary>
public sealed class BitMapClusterClickArgs
{
    /// <summary>Generated identifier of the clicked cluster. Not stable across renders - do not persist it.</summary>
    public required string ClusterId { get; init; }

    /// <summary>
    /// How many of your markers the bubble stands for. Reported whether or not
    /// <see cref="BitMapClustering.ZoomOnClick"/> is on.
    /// </summary>
    public required int Count { get; init; }
}
