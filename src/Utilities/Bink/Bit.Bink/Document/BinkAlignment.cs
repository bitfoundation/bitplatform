namespace Bit.Bink;

/// <summary>Which edge or centre <see cref="BinkDocument.AlignElements"/> lines elements up on.</summary>
public enum BinkAlignment : byte
{
    /// <summary>Left edges.</summary>
    Left = 0,
    /// <summary>Horizontal centres, on one vertical line.</summary>
    HorizontalCenter = 1,
    /// <summary>Right edges.</summary>
    Right = 2,
    /// <summary>Top edges.</summary>
    Top = 3,
    /// <summary>Vertical centres, on one horizontal line.</summary>
    VerticalCenter = 4,
    /// <summary>Bottom edges.</summary>
    Bottom = 5,
}
