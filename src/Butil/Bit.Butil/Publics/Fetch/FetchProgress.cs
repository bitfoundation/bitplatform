namespace Bit.Butil;

/// <summary>Progress event raised while a body is being received.</summary>
public class FetchProgress
{
    /// <summary>Bytes received so far.</summary>
    public long Loaded { get; set; }

    /// <summary>Total bytes expected, or null when unknown (chunked / no Content-Length).</summary>
    public long? Total { get; set; }
}
