namespace Bit.BlazorUI;

/// <summary>
/// A source was cited. Anchored citations attach to a span of a text part; unanchored ones are
/// appended to the message source list.
/// </summary>
public sealed record BitAiCitationEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="citation">The citation.</param>
    public BitAiCitationEvent(BitAiCitation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);
        Citation = citation;
    }

    /// <summary>The citation.</summary>
    public BitAiCitation Citation { get; }

    /// <inheritdoc />
    public override string Type => "citation";
}

/// <summary>
/// An image is part of the response.
/// </summary>
public sealed record BitAiImageEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="url">An absolute or data URL for the image.</param>
    public BitAiImageEvent(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        Url = url;
    }

    /// <summary>An absolute or data URL for the image.</summary>
    public string Url { get; }

    /// <summary>Alternative text. Always supply this: it is the accessible name of the image.</summary>
    public string? Alt { get; init; }

    /// <summary>Intrinsic width in pixels, used to reserve layout space and avoid reflow.</summary>
    public int? Width { get; init; }

    /// <summary>Intrinsic height in pixels, used to reserve layout space and avoid reflow.</summary>
    public int? Height { get; init; }

    /// <inheritdoc />
    public override string Type => "image";
}

/// <summary>
/// A file is part of the response.
/// </summary>
public sealed record BitAiFileEvent : BitAiPartScopedEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="attachment">The file.</param>
    public BitAiFileEvent(BitAiAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        Attachment = attachment;
    }

    /// <summary>The file.</summary>
    public BitAiAttachment Attachment { get; }

    /// <inheritdoc />
    public override string Type => "file";
}

/// <summary>
/// An artifact was created and will be filled in by subsequent delta events.
/// </summary>
public sealed record BitAiArtifactCreatedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="artifactId">Identifier, unique within the conversation.</param>
    /// <param name="kind">The artifact type, which selects a renderer.</param>
    /// <param name="title">The display title.</param>
    public BitAiArtifactCreatedEvent(string artifactId, BitAiArtifactKind kind, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentNullException.ThrowIfNull(title);
        ArtifactId = artifactId;
        Kind = kind;
        Title = title;
    }

    /// <summary>Identifier, unique within the conversation.</summary>
    public string ArtifactId { get; }

    /// <summary>The artifact type.</summary>
    public BitAiArtifactKind Kind { get; }

    /// <summary>The display title.</summary>
    public string Title { get; }

    /// <summary>Programming language or format hint.</summary>
    public string? Language { get; init; }

    /// <summary>Content supplied up front, for artifacts that are not streamed.</summary>
    public string? Content { get; init; }

    /// <summary>An addressable URL, for artifacts whose content is not inline text.</summary>
    public string? Url { get; init; }

    /// <summary>Application-defined values carried through to custom artifact renderers.</summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    /// <inheritdoc />
    public override string Type => "artifact.created";
}

/// <summary>
/// More content for an artifact.
/// </summary>
public sealed record BitAiArtifactDeltaEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="artifactId">The artifact being written.</param>
    /// <param name="delta">Content to append.</param>
    public BitAiArtifactDeltaEvent(string artifactId, string delta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentNullException.ThrowIfNull(delta);
        ArtifactId = artifactId;
        Delta = delta;
    }

    /// <summary>The artifact being written.</summary>
    public string ArtifactId { get; }

    /// <summary>Content to append.</summary>
    public string Delta { get; }

    /// <inheritdoc />
    public override string Type => "artifact.delta";
}

/// <summary>
/// An artifact is complete. A new version is committed to its history.
/// </summary>
public sealed record BitAiArtifactCompletedEvent : BitAiEvent
{
    /// <summary>Creates the event.</summary>
    /// <param name="artifactId">The finished artifact.</param>
    public BitAiArtifactCompletedEvent(string artifactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArtifactId = artifactId;
    }

    /// <summary>The finished artifact.</summary>
    public string ArtifactId { get; }

    /// <summary>A short description of what changed in this version.</summary>
    public string? Summary { get; init; }

    /// <inheritdoc />
    public override string Type => "artifact.completed";
}
