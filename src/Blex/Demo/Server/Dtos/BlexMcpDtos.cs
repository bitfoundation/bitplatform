namespace Bit.Blex.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBlexGuideSection (and the blex://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BlexGuideSectionDto
{
    /// <summary>The heading text, e.g. "Cancellation and concurrency".</summary>
    public required string Heading { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The owning level-2 section, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of one of the Bit.Blex assemblies.</summary>
public record BlexApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    /// <summary>The package the type ships in: Bit.Blex, Bit.Blex.Blazor, Bit.Blex.Maui or Bit.Blex.Testing.</summary>
    public required string Package { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Blex type.</summary>
public record BlexApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(this IServiceCollection services, ServiceLifetime lifetime = Scoped)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Blex type.</summary>
public record BlexApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public required string Package { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BlexApiMemberDto[] Members { get; init; }
}

/// <summary>A source file of the demo/samples, retrievable through GetBlexSourceFile.</summary>
public record BlexSourceFileDto
{
    /// <summary>The path to pass to GetBlexSourceFile, e.g. "Demo/Client/Stores/CartStore.cs".</summary>
    public required string Path { get; init; }

    /// <summary>Demo (this documentation site) or Sample (the minimal standalone WebAssembly sample).</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Blex.</summary>
public record BlexSearchHitDto
{
    /// <summary>What was found: "Guide section", "Docs page", "API class", "API method", "Diagnostic", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or package.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBlex answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BlexSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BlexSearchHitDto[] Hits { get; init; }

    /// <summary>True when the ranking was cut short at the requested limit, so a narrower query would show more.</summary>
    public bool HasMore { get; init; }

    /// <summary>The words the query was actually searched by. Present only when nothing matched.</summary>
    public string[]? Terms { get; init; }

    /// <summary>Set only when Hits is empty: the reason, and the tool to fall back to.</summary>
    public string? Message { get; init; }

    /// <summary>Titles that nearly matched, when nothing matched outright - a query worth retrying with.</summary>
    public string[]? DidYouMean { get; init; }
}

/// <summary>One diagnostic reported while compiling the inspected source.</summary>
public record BlexInspectionDiagnosticDto
{
    /// <summary>The id, e.g. "BLEX001" for the Blex generator or "CS0103" for the C# compiler.</summary>
    public required string Id { get; init; }

    /// <summary>Error or Warning.</summary>
    public required string Severity { get; init; }

    public required string Message { get; init; }

    /// <summary>"generator" for a Blex diagnostic, "compiler" for one the C# compiler reports on the result.</summary>
    public required string Source { get; init; }

    /// <summary>1-based line of the inspected source the diagnostic points at, when it points into it.</summary>
    public int? Line { get; init; }
}

/// <summary>A public member the generator added to a store.</summary>
public record BlexGeneratedMemberDto
{
    /// <summary>Property or Method.</summary>
    public required string Kind { get; init; }

    public required string Name { get; init; }

    /// <summary>The member as C# would declare it, e.g. "int Count { get; set; }" or "void Increment()".</summary>
    public required string Signature { get; init; }
}

/// <summary>One [BlexStore] class found in the inspected source, and what the generator made of it.</summary>
public record BlexInspectedStoreDto
{
    /// <summary>The class name.</summary>
    public required string Name { get; init; }

    /// <summary>True when the generator emitted a partial for it - false when it was rejected.</summary>
    public required bool Generated { get; init; }

    /// <summary>The public members the generator added, in declaration order. Empty when nothing was generated.</summary>
    public required BlexGeneratedMemberDto[] Members { get; init; }
}

/// <summary>What InspectBlexStore answers: the verdict of the real Blex generator on a piece of source.</summary>
public record BlexStoreInspectionDto
{
    /// <summary>True when the generator reported no error and the generated code compiles without one.</summary>
    public required bool IsValid { get; init; }

    public required BlexInspectedStoreDto[] Stores { get; init; }

    /// <summary>Everything reported, generator diagnostics first.</summary>
    public required BlexInspectionDiagnosticDto[] Diagnostics { get; init; }

    public required string[] Notes { get; init; }

    /// <summary>The generated source, only when it was asked for.</summary>
    public string? GeneratedCode { get; init; }
}
