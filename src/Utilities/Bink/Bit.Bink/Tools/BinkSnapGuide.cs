namespace Bit.Bink;

/// <summary>An alignment guide: a line two edges or centres share while snapped, in document coordinates.</summary>
/// <param name="Vertical">True for a vertical line at <paramref name="Position"/> on the x axis.</param>
/// <param name="Position">Where the line is: an x for a vertical guide, a y for a horizontal one.</param>
/// <param name="From">Where the line starts along its own direction.</param>
/// <param name="To">Where the line ends along its own direction.</param>
public readonly record struct BinkSnapGuide(bool Vertical, float Position, float From, float To);
