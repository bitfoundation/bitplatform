namespace Bit.Belemetry;

/// <summary>
/// A fully serialized export request. Self-contained so it can be persisted for a later retry, or handed to the
/// browser to send with <c>keepalive</c> while the page unloads.
/// </summary>
/// <remarks>
/// Credentials are not part of it while it stays in .NET: the owning exporter adds its configured headers (API keys)
/// and expiring tokens (Entra ID) when the request is actually sent (<see cref="IBelemetryHttpExporter.Headers"/>,
/// <see cref="IBelemetryHttpExporter.PrepareAsync"/>), so a request persisted to disk never holds a secret, and a
/// replayed one gets the exporter's current values. Only a request handed to the browser to send carries
/// <see cref="Headers"/> (<see cref="WithHeaders"/>).
/// </remarks>
internal sealed class BelemetryHttpRequest
{
    /// <summary>A unique id (also the file name of a persisted request).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The <see cref="IBelemetryExporter.Name"/> of the exporter that built it.</summary>
    public required string Exporter { get; set; }

    public required string Url { get; set; }

    public required string ContentType { get; set; }

    public string? ContentEncoding { get; set; }

    /// <summary>Headers to send with the request; set only when the browser sends it (see the remarks).</summary>
    public Dictionary<string, string>? Headers { get; set; }

    public required byte[] Body { get; set; }

    /// <summary>When the request was created, in Unix milliseconds (persisted requests expire by it).</summary>
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>A copy with <paramref name="headers"/> merged under its own <see cref="Headers"/>.</summary>
    internal BelemetryHttpRequest WithHeaders(IReadOnlyDictionary<string, string> headers)
    {
        var merged = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        if (Headers is not null)
        {
            foreach (var (name, value) in Headers) merged[name] = value;
        }

        return Copy(merged.Count > 0 ? merged : null);
    }

    /// <summary>A copy without any headers - what a durable store may keep.</summary>
    internal BelemetryHttpRequest WithoutHeaders() => Copy(null);

    private BelemetryHttpRequest Copy(Dictionary<string, string>? headers) => new()
    {
        Id = Id,
        Exporter = Exporter,
        Url = Url,
        ContentType = ContentType,
        ContentEncoding = ContentEncoding,
        Headers = headers,
        Body = Body,
        Time = Time,
    };
}
