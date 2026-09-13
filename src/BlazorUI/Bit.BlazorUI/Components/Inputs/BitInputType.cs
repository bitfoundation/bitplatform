namespace Bit.BlazorUI;

public enum BitInputType
{
    /// <summary>
    /// The input expects text characters.
    /// </summary>
    Text,

    /// <summary>
    /// The input expects password characters.
    /// </summary>
    Password,

    /// <summary>
    /// The input expects number characters.
    /// </summary>
    Number,

    /// <summary>
    /// The input expects email characters.
    /// </summary>
    Email,

    /// <summary>
    /// The input expects tel characters.
    /// </summary>
    Tel,

    /// <summary>
    /// The input expects url characters.
    /// </summary>
    Url,

    /// <summary>
    /// The input expects a search term, which is what lets a browser offer the previous searches of the
    /// same field and show its own clear affordance.
    /// </summary>
    Search
}
