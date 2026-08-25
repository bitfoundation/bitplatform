namespace Bit.Brouter.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBrouterGuideSection (and the brouter://guide resources); this record is how the
/// catalog, the search index and the completions describe a section to each other.
/// </summary>
public record BrouterGuideSectionDto
{
    /// <summary>The heading text, e.g. "Loader caching (stale-while-revalidate)".</summary>
    public required string Heading { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The owning level-2 section, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of the Bit.Brouter assembly.</summary>
public record BrouterApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Brouter type.</summary>
public record BrouterApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string url, bool replace = false)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Brouter type.</summary>
public record BrouterApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BrouterApiMemberDto[] Members { get; init; }
}

/// <summary>A source file of the demo/samples, retrievable through GetBrouterSourceFile.</summary>
public record BrouterSourceFileDto
{
    /// <summary>The path to pass to GetBrouterSourceFile, e.g. "Demo/Client/Pages/DataPage.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Demo (this documentation site) or Sample (the minimal hosting-model samples).</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Brouter.</summary>
public record BrouterSearchHitDto
{
    /// <summary>What was found: "Guide section", "Docs page", "API component", "API parameter", "Route constraint", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or category.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBrouter answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BrouterSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BrouterSearchHitDto[] Hits { get; init; }

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

/// <summary>One '/'-separated segment of a parsed route template.</summary>
public record BrouterTemplateSegmentDto
{
    /// <summary>The segment text as written in the template.</summary>
    public required string Value { get; init; }

    /// <summary>Literal, Parameter, CatchAll, Wildcard or Complex.</summary>
    public required string Kind { get; init; }

    /// <summary>Names of the parameters the segment binds (more than one for a complex segment).</summary>
    public string[]? ParameterNames { get; init; }

    /// <summary>Constraint tokens applied to the segment, in the order they are evaluated.</summary>
    public string[]? Constraints { get; init; }

    public bool IsOptional { get; init; }

    /// <summary>The declared default value, e.g. "Index" for <c>{action=Index}</c>.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>How specific the segment is - the router prefers the highest total when several routes match.</summary>
    public int Specificity { get; init; }
}

/// <summary>
/// The result of parsing one route template with Brouter's own parser.
/// <para>
/// The detail members - the segments, the parameter names, the notes - are filled in when a single
/// template was submitted, which is the call that is about that template. In a set they are left
/// out and <see cref="MatchOrder"/> and <see cref="Shape"/> take their place: that call is about
/// how the templates relate, and a set of two hundred would otherwise answer with the segments of
/// two hundred templates nobody asked about individually.
/// </para>
/// </summary>
public record BrouterTemplateInspectionDto
{
    public required string Template { get; init; }

    /// <summary>False when the template does not parse - Error then holds the reason the router would throw.</summary>
    public required bool IsValid { get; init; }

    public string? Error { get; init; }

    /// <summary>The template as the router stores it (leading/trailing slashes and "~/" removed).</summary>
    public string? NormalizedTemplate { get; init; }

    /// <summary>Sum of the segment specificities - the tie-breaker between two routes that both match.</summary>
    public int Specificity { get; init; }

    /// <summary>1 = the route the router prefers when several of them match the same URL. Only set within a set.</summary>
    public int? MatchOrder { get; init; }

    public string[]? ParameterNames { get; init; }

    public BrouterTemplateSegmentDto[]? Segments { get; init; }

    /// <summary>
    /// The template reduced to what the router actually tells apart - parameter names dropped, but
    /// constraints and declared defaults kept - mirroring the key the router itself uses to reject
    /// ambiguous registrations. Two routes sharing a shape are indistinguishable, so it is reported
    /// where comparing one template against another is the point, and left out otherwise.
    /// </summary>
    public string? Shape { get; init; }

    /// <summary>Notes about the template's behavior that are easy to get wrong (middle optionals, catch-alls, ...).</summary>
    public string[]? Notes { get; init; }
}

/// <summary>The result of parsing one route template, or of comparing a set of them.</summary>
public record BrouterRouteAnalysisDto
{
    /// <summary>The routes, most specific first - the order in which the router prefers them.</summary>
    public required BrouterTemplateInspectionDto[] Routes { get; init; }

    /// <summary>Groups of templates that match exactly the same URLs. Brouter throws at registration for these.</summary>
    public required string[][] Ambiguous { get; init; }

    public required string[] Notes { get; init; }

    /// <summary>
    /// True when more templates were sent than this server analyzes in one call, which makes every
    /// other member a partial answer - <see cref="Ambiguous"/> most of all, since it can only report
    /// collisions among the templates it was given. Treat such an answer as covering the first
    /// <see cref="AnalyzedTemplateCount"/> templates and nothing else.
    /// </summary>
    public bool IsPartial { get; init; }

    /// <summary>How many templates the call sent - present only when <see cref="IsPartial"/> is true.</summary>
    public int? SubmittedTemplateCount { get; init; }

    /// <summary>How many of them were analyzed - present only when <see cref="IsPartial"/> is true.</summary>
    public int? AnalyzedTemplateCount { get; init; }
}
