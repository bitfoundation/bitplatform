namespace Bit.Belemetry;

/// <summary>
/// A fully serialized export request. Self-contained so it can be persisted for a later retry, or handed to the
/// browser to send with <c>keepalive</c> while the page unloads.
/// </summary>
/// <remarks>
/// Credentials that expire (Entra ID tokens) are not part of it: the owning exporter adds them when the request is
/// actually sent (<see cref="IBelemetryHttpExporter.PrepareAsync"/>).
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

    public Dictionary<string, string> Headers { get; set; } = [];

    public required byte[] Body { get; set; }

    /// <summary>When the request was created, in Unix milliseconds (persisted requests expire by it).</summary>
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
