namespace Bit.Bink;

/// <summary>How moving, resizing and drawing shapes snap to a grid and to other elements.</summary>
public sealed record BinkSnapOptions
{
    /// <summary>No snapping.</summary>
    public static readonly BinkSnapOptions None = new() { ToObjects = false, ToPage = false };

    /// <summary>Grid spacing in document units, or 0 for no grid.</summary>
    public float GridSize { get; init; }

    /// <summary>Whether edges and centres line up with those of other elements, showing a guide when they do.</summary>
    public bool ToObjects { get; init; } = true;

    /// <summary>Whether the page's edges and centre count as an element to line up with.</summary>
    public bool ToPage { get; init; } = true;

    /// <summary>How close, in view (CSS) pixels, an edge has to come to another before it snaps.</summary>
    public float Threshold { get; init; } = 6f;

    /// <summary>True when anything snaps.</summary>
    public bool IsEnabled => GridSize > 0 || ToObjects || ToPage;
}
