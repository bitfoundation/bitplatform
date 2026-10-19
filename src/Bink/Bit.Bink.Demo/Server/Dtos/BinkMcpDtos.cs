namespace Bit.Bink.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBinkGuideSection (and the bink://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BinkGuideSectionDto
{
    /// <summary>The heading text, e.g. "Saving and loading".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBinkGuideSection for exactly this section: the heading itself, or - for the
    /// few headings the README uses twice, such as "Highlights" - "Parent > Heading".
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 or 4 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The heading this one sits under, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of the Bit.Bink assembly.</summary>
public record BinkApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Bink type.</summary>
public record BinkApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(BinkDocument document, BinkSvgExportOptions options = null)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bink type.</summary>
public record BinkApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BinkApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBinkSourceFile.</summary>
public record BinkSourceFileDto
{
    /// <summary>The path to pass to GetBinkSourceFile, e.g. "Demo/Client/Pages/Signature.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Page (a working demo page), Hosting (the wiring of the app), Library (a shipped file) or Shared.</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>
/// One brush the library registers by default, read off a freshly created instance - so every value
/// here is the one a caller gets by writing <c>new BinkPenBrush()</c>, not one someone wrote down.
/// </summary>
public record BinkBrushDto
{
    /// <summary>The serialized kind, e.g. "pen" - what a document stores and BinkBrushRegistry is keyed by.</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type that implements it, e.g. "BinkPenBrush".</summary>
    public required string TypeName { get; init; }

    public string? Summary { get; init; }

    /// <summary>The default ink colour, as CSS.</summary>
    public required string Color { get; init; }

    /// <summary>The default maximum width in document units (reached at full pressure).</summary>
    public required float Width { get; init; }

    public required float Opacity { get; init; }

    /// <summary>Normal, Multiply, Screen or DestinationOut (the pixel eraser).</summary>
    public required string BlendMode { get; init; }

    /// <summary>The pressure curve, e.g. "EaseInOut 0.2..1".</summary>
    public required string PressureCurve { get; init; }

    public required float Smoothing { get; init; }

    public required float Spacing { get; init; }

    public required bool SimulatePressure { get; init; }

    public required float TaperStart { get; init; }

    public required float TaperEnd { get; init; }

    /// <summary>
    /// How the brush turns points into geometry, found by asking it to build a stroke: "Fill" is a
    /// variable-width outline that follows pressure, "Centerline" a constant-width stroked path.
    /// </summary>
    public required string Geometry { get; init; }

    /// <summary>The line cap of that geometry: Round, Butt or Square.</summary>
    public required string LineCap { get; init; }

    /// <summary>The brush exactly as BinkSerializer writes it into a stroke, as indented JSON.</summary>
    public required string Serialized { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Bink.</summary>
public record BinkSearchHitDto
{
    /// <summary>What was found: "Guide section", "API component", "API parameter", "Brush", "CSS class", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBink answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BinkSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BinkSearchHitDto[] Hits { get; init; }

    /// <summary>True when the ranking was cut short at the requested limit, so a narrower query would show more.</summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// The words the query was actually searched by - punctuation, duplicates and filler words
    /// removed. Present only when nothing matched, which is the one time it explains something the
    /// caller could not work out from the query it just sent.
    /// </summary>
    public string[]? Terms { get; init; }

    /// <summary>Set only when Hits is empty: the reason, and the tool to fall back to.</summary>
    public string? Message { get; init; }

    /// <summary>Titles that nearly matched, when nothing matched outright - a query worth retrying with.</summary>
    public string[]? DidYouMean { get; init; }
}

/// <summary>An axis-aligned rectangle in document units.</summary>
public record BinkRectDto
{
    public required float X { get; init; }

    public required float Y { get; init; }

    public required float Width { get; init; }

    public required float Height { get; init; }
}

/// <summary>A name and how many times it occurs - an element type, a brush kind, a point encoding.</summary>
public record BinkCountDto
{
    public required string Name { get; init; }

    public required int Count { get; init; }
}

/// <summary>One layer of an inspected document.</summary>
public record BinkLayerReportDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required bool Visible { get; init; }

    public required bool Locked { get; init; }

    public required float Opacity { get; init; }

    /// <summary>The layer's elements, counted by their TypeName ("Stroke", "Rectangle", "Text", ...).</summary>
    public required BinkCountDto[] Elements { get; init; }
}

/// <summary>What the library's own server-side PNG path makes of the document.</summary>
public record BinkPngReportDto
{
    /// <summary>The width in pixels a page-region export at scale 1 produces.</summary>
    public required int Width { get; init; }

    /// <summary>The height in pixels a page-region export at scale 1 produces.</summary>
    public required int Height { get; init; }

    /// <summary>Text elements BinkPngExporter could not draw, because no IBinkTextRasterizer was supplied.</summary>
    public required int SkippedTextElements { get; init; }

    /// <summary>Images BinkPngExporter could not draw: anything but an embedded PNG data URI.</summary>
    public required int SkippedImages { get; init; }
}

/// <summary>
/// What Bit.Bink's own serializer makes of a document - the answer the running library gives for
/// the same JSON - plus what the exporters will do with it.
/// </summary>
public record BinkDocumentReportDto
{
    /// <summary>True when BinkSerializer.TryDeserialize accepted the document under the default (untrusted input) limits.</summary>
    public required bool Valid { get; init; }

    /// <summary>The serializer's own error message when the document was rejected.</summary>
    public string? Error { get; init; }

    /// <summary>The document's "format" field, which BinkSerializer writes as "bink".</summary>
    public string? Format { get; init; }

    /// <summary>The schema version the document declares.</summary>
    public int? SchemaVersion { get; init; }

    /// <summary>The schema versions this build of the library reads, e.g. "1..1".</summary>
    public required string SupportedVersions { get; init; }

    public string? Id { get; init; }

    /// <summary>The page size; 0 x 0 means an unbounded page.</summary>
    public float? Width { get; init; }

    public float? Height { get; init; }

    /// <summary>The page background, as CSS.</summary>
    public string? Background { get; init; }

    public int? MetadataEntries { get; init; }

    public BinkLayerReportDto[]? Layers { get; init; }

    /// <summary>Every element of the document counted by its TypeName.</summary>
    public BinkCountDto[]? Elements { get; init; }

    public long? TotalPoints { get; init; }

    /// <summary>The bounding box of the visible content, or null when there is none.</summary>
    public BinkRectDto? ContentBounds { get; init; }

    /// <summary>The brush kinds the strokes were written with, as stored - including kinds this build does not register.</summary>
    public BinkCountDto[]? BrushKinds { get; init; }

    /// <summary>How the strokes' points are encoded: "b64" (compact binary) or "json".</summary>
    public BinkCountDto[]? PointEncodings { get; init; }

    /// <summary>The size of the input, in UTF-8 bytes.</summary>
    public required int InputBytes { get; init; }

    /// <summary>The size BinkSerializer.Serialize writes with the default options (binary points, no indentation).</summary>
    public int? CompactBytes { get; init; }

    /// <summary>The size with BinkSerializerOptions.Readable (JSON points, indented).</summary>
    public int? ReadableBytes { get; init; }

    /// <summary>
    /// True when writing the loaded document, reading it back and writing it again gives the same
    /// text - the round trip an application performs every time it saves what it loaded.
    /// </summary>
    public bool? RoundTripStable { get; init; }

    /// <summary>What the server-side PNG exporter does with the page.</summary>
    public BinkPngReportDto? Png { get; init; }

    /// <summary>The document exported by BinkSvgExporter, when it was asked for.</summary>
    public string? Svg { get; init; }

    /// <summary>
    /// A small document written by BinkSerializer itself with readable points, returned when no
    /// document was passed in - the quickest way to see the format.
    /// </summary>
    public string? Sample { get; init; }

    /// <summary>What the numbers mean in practice, and what to change when the outcome is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
