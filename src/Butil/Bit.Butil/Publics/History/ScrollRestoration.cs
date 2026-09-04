namespace Bit.Butil;

/// <summary>
/// Whether the browser restores the scroll position when the user returns to a history entry.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration">History.scrollRestoration</see>
/// </summary>
public enum ScrollRestoration
{
    /// <summary>
    /// The location on the page to which the user has scrolled will be restored.
    /// </summary>
    Auto,

    /// <summary>
    /// The location on the page is not restored. The user will have to scroll to the location manually.
    /// </summary>
    Manual
}
