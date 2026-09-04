namespace Bit.Butil;

/// <summary>
/// The result of evaluating one media query - a snapshot, not the live object the browser keeps.
/// To follow it over time, subscribe through <see cref="Window.SubscribeMatchMedia"/> rather than
/// re-reading this.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaQueryList">MediaQueryList</see>
/// </summary>
public class MediaQueryList
{
    /// <summary>Whether the document matched the query at the moment it was evaluated.</summary>
    public bool Matches { get; set; }

    /// <summary>The query as the browser serialized it, which may be normalised from what was asked.</summary>
    public string Media { get; set; } = default!;
}
