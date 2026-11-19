namespace Bit.Bink;

/// <summary>Which way <see cref="BinkDocument.FlipElements"/> mirrors elements.</summary>
public enum BinkFlipDirection : byte
{
    /// <summary>Left becomes right, about a vertical line.</summary>
    Horizontal = 0,
    /// <summary>Top becomes bottom, about a horizontal line.</summary>
    Vertical = 1,
}
