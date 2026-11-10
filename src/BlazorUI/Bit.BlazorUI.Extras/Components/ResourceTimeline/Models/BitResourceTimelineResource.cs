using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// A single row on the timeline (for example a server, database or worker).
/// Resources can be nested into a multi-level hierarchy via <see cref="ParentId"/>:
/// a resource that has children renders as a collapsible group header (indented
/// by its depth) and can still own allocations of its own.
/// </summary>
public class BitResourceTimelineResource
{
    /// <summary>Stable identifier used to associate allocations with this resource.</summary>
    public required string Id { get; set; }

    /// <summary>Display label rendered on the sticky resource axis.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional <see cref="Id"/> of the parent resource. When set (and the parent
    /// exists), this resource is nested under it, forming a collapsible group.
    /// Root resources leave this <c>null</c>. Sibling and root order follows the
    /// order of the resources list.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentId { get; set; }

    /// <summary>
    /// Initial collapsed state for a resource that has children: when <c>true</c>,
    /// its descendants start hidden until the group is expanded. Ignored for
    /// resources without children. Defaults to <c>false</c> (expanded).
    /// </summary>
    public bool Collapsed { get; set; }

    /// <summary>
    /// CSS color painted behind this resource's row, under the grid and bars.
    /// Wins over <c>Options.Surface</c> row stripes and
    /// <see cref="BitResourceTimelineSurface.ResourceColors"/>. <c>null</c>
    /// leaves the row to those, or to the content background.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Background { get; set; }

    /// <summary>
    /// CSS color of the horizontal line along the top of this resource's row.
    /// Wins over <c>Options.AxisLines</c> row stripes and
    /// <see cref="BitResourceTimelineAxisLines.ResourceColors"/>. <c>null</c>
    /// leaves the line to those, or to <see cref="BitResourceTimelineColors.Grid"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LineColor { get; set; }
}
