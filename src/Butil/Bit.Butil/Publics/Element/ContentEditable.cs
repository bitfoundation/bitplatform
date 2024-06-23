namespace Bit.Butil;

public enum ContentEditable
{

    /// <summary>
    /// Indicates that the element inherits its parent's editable status.
    /// </summary>
    Inherit,

    /// <summary>
    /// Indicates that the element is contenteditable.
    /// </summary>
    True,

    /// <summary>
    /// Indicates that the element cannot be edited.
    /// </summary>
    False,

    /// <summary>
    /// Indicates that the element's raw text is editable, but rich text formatting is disabled.
    /// </summary>
    PlainTextOnly
}
