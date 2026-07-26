namespace Bit.BlazorUI;

public enum BitDropdownSearchMode
{
    /// <summary>
    /// An item matches when its text contains the search text.
    /// </summary>
    Contains,

    /// <summary>
    /// An item matches when its text starts with the search text.
    /// </summary>
    StartsWith,

    /// <summary>
    /// An item matches when its text ends with the search text.
    /// </summary>
    EndsWith,

    /// <summary>
    /// An item matches when its text is equal to the search text.
    /// </summary>
    Equals
}
