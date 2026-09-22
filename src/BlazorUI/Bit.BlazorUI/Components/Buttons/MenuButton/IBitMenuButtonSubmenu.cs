namespace Bit.BlazorUI;

// An item that owns an open submenu. The menu button keeps the open ones as a path - one per level,
// outermost first - so that opening a submenu closes whatever was open beside it, and closing the menu
// (or walking back out of it with the keyboard) closes the rest of the path from the inside out.
internal interface IBitMenuButtonSubmenu
{
    // The level of the menu the item itself lives in: zero for the menu the button opens, one more for
    // each submenu. The items of the submenu it owns are therefore at Level + 1.
    int Level { get; }

    // Gives focus back to the item the submenu was opened from, which is where the keyboard returns to
    // when the submenu is left (Escape, or the arrow key that walks back out of it).
    ValueTask FocusAsync();

    Task CloseSubmenuAsync();
}
