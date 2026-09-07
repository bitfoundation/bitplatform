namespace Bit.BlazorUI;

/// <summary>Interaction mode used to determine which items are active on hover.</summary>
public enum BitChartInteractionMode
{
    /// <summary>Only the element under the pointer becomes active.</summary>
    Nearest,

    /// <summary>Every element sharing the hovered element's data index becomes active.</summary>
    Index,

    /// <summary>Every element of the hovered element's dataset becomes active.</summary>
    Dataset,

    /// <summary>Same as <see cref="Nearest"/>: the single element under the pointer.</summary>
    Point,

    /// <summary>Groups by position along the index axis, which for this renderer is the data index.</summary>
    X,

    /// <summary>Groups by position along the value axis; on a cartesian chart that is the data index.</summary>
    Y
}
