namespace Bit.Blow;

/// <summary>
/// The bounds an imported document has to stay within. A graph file is input like any other: one that is huge, nested
/// absurdly deep or holds a million nodes is refused before it can exhaust memory or freeze the page, rather than
/// trusted because it parses.
/// </summary>
public sealed class BlowImportLimits
{
    /// <summary>The largest document read, in bytes of UTF-8. 32 MB by default.</summary>
    public long MaxBytes { get; set; } = 32L * 1024 * 1024;

    /// <summary>The most nodes a document may hold. 50,000 by default.</summary>
    public int MaxNodes { get; set; } = 50_000;

    /// <summary>The most edges a document may hold. 100,000 by default.</summary>
    public int MaxEdges { get; set; } = 100_000;

    /// <summary>The most ports a node may declare. 256 by default.</summary>
    public int MaxPortsPerNode { get; set; } = 256;

    /// <summary>The deepest the JSON may nest - in node and edge data as anywhere else. 64 by default.</summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    /// The longest an id, a label, a type, a class or a style may be, in characters. 64 KB by default. Data payloads
    /// are the application's and are bounded only by <see cref="MaxBytes"/>.
    /// </summary>
    public int MaxStringLength { get; set; } = 64 * 1024;
}
