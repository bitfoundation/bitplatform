namespace Bit.Butil;

/// <summary>
/// The <c>Request</c> half of the fetch object model: the URL plus the <c>init</c> options
/// <c>fetch()</c> takes. Use a real <c>HttpClient</c> for normal API calls; reach for this wrapper
/// when you need browser-side features such as progress reporting, an <see cref="AbortableFetch"/>
/// handle, or fetch semantics like CORS / credentials / priority.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request">https://developer.mozilla.org/en-US/docs/Web/API/Request</see>
/// </summary>
public class FetchRequest
{
    /// <summary>The URL to request. A relative URL resolves against the current document.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>HTTP verb. Defaults to GET.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// Request headers. Assigning a <c>Dictionary&lt;string, string&gt;</c> still works; use
    /// <see cref="FetchHeaders.Append(string, string)"/> when a name has to repeat.
    /// </summary>
    public FetchHeaders Headers { get; set; } = new();

    /// <summary>Optional body bytes. Set <see cref="Headers"/>'s <c>Content-Type</c> when needed.</summary>
    public byte[]? Body { get; set; }

    /// <summary>One of <c>"omit"</c>, <c>"same-origin"</c>, <c>"include"</c>.</summary>
    public string Credentials { get; set; } = "same-origin";

    /// <summary>One of <c>"cors"</c>, <c>"no-cors"</c>, <c>"same-origin"</c>, <c>"navigate"</c>.</summary>
    public string Mode { get; set; } = "cors";

    /// <summary>
    /// Cache mode: <c>"default"</c>, <c>"no-store"</c>, <c>"reload"</c>, <c>"no-cache"</c>,
    /// <c>"force-cache"</c>, <c>"only-if-cached"</c>.
    /// </summary>
    public string Cache { get; set; } = "default";

    /// <summary>One of <c>"follow"</c>, <c>"error"</c>, <c>"manual"</c>.</summary>
    public string Redirect { get; set; } = "follow";

    /// <summary>
    /// The referrer to send: a same-origin URL, <c>"about:client"</c> for the default, or an empty
    /// string to send none.
    /// </summary>
    public string? Referrer { get; set; }

    /// <summary>
    /// How much of the referrer to send: <c>"no-referrer"</c>,
    /// <c>"no-referrer-when-downgrade"</c>, <c>"origin"</c>, <c>"origin-when-cross-origin"</c>,
    /// <c>"same-origin"</c>, <c>"strict-origin"</c>, <c>"strict-origin-when-cross-origin"</c> or
    /// <c>"unsafe-url"</c>.
    /// </summary>
    public string? ReferrerPolicy { get; set; }

    /// <summary>
    /// A subresource integrity digest - <c>"sha256-..."</c> - that the response must match, or the
    /// fetch fails. Only meaningful for a response the browser can hash whole.
    /// </summary>
    public string? Integrity { get; set; }

    /// <summary>
    /// Lets the request outlive the page, for a beacon sent during unload. Capped at 64 KiB of body
    /// across all keepalive requests, and incompatible with a streamed body.
    /// </summary>
    public bool KeepAlive { get; set; }

    /// <summary>
    /// A scheduling hint: <c>"high"</c>, <c>"low"</c> or <c>"auto"</c>. Advisory - the browser
    /// decides, and ignores the field entirely where it is not implemented.
    /// </summary>
    public string? Priority { get; set; }
}
