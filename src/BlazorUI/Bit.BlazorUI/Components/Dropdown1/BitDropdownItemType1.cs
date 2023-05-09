namespace Bit.BlazorUI;

public enum BitDropdownItemType1

{
    /// <summary>
    /// Dropdown items are being rendered as a normal item
    /// </summary>
    Normal,

    /// <summary>
    /// Dropdown items are being rendered as a header, they cannot be selected
    /// </summary>
    Header,

    /// <summary>
    /// Dropdown items are being rendered as a divider, just draw a line
    /// </summary>
    Divider
}
