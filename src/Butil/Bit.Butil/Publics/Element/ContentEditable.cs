namespace Bit.Butil;

/// <summary>
/// Whether an element's content can be edited in place.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Global_attributes/contenteditable">contenteditable</see>
/// </summary>
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
