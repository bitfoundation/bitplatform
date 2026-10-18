namespace Bit.Bmage.Demo.Server.Dtos;

// The shapes every Bmage MCP tool answers with. They are records with documented members rather
// than loose dictionaries because the MCP client turns them into the tool's output schema: an agent
// that can see "Bytes" and "EncodeMilliseconds" as named, described fields does not have to infer
// what a number means, and cannot mistake one for another.

/// <summary>One of the Bit.Bmage guides: the README or a document under docs/.</summary>
public record BmageDocumentDto
{
    /// <summary>The name to pass to GetBmageDocument, e.g. "pipelines" or "readme".</summary>
    public required string Name { get; init; }

    /// <summary>The document's own title, its first heading.</summary>
    public required string Title { get; init; }

    /// <summary>What the document covers, in a sentence.</summary>
    public string? Summary { get; init; }

    /// <summary>Its level-2 headings, in order - each one a section GetBmageDocument can return alone.</summary>
    public required string[] Sections { get; init; }

    /// <summary>Number of markdown lines in the document.</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of one of the three Bit.Bmage assemblies.</summary>
public record BmageApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>The namespace to import, e.g. "Bit.Bmage.Pipeline".</summary>
    public required string Namespace { get; init; }

    /// <summary>The NuGet package that ships it: Bit.Bmage.Core, Bit.Bmage or Bit.Bmage.Editor.</summary>
    public required string Package { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method, event or enum value) of a public Bit.Bmage type.</summary>
public record BmageApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, EnumValue, Method or Event. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or a method's return type.</summary>
    public string? Type { get; init; }

    /// <summary>A method's parameter list, e.g. "(int maxWidth, int maxHeight, ResamplingFilter filter = ResamplingFilter.Auto)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member holds on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters and for required init properties.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bmage type.</summary>
public record BmageApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Namespace { get; init; }

    public required string Package { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public required string[] Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BmageApiMemberDto[] Members { get; init; }
}

/// <summary>The answer of GetBmageApiDetails: the type, or a message naming the near misses.</summary>
public record BmageApiDetailsResultDto
{
    public BmageApiTypeDetailsDto? Details { get; init; }

    /// <summary>Set instead of Details when no public type goes by the requested name.</summary>
    public string? Message { get; init; }
}

/// <summary>One finding from reviewing Bmage code.</summary>
public record BmageReviewFindingDto
{
    /// <summary>Error, Warning or Suggestion. An Error is code that will fail or leak at runtime.</summary>
    public required string Severity { get; init; }

    /// <summary>The rule that fired, e.g. "browser-call-during-prerender".</summary>
    public required string Rule { get; init; }

    /// <summary>The 1-based line of the reviewed code the finding sits on, when it maps to one.</summary>
    public int? Line { get; init; }

    /// <summary>What is wrong.</summary>
    public required string Message { get; init; }

    /// <summary>What to do about it.</summary>
    public required string Fix { get; init; }
}

/// <summary>The result of reviewing a piece of Bmage code.</summary>
public record BmageReviewDto
{
    /// <summary>True when nothing above a suggestion was found.</summary>
    public required bool Passed { get; init; }

    public required BmageReviewFindingDto[] Findings { get; init; }

    /// <summary>What was checked, so an empty result is not mistaken for an unchecked one.</summary>
    public required string[] RulesApplied { get; init; }
}

/// <summary>One rule the code review applies, and the library behaviour it is based on.</summary>
public record BmageReviewRuleDto
{
    public required string Id { get; init; }

    public required string Severity { get; init; }

    /// <summary>The mistake the rule catches, in a sentence.</summary>
    public required string Catches { get; init; }

    /// <summary>The guide the behaviour is documented in, e.g. "getting-started".</summary>
    public required string Source { get; init; }
}

/// <summary>A ready-made, copy-pasteable Bmage pattern.</summary>
public record BmageRecipeDto
{
    /// <summary>The id to pass to GetBmageRecipe, e.g. "avatar-crop".</summary>
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>What it is for, phrased the way the request usually arrives.</summary>
    public required string Intent { get; init; }

