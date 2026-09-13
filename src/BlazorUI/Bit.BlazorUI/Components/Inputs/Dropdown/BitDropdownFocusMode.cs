namespace Bit.BlazorUI;

/// <summary>
/// Which item the keyboard handling asks Dropdowns.ts to focus. It is internal because it is only the
/// wire format of that call; the mapping to the strings the script expects lives in
/// <see cref="DropdownsJsRuntimeExtensions.BitDropdownsFocusItem"/>.
/// </summary>
internal enum BitDropdownFocusMode
{
    /// <summary>
    /// The selected item, or the first one when nothing is selected.
    /// </summary>
    Selected,

    /// <summary>
    /// The first item of the list.
    /// </summary>
    First,

    /// <summary>
    /// The last item of the list.
    /// </summary>
    Last,

    /// <summary>
    /// The next item after the focused one.
    /// </summary>
    Next,

    /// <summary>
    /// The item before the focused one.
    /// </summary>
    Prev,

    /// <summary>
    /// A page further down the list.
    /// </summary>
    NextPage,

    /// <summary>
    /// A page further up the list.
    /// </summary>
    PrevPage,

    /// <summary>
    /// The first item matching the typed characters.
    /// </summary>
    Char
}
