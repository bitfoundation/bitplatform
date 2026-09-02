namespace Bit.Butil;

/// <summary>
/// An element's text direction.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Global_attributes/dir">dir</see>
/// </summary>
public enum ElementDir
{
    /// <summary>
    /// The dir value is not set.
    /// </summary>
    NotSet,

    /// <summary>
    /// Left to right.
    /// </summary>
    Ltr,

    /// <summary>
    /// Right to left.
    /// </summary>
    Rtl,

    /// <summary>
    /// The direction of the element will be determined based on the contents of the element.
    /// </summary>
    Auto
}
