using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>
/// Minimal request shape for <see cref="Fetch.Send"/>. Use a real <c>HttpClient</c> for normal API calls;
/// reach for this wrapper when you need browser-side features such as progress reporting, an
/// <see cref="AbortableFetch"/> handle, or fetch semantics like CORS / credentials.
/// </summary>
public class FetchRequest
{
    public string Url { get; set; } = string.Empty;

    /// <summary>HTTP verb. Defaults to GET.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Headers as a key/value dictionary. Repeating keys aren't supported here - use one comma-joined value.</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

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
}