    /// <summary>Search terms the recipe covers.</summary>
    public required string Keywords { get; init; }

    /// <summary>Where it runs: "Browser", "Server" or "Anywhere".</summary>
    public required string RunsIn { get; init; }

    /// <summary>The Razor or C#. Present on GetBmageRecipe, omitted from the listing.</summary>
    public string? Code { get; init; }

    /// <summary>
    /// What to know before using it - the caveat that is not visible in the code. Present on
    /// GetBmageRecipe, omitted from the listing.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>The demo page that shows it running, e.g. "/avatar".</summary>
    public string? SeeAlso { get; init; }
}

/// <summary>The answer of GetBmageRecipe: the recipe, or a message naming the ones that exist.</summary>
public record BmageRecipeResultDto
{
    public BmageRecipeDto? Recipe { get; init; }

    /// <summary>Set instead of Recipe when no recipe goes by the requested id.</summary>
    public string? Message { get; init; }
}

/// <summary>One source file the MCP server can hand out verbatim.</summary>
public record BmageSourceFileDto
{
    /// <summary>The path to pass to GetBmageSourceFile.</summary>
    public required string Path { get; init; }

    /// <summary>"Demo page", "Demo" or "Host".</summary>
    public required string Kind { get; init; }

    /// <summary>What the file is. For a demo page, what the page demonstrates.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }

    /// <summary>The demo page's title, e.g. "Avatar editor". Null for a file that is not a demo page.</summary>
    public string? Title { get; init; }

    /// <summary>
    /// The route the demo page is served at, e.g. "avatar" - empty for the landing page, null for a
    /// file that is not a demo page.
    /// </summary>
    public string? Slug { get; init; }

    /// <summary>Space-separated search terms the demo page covers. Null for anything else.</summary>
    public string? Keywords { get; init; }
}

/// <summary>One hit from the unified search, carrying the call that returns its full text.</summary>
public record BmageSearchHitDto
{
    /// <summary>What kind of thing was found: "Guide section", "API class", "Recipe", "Review rule", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where it sits - the document, the owning type, the category.</summary>
    public string? Context { get; init; }

    /// <summary>The exact tool call that returns this hit in full.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>The result of running a pipeline on the real Bmage engine against a generated image.</summary>
public record BmagePipelineRunDto
{
    /// <summary>The pipeline as it was understood, written as the C# that builds it.</summary>
    public required string Pipeline { get; init; }

    /// <summary>The operations in the order they were written.</summary>
    public required string[] Operations { get; init; }

    /// <summary>
    /// The operations the optimiser actually runs for this input size - identities dropped, crops and
    /// orientations merged, per-pixel runs fused. This is what ExecuteAsync executes by default.
    /// </summary>
    public required string[] OptimizedOperations { get; init; }

    /// <summary>What the optimiser changed, and what stopped it from doing more.</summary>
    public required string[] OptimizerNotes { get; init; }

    /// <summary>The generated test image: "photo", "screenshot" or "graphic".</summary>
    public required string Source { get; init; }

    public required int SourceWidth { get; init; }

    public required int SourceHeight { get; init; }

    /// <summary>What GetOutputSize predicted without touching a pixel.</summary>
    public required string PredictedOutputSize { get; init; }

    public required int OutputWidth { get; init; }

    public required int OutputHeight { get; init; }

    /// <summary>Wall-clock time of ExecuteAsync with the optimiser on, on this server's desktop .NET.</summary>
    public required double ExecuteMilliseconds { get; init; }

    /// <summary>
    /// The same pipeline with the optimiser off, when the optimiser changed anything - the measured
    /// value of leaving it on. Null when the two runs are the same pipeline.
    /// </summary>
    public double? UnoptimizedMilliseconds { get; init; }

    /// <summary>Whether the output has any pixel with alpha below 255 - which a JPEG would flatten onto Background.</summary>
    public required bool OutputHasTransparency { get; init; }

