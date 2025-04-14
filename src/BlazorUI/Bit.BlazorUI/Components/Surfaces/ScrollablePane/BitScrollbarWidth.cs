namespace Bit.BlazorUI;

public enum BitScrollbarWidth
{
    /// <summary>
    /// The default scrollbar width for the platform.
    /// </summary>
    Auto,

    /// <summary>
    /// A thin scrollbar width variant on platforms that provide that option, or a thinner scrollbar than the default platform scrollbar width.
    /// </summary>
    Thin,

    ///<summary>
    /// No scrollbar shown, however the element will still be scrollable.
    /// </summary>
    None

}
