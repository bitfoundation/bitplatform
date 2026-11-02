namespace Bit.BlazorUI;

/// <summary>
/// The type of an artifact, which selects a renderer.
/// </summary>
/// <remarks>
/// A string-backed value type rather than an <see langword="enum"/> so applications can introduce
/// artifact types the framework does not know about and register renderers for them. Comparison is
/// ordinal and case-insensitive.
/// </remarks>
[System.Text.Json.Serialization.JsonConverter(typeof(BitAiArtifactKindJsonConverter))]
public readonly record struct BitAiArtifactKind
{
    /// <summary>Prose, rendered as Markdown.</summary>
    public static readonly BitAiArtifactKind Document = new("document");

    /// <summary>Source code, rendered with syntax highlighting.</summary>
    public static readonly BitAiArtifactKind Code = new("code");

    /// <summary>A self-contained HTML page, rendered in a sandboxed frame.</summary>
    public static readonly BitAiArtifactKind Html = new("html");

    /// <summary>A JSON value, rendered as a collapsible tree.</summary>
    public static readonly BitAiArtifactKind Json = new("json");

    /// <summary>
    /// Tabular data, rendered as a table: a JSON array of objects (one row per object, one column per property), or
    /// comma-separated values - tab-separated when the language of the artifact is <c>tsv</c> - whose first row is the
    /// header. A JSON value that is not an array is shown as a JSON tree.
    /// </summary>
    public static readonly BitAiArtifactKind Table = new("table");

    /// <summary>
    /// A chart specification, rendered by the renderer the application registers for this kind; without one, its source
    /// is shown as code.
    /// </summary>
    public static readonly BitAiArtifactKind Chart = new("chart");

    /// <summary>
    /// A raster or vector image, shown from the URL of the artifact or, without one, from its content as an image source
    /// (a data URL).
    /// </summary>
    public static readonly BitAiArtifactKind Image = new("image");

    /// <summary>
    /// SVG markup, shown once it is complete as an image (an <c>img</c> element with the markup as a data URL, or the URL
    /// of the artifact when it has no content) rather than inlined into the page, so no script inside it ever runs. The
    /// markup itself is not altered.
    /// </summary>
    public static readonly BitAiArtifactKind Svg = new("svg");

    /// <summary>
    /// A diagram description, rendered by the renderer the application registers for this kind; without one, its source
    /// is shown as code.
    /// </summary>
    public static readonly BitAiArtifactKind Diagram = new("diagram");

    /// <summary>Creates an artifact kind from its wire value.</summary>
    /// <param name="value">The kind identifier. Must not be <see langword="null"/> or whitespace.</param>
    public BitAiArtifactKind(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>The underlying identifier, as it appears on the wire.</summary>
    public string Value { get; }

    /// <inheritdoc />
    public bool Equals(BitAiArtifactKind other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() => Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Converts a string to an <see cref="BitAiArtifactKind"/>.</summary>
    /// <param name="value">The kind identifier.</param>
    public static implicit operator BitAiArtifactKind(string value) => new(value);
}

/// <summary>
/// A substantial, self-contained piece of content produced alongside a conversation: a document, a
/// program, a table, a chart.
/// </summary>
/// <remarks>
/// Artifacts are addressed by id and versioned, so an assistant can revise one across several turns
/// while the UI keeps showing a single stable panel rather than a growing pile of code blocks.
/// </remarks>
public interface IBitAiArtifact
{
    /// <summary>Stable identifier, unique within a conversation.</summary>
    string ArtifactId { get; }

    /// <summary>The type of the artifact, which selects a renderer.</summary>
    BitAiArtifactKind Kind { get; }

    /// <summary>Display title.</summary>
    string Title { get; }

    /// <summary>The current content, as text. Binary artifacts use <see cref="Url"/> instead.</summary>
    string? Content { get; }

    /// <summary>Programming language or format hint, for example <c>csharp</c> or <c>csv</c>.</summary>
    string? Language { get; }

    /// <summary>An addressable URL, for artifacts whose content is not inline text.</summary>
    string? Url { get; }

    /// <summary>One-based version number, incremented on every revision.</summary>
    int Version { get; }

    /// <summary><see langword="true"/> while content is still streaming in.</summary>
    bool IsStreaming { get; }

    /// <summary>When the artifact was first created.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>When the current version was produced.</summary>
    DateTimeOffset UpdatedAt { get; }

    /// <summary>Application-defined values carried through to custom artifact renderers.</summary>
    IReadOnlyDictionary<string, object?>? Metadata { get; }
}

/// <summary>
/// An immutable snapshot of an artifact at one version.
/// </summary>
/// <param name="Version">The one-based version number.</param>
/// <param name="Content">The content at this version.</param>
/// <param name="CreatedAt">When this version was produced.</param>
/// <param name="Summary">An optional description of what changed.</param>
public sealed record BitAiArtifactVersion(int Version, string? Content, DateTimeOffset CreatedAt, string? Summary = null);
