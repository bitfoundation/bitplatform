namespace Bit.BlazorUI;

/// <summary>
/// Represents the img referrerpolicy attribute values explained here:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Referrer-Policy"/>
/// </summary>
/// <remarks>
/// The policy decides how much of the address of the page the image sits on is sent to whoever serves
/// that image. It matters for an image loaded from another origin - a CDN, an avatar service, a
/// tracking pixel - since the full URL of the current page is otherwise handed to it.
/// </remarks>
public enum BitImageReferrerPolicy
{
    /// <summary>
    /// Sends no Referer header at all.
    /// </summary>
    NoReferrer,

    /// <summary>
    /// Sends the full URL, except to a less secure destination (HTTPS to HTTP), where nothing is sent.
    /// </summary>
    NoReferrerWhenDowngrade,

    /// <summary>
    /// Sends only the origin - the scheme, the host and the port - of the current page.
    /// </summary>
    Origin,

    /// <summary>
    /// Sends the full URL to the same origin, and only the origin to any other one.
    /// </summary>
    OriginWhenCrossOrigin,

    /// <summary>
    /// Sends the full URL to the same origin, and nothing at all to any other one.
    /// </summary>
    SameOrigin,

    /// <summary>
    /// Sends only the origin, and nothing to a less secure destination (HTTPS to HTTP).
    /// </summary>
    StrictOrigin,

    /// <summary>
    /// The default behavior: the full URL to the same origin, the origin alone to another secure one,
    /// and nothing to a less secure destination (HTTPS to HTTP).
    /// </summary>
    StrictOriginWhenCrossOrigin,

    /// <summary>
    /// Sends the full URL to every destination, secure or not. The path and the query string of the
    /// current page leave the origin with every request, so this is unsafe wherever either of them
    /// can carry anything private.
    /// </summary>
    UnsafeUrl
}
