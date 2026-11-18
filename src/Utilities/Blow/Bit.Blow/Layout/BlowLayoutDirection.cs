namespace Bit.Blow;

/// <summary>Which way a layered layout flows.</summary>
public enum BlowLayoutDirection
{
    /// <summary>Roots at the top, each layer below the last.</summary>
    TopToBottom,

    /// <summary>Roots on the left, each layer to the right of the last.</summary>
    LeftToRight,

    /// <summary>Roots at the bottom, each layer above the last.</summary>
    BottomToTop,

    /// <summary>Roots on the right, each layer to the left of the last.</summary>
    RightToLeft,
}
