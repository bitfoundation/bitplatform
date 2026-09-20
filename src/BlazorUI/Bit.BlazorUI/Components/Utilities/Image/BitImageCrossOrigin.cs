namespace Bit.BlazorUI;

/// <summary>
/// Represents the img crossorigin attribute values explained here:
/// <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/img#crossorigin"/>
/// </summary>
/// <remarks>
/// Without the attribute a cross-origin image is fetched with no CORS at all, which is enough to
/// display it and not enough to read its pixels: it taints a canvas it is drawn into. Setting the
/// attribute is what makes an image from another origin usable by a canvas, a WebGL texture or a
/// service worker cache - and it makes the fetch fail outright where the other origin does not
/// answer with the matching CORS headers, so it is set where it is needed rather than by default.
/// </remarks>
public enum BitImageCrossOrigin
{
    /// <summary>
    /// Sends a cross-origin request with no credentials: no cookie, no client certificate and no
    /// HTTP authentication. This is the value nearly every case wants.
    /// </summary>
    Anonymous,

    /// <summary>
    /// Sends a cross-origin request with credentials - cookies, a client certificate, an
    /// Authorization header. The other origin has to answer with an Access-Control-Allow-Credentials
    /// header, and with its own origin rather than a wildcard, or the image is not loaded at all.
    /// </summary>
    UseCredentials
}
