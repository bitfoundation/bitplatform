using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Bit.Blow;

/// <summary>How <see cref="BlowSerializer"/> writes and reads a document.</summary>
public sealed class BlowSerializerOptions
{
    private readonly Dictionary<string, Func<JsonElement, object?>> _nodeReaders = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, Func<object, JsonElement>> _writers = [];
    private Func<JsonElement, object?>? _edgeReader;

    /// <summary>Whether to indent the JSON. Off by default, which is smaller and what storage wants.</summary>
    public bool WriteIndented { get; set; }

    /// <summary>
    /// Whether to save which nodes and edges are selected. Off by default: selection is the state of an editing
    /// session, not of the graph, and saving it makes two saves of the same graph differ.
    /// </summary>
    public bool IncludeSelection { get; set; }

    /// <summary>The bounds an imported document must stay within. See <see cref="BlowImportLimits"/>.</summary>
    public BlowImportLimits Limits { get; set; } = new();

    /// <summary>
    /// The application's current document version, written into every document saved and expected of every document
    /// loaded. A document saved with an older version is brought up to date by <see cref="Migrations"/>; one saved by
    /// a newer version of the application is refused.
    /// </summary>
    public int CurrentVersion { get; set; }

    /// <summary>
    /// The steps that bring a document of an older <see cref="BlowDocument.Version"/> up to date, one version at a
    /// time. Each receives the whole document as JSON and returns it as the next version would have written it.
    /// </summary>
    public IList<BlowMigration> Migrations { get; } = [];

    /// <summary>
    /// Reads the data of every node of <paramref name="nodeType"/> as <typeparamref name="T"/>, and writes a node's
    /// data of type <typeparamref name="T"/> with the same contract. Pass a source-generated type info
    /// (<c>MyJsonContext.Default.TaskData</c>), which keeps the application trimmable and AOT-safe.
    /// </summary>
    /// <remarks>
    /// Data with no mapping is still saved when it is JSON already (a <see cref="JsonElement"/> or a
    /// <see cref="JsonNode"/>), a string, a number or a boolean, and it is loaded back as a <see cref="JsonElement"/>
    /// - so unknown data survives a load and a save untouched. Data of any other type with no mapping cannot be
    /// written, and saving it throws a <see cref="BlowSerializationException"/> naming the element.
    /// </remarks>
    public BlowSerializerOptions MapNodeData<T>(string nodeType, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(nodeType);
        ArgumentNullException.ThrowIfNull(typeInfo);
        _nodeReaders[nodeType] = element => element.Deserialize(typeInfo);
        return MapData(typeInfo);
    }

    /// <summary>
    /// Reads the data of every edge as <typeparamref name="T"/>, and writes edge data of type <typeparamref name="T"/>
    /// with the same contract. For graphs whose edges carry one kind of payload.
    /// </summary>
    public BlowSerializerOptions MapEdgeData<T>(JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        _edgeReader = element => element.Deserialize(typeInfo);
        return MapData(typeInfo);
    }

    /// <summary>
    /// Writes data of type <typeparamref name="T"/>, wherever it appears (nodes, edges, ports), with the given
    /// contract. Reading it back as <typeparamref name="T"/> needs <see cref="MapNodeData"/> or
    /// <see cref="MapEdgeData"/>; otherwise it is loaded as a <see cref="JsonElement"/>.
    /// </summary>
    public BlowSerializerOptions MapData<T>(JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        _writers[typeof(T)] = value => JsonSerializer.SerializeToElement((T)value, typeInfo);
        return this;
    }

    internal Func<JsonElement, object?>? NodeReader(string nodeType) => _nodeReaders.GetValueOrDefault(nodeType);

    internal Func<JsonElement, object?>? EdgeReader => _edgeReader;

    internal Func<object, JsonElement>? Writer(Type type) => _writers.GetValueOrDefault(type);

    internal static readonly BlowSerializerOptions Default = new();
}

/// <summary>
/// One step of bringing an older document up to date: <see cref="Migrate"/> receives a document of
/// <see cref="FromVersion"/> as JSON and returns it as version <c>FromVersion + 1</c> would have written it.
/// </summary>
/// <param name="FromVersion">The application document version this step upgrades from.</param>
/// <param name="Migrate">The upgrade. It may change the object it is given and return it.</param>
public sealed record BlowMigration(int FromVersion, Func<JsonObject, JsonObject> Migrate);

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

/// <summary>A graph could not be written: an element carries data no mapping says how to save.</summary>
public sealed class BlowSerializationException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BlowSerializationException(string message) : base(message) { }

    /// <summary>Creates the exception with the error that caused it.</summary>
    public BlowSerializationException(string message, Exception inner) : base(message, inner) { }
}
