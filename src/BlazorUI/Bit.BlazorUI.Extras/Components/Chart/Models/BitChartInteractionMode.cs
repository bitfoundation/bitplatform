namespace Bit.BlazorUI;

/// <summary>Interaction mode used to determine which items are active on hover.</summary>
public enum BitChartInteractionMode
{
    /// <summary>
    /// Only the single nearest element becomes active. With
    /// <see cref="BitChartInteractionOptions.Intersect"/> true that is the element directly under the
    /// pointer; with it false the pointer only has to be inside the plot area, and the element nearest
    /// to it activates.
    /// </summary>
    Nearest,

    /// <summary>Every element sharing the hovered element's data index becomes active.</summary>
    Index,

    /// <summary>Every element of the hovered element's dataset becomes active.</summary>
    Dataset,

    /// <summary>
    /// Same as <see cref="Nearest"/>: a single element, which is the one under the pointer only when
    /// <see cref="BitChartInteractionOptions.Intersect"/> is true.
    /// </summary>
    Point,

    /// <summary>Groups by position along the index axis, which for this renderer is the data index.</summary>
    X,

    /// <summary>
    /// Groups by each element's coordinate along the value axis. This renderer resolves it the same way
    /// as <see cref="Index"/>, so the hovered element's whole index group becomes active.
    /// </summary>
    Y
}
