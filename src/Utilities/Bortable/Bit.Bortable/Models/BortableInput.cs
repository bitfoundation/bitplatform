namespace Bit.Bortable;

/// <summary>How a drag of a <see cref="Bortable{TItem}"/> list was made, as its events report it.</summary>
public enum BortableInput
{
    /// <summary>A mouse (or a trackpad).</summary>
    Mouse,

    /// <summary>A finger on a touch screen.</summary>
    Touch,

    /// <summary>A pen or stylus.</summary>
    Pen,

    /// <summary>The keyboard: the item was picked up, moved and dropped with keys.</summary>
    Keyboard,

    /// <summary>Code: a move made through the list's methods, such as <see cref="Bortable{TItem}.MoveAsync"/>.</summary>
    Api
}
