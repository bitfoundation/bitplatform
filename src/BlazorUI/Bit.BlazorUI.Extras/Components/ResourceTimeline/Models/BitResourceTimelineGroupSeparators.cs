using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Separator bands drawn between resource groups, across the whole width of the
/// timeline: through the resource column and the content pane. Assign to
/// <see cref="BitResourceTimelineOptions.GroupSeparators"/>. A new instance
/// replaces the previous one entirely; an empty instance clears it, since a
/// separator with neither a <see cref="Height"/> nor a <see cref="BorderWidth"/>
/// has nothing to draw.
/// <para>
/// A separator is drawn wherever a group begins or ends among its siblings: before
/// a resource that has children (expanded or collapsed), and after the last row of
/// one, when the next row is a sibling of it or of one of its ancestors. Two leaf
/// resources next to each other are not separated, and neither is a group's own
/// row from its first child. Only groups at depth <see cref="MaxDepth"/> or less
/// are separated, so a hierarchy of regions, sites and machines can separate its
/// regions alone (the default) or its sites as well.
/// </para>
/// <para>
/// A separator takes room of its own between the rows it separates: it is
/// <see cref="Height"/> plus twice <see cref="BorderWidth"/> pixels tall, and the
/// rows below it move down by as much. The grid line it stands in for is not drawn.
/// Pointing into a separator hits no resource row. Visual only otherwise: the
/// separator is not a row, takes no keyboard focus and is not reported to the
/// <c>ResourceTemplate</c>, whose rows are spaced out by it in the same way.
/// </para>
/// </summary>
public class BitResourceTimelineGroupSeparators
{
    /// <summary>
    /// Height of the separator's band between its borders, in pixels, painted with
    /// <see cref="Background"/>. <c>null</c> is 0, so a separator made of borders
    /// alone is a <see cref="BorderWidth"/> and nothing else.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Height { get; set; }

    /// <summary>
    /// CSS color of the band between the borders. <c>null</c> is
    /// <see cref="BitResourceTimelineColors.AxisBorder"/>; use <c>"transparent"</c>
    /// for a gap that only spaces the groups apart, letting the content and the
    /// resource column show through.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Background { get; set; }

    /// <summary>
    /// Width of the border along the top and along the bottom of the band, in
    /// pixels. <c>null</c> is 0: no borders.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BorderWidth { get; set; }

    /// <summary>
    /// CSS color of the borders. <c>null</c> is
    /// <see cref="BitResourceTimelineColors.AxisBorder"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BorderColor { get; set; }

    /// <summary>
    /// How the borders are stroked: <see cref="BitLineStyle.Solid"/> (the default),
    /// <see cref="BitLineStyle.Dashed"/>, <see cref="BitLineStyle.Dotted"/> or
    /// <see cref="BitLineStyle.Double"/>. A double border needs a
    /// <see cref="BorderWidth"/> of at least 3 to have room for the gap between its
    /// two lines, and is drawn solid when it is thinner.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(JsonStringEnumConverter<BitLineStyle>))]
    public BitLineStyle? BorderStyle { get; set; }

    /// <summary>
    /// The deepest level of the hierarchy whose groups are separated: <c>0</c>
    /// separates the top-level resources alone, <c>1</c> their child groups as well,
    /// and so on. <c>null</c> is 0. A negative value separates nothing.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Whether the separators also cross the resource column, under its labels, so
    /// they run through the whole timeline. <c>false</c> keeps them to the content
    /// pane; the room they take is the same either way. <c>null</c> is <c>true</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShowInResourceAxis { get; set; }
}
