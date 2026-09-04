using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>
/// Snapshot of a cached <c>Response</c> retrieved from <see cref="CacheStorage"/>.
/// </summary>
public class CachedResponse
{
    /// <summary>True when a response was found.</summary>
    public bool Found { get; set; }

    /// <summary>The HTTP status the response was cached with. 0 when nothing was found.</summary>
    public int Status { get; set; }

    /// <summary>The status text that went with <see cref="Status"/>.</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>The URL the response was cached against.</summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>The response headers. Empty for an opaque (cross-origin, no-cors) response, which hides them.</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>Body bytes. Empty for 204/304 or when the cache stored an opaque response.</summary>
    public byte[] Body { get; set; } = [];
}
