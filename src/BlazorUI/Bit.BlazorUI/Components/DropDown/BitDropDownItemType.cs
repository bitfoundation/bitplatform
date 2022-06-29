namespace Bit.BlazorUI;

public enum BitDropDownItemType
{
    /// <summary>
    /// DropDown items are being rendered as a normal item
    /// </summary>
    Normal,

    /// <summary>
    /// DropDown items are being rendered as a header, they cannot be selected
    /// </summary>
    Header,

    /// <summary>
    /// DropDown items are being rendered as a divider, just draw a line
    /// </summary>
    Divider
}
