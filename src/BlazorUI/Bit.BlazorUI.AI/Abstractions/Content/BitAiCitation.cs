namespace Bit.BlazorUI;

/// <summary>
/// Where a citation points: a web page, an uploaded document, a database row, and so on.
/// </summary>
public enum BitAiSourceKind
{
    /// <summary>Kind not stated.</summary>
    Unknown = 0,

    /// <summary>A page on the public web.</summary>
    Web = 1,

    /// <summary>A document in a corpus or knowledge base.</summary>
    Document = 2,

    /// <summary>A file the user attached to the conversation.</summary>
    File = 3,

    /// <summary>A row, record, or query result from structured data.</summary>
    Dataset = 4,

    /// <summary>A message earlier in this conversation.</summary>
    Message = 5,

    /// <summary>The output of a tool.</summary>
    Tool = 6,
}

/// <summary>
/// A referenceable source that one or more citations can point at.
/// </summary>
/// <remarks>
/// Sources are deduplicated by <see cref="SourceId"/> across a message, so several inline citations
/// that quote different passages of the same document share one entry in the source list and one
/// numbered marker.
/// </remarks>
public sealed record BitAiCitationSource
{
    /// <summary>Creates a source.</summary>
    /// <param name="sourceId">Stable identifier, used to deduplicate and to group citations.</param>
    /// <param name="title">Display title.</param>
    public BitAiCitationSource(string sourceId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(title);
        SourceId = sourceId;
        Title = title;
    }

    /// <summary>Stable identifier, used to deduplicate and to group citations.</summary>
    public string SourceId { get; }

    /// <summary>Display title.</summary>
    public string Title { get; init; }

    /// <summary>The kind of source, which selects the default source-card layout.</summary>
    public BitAiSourceKind Kind { get; init; } = BitAiSourceKind.Unknown;

    /// <summary>Absolute URL, when the source is addressable.</summary>
    public string? Url { get; init; }

    /// <summary>Host or publication name shown under the title.</summary>
    public string? SiteName { get; init; }

    /// <summary>URL of a favicon or thumbnail for the source card.</summary>
    public string? IconUrl { get; init; }

    /// <summary>A short description or excerpt used for the source preview.</summary>
    public string? Description { get; init; }

    /// <summary>Author or byline.</summary>
    public string? Author { get; init; }

    /// <summary>Publication date.</summary>
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Retrieval score, normalised to 0..1 where the retriever reports one.</summary>
    public double? Score { get; init; }

    /// <summary>Application-defined values carried through to custom source renderers.</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

/// <summary>
/// A claim in the response attributed to a <see cref="BitAiCitationSource"/>.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="StartIndex"/> and <see cref="EndIndex"/> are supplied they address a span of the
/// containing text part, which is what lets the Markdown renderer place an inline marker at exactly
/// the right position instead of appending footnotes at the end.
/// </para>
/// <para>
/// Offsets are measured in UTF-16 code units against the final text of the part. During streaming a
/// citation whose span extends past the text received so far is held back until the text catches up.
/// </para>
/// </remarks>
public sealed record BitAiCitation
{
    /// <summary>Creates a citation.</summary>
    /// <param name="citationId">Identifier unique within the message.</param>
    /// <param name="source">The source being cited.</param>
    public BitAiCitation(string citationId, BitAiCitationSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(citationId);
        ArgumentNullException.ThrowIfNull(source);
        CitationId = citationId;
        Source = source;
    }

    /// <summary>Identifier unique within the message.</summary>
    public string CitationId { get; }

    /// <summary>The source being cited.</summary>
    public BitAiCitationSource Source { get; init; }

    /// <summary>Identifier of the part the span refers to, when the citation is anchored.</summary>
    public string? PartId { get; init; }

    /// <summary>Start of the cited span, in UTF-16 code units.</summary>
    public int? StartIndex { get; init; }

    /// <summary>End of the cited span, exclusive, in UTF-16 code units.</summary>
    public int? EndIndex { get; init; }

    /// <summary>The exact text quoted from the source, shown in the citation preview.</summary>
    public string? Quote { get; init; }

    /// <summary>Page number, section, or other location within the source.</summary>
    public string? Locator { get; init; }

    /// <summary>
    /// One-based marker number assigned when the message is finalised, so that markers read
    /// <c>[1]</c>, <c>[2]</c> in document order regardless of the order events arrived.
    /// </summary>
    public int? Number { get; init; }

    /// <summary><see langword="true"/> when the citation addresses a span of text.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsAnchored => StartIndex is not null && EndIndex is not null && EndIndex > StartIndex;
}
