using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>Response payload from <see cref="Fetch.Send"/>.</summary>
public class FetchResponse
{
    /// <summary>True when the response status is in [200, 300).</summary>
    public bool Ok { get; set; }

    /// <summary>HTTP status (or 0 when the request was aborted/failed before headers).</summary>
    public int Status { get; set; }

    public string StatusText { get; set; } = string.Empty;

    /// <summary>Final URL after redirects.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Response headers.</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>Body bytes. May be empty for 204/304 or aborted responses.</summary>
    public byte[] Body { get; set; } = [];

    /// <summary>True when the request was aborted (via <see cref="AbortableFetch"/> or cancellation).</summary>
    public bool Aborted { get; set; }

    /// <summary>Network/CORS error description, when one occurred.</summary>
    public string? Error { get; set; }
}
