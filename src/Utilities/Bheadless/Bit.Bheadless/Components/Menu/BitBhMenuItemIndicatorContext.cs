namespace Bit.Bheadless;

/// <summary>Checked state cascaded from a checkable menu item to its indicator.</summary>
public sealed class BitBhMenuItemIndicatorContext
{
    internal BitBhMenuItemIndicatorContext(bool isChecked) => Checked = isChecked;

    /// <summary>Whether the item is checked.</summary>
    public bool Checked { get; }
}
