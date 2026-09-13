namespace Bit.Butil;

/// <summary>The popover behaviour of an element - the values of its <c>popover</c> attribute.</summary>
public enum ElementPopover
{
    /// <summary>Not a popover.</summary>
    NotSet,

    /// <summary>Light-dismissed: opening it closes other auto popovers, and clicking away or pressing Escape closes it.</summary>
    Auto,

    /// <summary>Closed only by the code that opened it. Several can be open at once.</summary>
    Manual,

    /// <summary>Light-dismissed like <see cref="Auto"/>, but does not close other popovers - for tooltips over an open menu.</summary>
    Hint
}
