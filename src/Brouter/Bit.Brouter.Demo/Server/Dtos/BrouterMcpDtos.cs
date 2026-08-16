namespace Bit.Brouter.Demo.Server.Dtos;

/// <summary>One page of the documentation site (mirrors an entry of the client's DocsCatalog).</summary>
public record BrouterDocsPageDto
{
    /// <summary>The sidebar section the page belongs to, e.g. "Routing".</summary>
    public required string Section { get; init; }

    /// <summary>The value to pass to GetBrouterDocsPage. Empty string for the docs overview.</summary>
    public required string Slug { get; init; }

    /// <summary>The page's URL on the live documentation site.</summary>
    public required string Url { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    /// <summary>Space-separated search terms that the page covers - useful for picking the right slug.</summary>
    public required string Keywords { get; init; }
}

/// <summary>One heading of the library's README, which doubles as its reference guide.</summary>
public record BrouterGuideSectionDto
{
    /// <summary>The heading text, e.g. "Loader caching (stale-while-revalidate)". Pass it to GetBrouterGuideSection.</summary>
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

/// <summary>What GetBrouterApiDetails answers: the type's reference, or why there is none.</summary>
public record BrouterApiDetailsResultDto
{
    /// <summary>The full reference of the type, when a public type goes by the requested name.</summary>
    public BrouterApiTypeDetailsDto? Details { get; init; }

    /// <summary>Set instead of Details when nothing matched - it names the closest candidates.</summary>
    public string? Message { get; init; }
}

/// <summary>A route constraint usable inside a route template, e.g. <c>{id:int}</c>.</summary>
public record BrouterConstraintDto
{
    /// <summary>The constraint text as written in a template, e.g. "int" or "range(1,10)".</summary>
    public required string Token { get; init; }

    /// <summary>type (validates AND converts the bound value), validation (accepts/rejects, value stays a string), custom or chain.</summary>
    public required string Category { get; init; }

    public required string Rule { get; init; }

    public required string PassExample { get; init; }

    public required string FailExample { get; init; }

    /// <summary>A live URL of the documentation site that exercises the constraint with the passing example.</summary>
    public required string TryUrl { get; init; }
}

/// <summary>A source file of the demo/samples, retrievable through GetBrouterSourceFile.</summary>
public record BrouterSourceFileDto
{
    /// <summary>The path to pass to GetBrouterSourceFile, e.g. "Demo/Pages/DataPage.razor".</summary>
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

/// <summary>One URL builder emitted by the Bit.Brouter.Generators source generator.</summary>
public record BrouterTypedRouteDto
{
    /// <summary>The generated method, e.g. "Counter".</summary>
    public required string Method { get; init; }

    /// <summary>Its parameter list, e.g. "(int init)".</summary>
    public required string Signature { get; init; }

    /// <summary>The URL it builds for a sample argument set - what the method is for, shown rather than described.</summary>
    public string? ExampleUrl { get; init; }
}

/// <summary>The typed routes a project gets from the Bit.Brouter.Generators package.</summary>
public record BrouterTypedRoutesDto
{
    /// <summary>The assembly whose route declarations produced these builders.</summary>
    public required string GeneratedFor { get; init; }

    /// <summary>How the generator produces this class, and how to enable it.</summary>
    public required string HowItWorks { get; init; }

    public required BrouterTypedRouteDto[] Builders { get; init; }

    /// <summary>The constants under BrouterRoutes.Names - one per named route, for IBrouter.NavigateToName.</summary>
    public required Dictionary<string, string> Names { get; init; }
}

/// <summary>One route of an analyzed route table.</summary>
public record BrouterRouteTableEntryDto
{
    public required string Template { get; init; }

    public required bool IsValid { get; init; }

    public string? Error { get; init; }

    public int Specificity { get; init; }

    /// <summary>1 = the route the router prefers when several of them match the same URL.</summary>
    public int MatchOrder { get; init; }

    /// <summary>The template with parameter names dropped - two routes sharing a shape match exactly the same URLs.</summary>
    public string? Shape { get; init; }
}

/// <summary>The result of analyzing a set of route templates together.</summary>
public record BrouterRouteTableAnalysisDto
{
    /// <summary>The routes, most specific first - the order in which the router prefers them.</summary>
    public required BrouterRouteTableEntryDto[] Routes { get; init; }

    /// <summary>Groups of templates that match exactly the same URLs. Brouter throws at registration for these.</summary>
    public required string[][] Ambiguous { get; init; }

    public required string[] Notes { get; init; }
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

/// <summary>The result of parsing a route template with Brouter's own parser.</summary>
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

    public string[]? ParameterNames { get; init; }

    public BrouterTemplateSegmentDto[]? Segments { get; init; }

    /// <summary>Notes about the template's behavior that are easy to get wrong (middle optionals, catch-alls, ...).</summary>
    public string[]? Notes { get; init; }
}
