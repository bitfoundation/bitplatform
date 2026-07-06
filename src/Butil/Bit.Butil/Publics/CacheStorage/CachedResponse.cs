using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>
/// Snapshot of a cached <c>Response</c> retrieved from <see cref="CacheStorage"/>.
/// </summary>
public class CachedResponse
{
    /// <summary>True when a response was found.</summary>
    public bool Found { get; set; }

    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>Body bytes. Empty for 204/304 or when the cache stored an opaque response.</summary>
    public byte[] Body { get; set; } = [];
}
