using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// How a row color and a column color share a cell.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitResourceTimelineSurfaceCombine>))]
public enum BitResourceTimelineSurfaceCombine
{
    /// <summary>
    /// Paint row fills, then column stripes on top. A translucent column color
    /// tints the row underneath; an opaque one covers it. The default.
    /// </summary>
    Overlay,

    /// <summary>Paint column stripes, then row fills on top.</summary>
    RowsOnTop,

    /// <summary>
    /// Paint each cell from <see cref="BitResourceTimelineColumnStripes.Colors"/>
    /// by (column index + row index), so two colors are a checkerboard that
    /// spans the whole surface. A resource's own background still paints that
    /// row solid. Row stripes are not used. When the cells would be thinner
    /// than a few pixels, or too many to paint, the frame falls back to
    /// full-height column stripes.
    /// </summary>
    Checker
}
