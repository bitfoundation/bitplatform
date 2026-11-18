namespace Bit.Bink;

/// <summary>The handles drawn around a selection.</summary>
public enum BinkSelectionHandle : byte
{
    /// <summary>No handle.</summary>
    None = 0,
    /// <summary>Top-left corner.</summary>
    TopLeft = 1,
    /// <summary>Top-center edge.</summary>
    Top = 2,
    /// <summary>Top-right corner.</summary>
    TopRight = 3,
    /// <summary>Right-center edge.</summary>
    Right = 4,
    /// <summary>Bottom-right corner.</summary>
    BottomRight = 5,
    /// <summary>Bottom-center edge.</summary>
    Bottom = 6,
    /// <summary>Bottom-left corner.</summary>
    BottomLeft = 7,
    /// <summary>Left-center edge.</summary>
    Left = 8,
    /// <summary>The rotation handle above the selection.</summary>
    Rotate = 9,
}
