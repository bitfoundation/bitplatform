namespace Bit.Bmage;

/// <summary>Which part of the crop rectangle a drag is resizing.</summary>
public enum BmageCropHandle
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Right,
    BottomLeft,
    Bottom,
    BottomRight,
    /// <summary>The interior: moves the whole rectangle.</summary>
    Move,
}