    /// <summary>One measured encode per format and quality asked for, with the managed codecs.</summary>
    public required BmageEncodingDto[] Encodings { get; init; }

    /// <summary>The size-budget search, when maxFileSize was given.</summary>
    public BmageBudgetDto[]? Budgets { get; init; }

    /// <summary>What the numbers mean, in a few sentences, including how they transfer to the browser.</summary>
    public required string Reading { get; init; }

    /// <summary>Anything that was assumed, clamped or could not be measured here.</summary>
    public required string[] Warnings { get; init; }

    /// <summary>
    /// Set instead of a measurement when the pipeline could not be read or failed to run, and says
    /// why. A correctable mistake comes back as data rather than as a failed call.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>One measured encode.</summary>
public record BmageEncodingDto
{
    /// <summary>Jpeg, Png, Bmp - or WebP/Avif, which are reported as browser-only and not measured.</summary>
    public required string Format { get; init; }

    /// <summary>The quality asked for; null for a lossless format.</summary>
    public double? Quality { get; init; }

    /// <summary>The encoded size in bytes, or null when this format could not be encoded here.</summary>
    public long? Bytes { get; init; }

    /// <summary>Bits per output pixel - the number that carries over between image sizes.</summary>
    public double? BitsPerPixel { get; init; }

    public double? EncodeMilliseconds { get; init; }

    /// <summary>Why a format was not measured, or what to know about its number.</summary>
    public string? Note { get; init; }
}

/// <summary>The outcome of a MaxFileSize search for one format.</summary>
public record BmageBudgetDto
{
    public required string Format { get; init; }

    public required long MaxFileSize { get; init; }

    /// <summary>True when the encoder found settings that fit.</summary>
    public required bool Met { get; init; }

    public long? Bytes { get; init; }

    /// <summary>The quality the search settled on - reported on EncodedImage.Quality in real code.</summary>
    public double? Quality { get; init; }

    /// <summary>The dimensions it settled on; smaller than the output when the strategy had to shrink it.</summary>
    public int? Width { get; init; }

    public int? Height { get; init; }

    /// <summary>The ImageEncodeException message when the budget could not be met.</summary>
    public string? Error { get; init; }
}

/// <summary>What one metadata policy kept, read back from the file the real encoder wrote.</summary>
public record BmageMetadataPolicyDto
{
    /// <summary>Remove, StripSensitive or Preserve.</summary>
    public required string Policy { get; init; }

    public required string Format { get; init; }

    public required long Bytes { get; init; }

    /// <summary>The fields of the test photo that survived into the exported file.</summary>
    public required string[] Kept { get; init; }

    /// <summary>The fields that did not.</summary>
    public required string[] Removed { get; init; }

    /// <summary>True when the GPS position is still readable from the exported file.</summary>
    public required bool LeaksLocation { get; init; }

    /// <summary>True when something identifying the person - serial number, owner name - survived.</summary>
    public required bool LeaksIdentity { get; init; }
}

/// <summary>The answer of CompareBmageMetadataPolicies.</summary>
public record BmageMetadataComparisonDto
{
    public required string Format { get; init; }

    /// <summary>What the test photo carried before export, field by field.</summary>
    public required string[] Original { get; init; }

    public required BmageMetadataPolicyDto[] Policies { get; init; }

    public required string Reading { get; init; }

    /// <summary>Set when the format cannot be tested here, and says why.</summary>
    public string? Error { get; init; }
}

/// <summary>Everything this server exposes over MCP, for the demo page that documents it.</summary>
public record BmageMcpCatalogDto
{
    public required BmageMcpMemberDto[] Tools { get; init; }

    public required BmageMcpMemberDto[] Prompts { get; init; }

    public required BmageMcpMemberDto[] Resources { get; init; }
}

/// <summary>One tool, prompt or resource, read off the attribute that declares it.</summary>
public record BmageMcpMemberDto
{
    /// <summary>The tool or prompt name, or the resource's URI template.</summary>
    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>The parameters, as "name: type" with a "?" on the optional ones.</summary>
    public required string[] Parameters { get; init; }
}
