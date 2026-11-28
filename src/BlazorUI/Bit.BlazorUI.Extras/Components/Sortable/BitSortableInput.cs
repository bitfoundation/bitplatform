namespace Bit.BlazorUI;

/// <summary>How a drag of a <see cref="BitSortable{TItem}"/> list was made, as its events report it.</summary>
public enum BitSortableInput
{
    /// <summary>A mouse (or a trackpad).</summary>
    Mouse,

    /// <summary>A finger on a touch screen.</summary>
    Touch,

    /// <summary>A pen or stylus.</summary>
    Pen,

    /// <summary>The keyboard: the item was picked up, moved and dropped with keys.</summary>
    Keyboard,

    /// <summary>Code: a move made through the list's methods, such as <see cref="BitSortable{TItem}.MoveAsync"/>.</summary>
    Api
}
