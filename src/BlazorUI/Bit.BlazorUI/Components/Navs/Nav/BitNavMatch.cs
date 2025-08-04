namespace Bit.BlazorUI;

/// <summary>
/// Modifies the URL matching behavior for a <see cref="BitNav&lt;TItem&gt;"/>.
/// </summary>
public enum BitNavMatch
{
    /// <summary>
    /// Specifies that the nav item should be active when it matches exactly the current URL.
    /// </summary>
    Exact,

    /// <summary>
    /// Specifies that the nav item should be active when it matches any prefix of the current URL.
    /// </summary>
    Prefix,

    /// <summary>
    /// Specifies that the nav item should be active when its provided regex matches the current URL.
    /// </summary>
    Regex,

    /// <summary>
    /// Specifies that the nav item should be active when its provided wildcard matches the current URL.
    /// </summary>
    Wildcard
}
