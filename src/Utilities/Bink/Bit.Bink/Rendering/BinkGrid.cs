namespace Bit.Bink;

/// <summary>
/// A grid drawn behind the drawing on <see cref="BinkCanvas"/>, as a guide. It is part of the view, not of the
/// document: it is never exported, saved or sent to collaborators, and it follows pan and zoom.
/// </summary>
/// <remarks>
/// When cells get too small on screen to be useful, multiples of the spacing are drawn instead, so zooming out never
/// turns the grid into a grey wash. To snap to the grid as well, set the same spacing as
/// <see cref="BinkSnapOptions.GridSize"/>.
/// </remarks>
public sealed record BinkGrid
{
    /// <summary>Light lines every 20 units, every fifth one stronger.</summary>
    public static readonly BinkGrid Default = new();

    /// <summary>Light dots every 20 units.</summary>
    public static readonly BinkGrid DottedDefault = new() { Style = BinkGridStyle.Dots, Color = new BinkColor(0, 0, 0, 64) };

    /// <summary>Lines or dots.</summary>
    public BinkGridStyle Style { get; init; } = BinkGridStyle.Lines;

    /// <summary>The distance between lines, in document units.</summary>
    public float Spacing { get; init; } = 20f;

    /// <summary>Every how many lines one is drawn in <see cref="MajorColor"/>; 0 for none. Lines only.</summary>
    public int MajorEvery { get; init; } = 5;

    /// <summary>The colour of the lines or dots. Translucent by default, so it suits any background.</summary>
    public BinkColor Color { get; init; } = new(0, 0, 0, 20);

    /// <summary>The colour of every <see cref="MajorEvery"/>-th line.</summary>
    public BinkColor MajorColor { get; init; } = new(0, 0, 0, 40);

    /// <summary>The smallest distance between lines on screen, in CSS pixels, before coarser multiples are drawn.</summary>
    public float MinScreenSpacing { get; init; } = 8f;
}
