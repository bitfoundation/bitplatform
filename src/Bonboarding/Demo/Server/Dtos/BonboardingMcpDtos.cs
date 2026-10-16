namespace Bit.Bonboarding.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBonboardingGuideSection (and the bonboarding://guide resources); this record is how the
/// catalog, the search index and the completions describe a section to each other.
/// </summary>
public record BonboardingGuideSectionDto
{
    /// <summary>The heading text, e.g. "Conditional and branching flows".</summary>
    public required string Heading { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The owning level-2 section, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of the Bit.Bonboarding assembly.</summary>
public record BonboardingApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Bonboarding type.</summary>
public record BonboardingApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string stepId, CancellationToken cancellationToken = null)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bonboarding type.</summary>
public record BonboardingApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BonboardingApiMemberDto[] Members { get; init; }
}

/// <summary>A source file of the demo, retrievable through GetBonboardingSourceFile.</summary>
public record BonboardingSourceFileDto
{
    /// <summary>The path to pass to GetBonboardingSourceFile, e.g. "Demo/Client/Pages/Flows.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Page (a demo page with working tours), Hosting (the wiring of the app) or Shared.</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One CSS custom property of the shipped stylesheet.</summary>
public record BonboardingThemeVariableDto
{
    /// <summary>The property's name, e.g. "--bit-bonboarding-accent".</summary>
    public required string Name { get; init; }

    /// <summary>The group the stylesheet files it under, e.g. "Palette (light)" or "Typography".</summary>
    public required string Group { get; init; }

    /// <summary>The value under the default (light) theme.</summary>
    public required string LightValue { get; init; }

    /// <summary>The value under <c>data-bonboarding-theme="dark"</c>, when the dark theme overrides it.</summary>
    public string? DarkValue { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Bonboarding.</summary>
public record BonboardingSearchHitDto
{
    /// <summary>What was found: "Guide section", "API component", "API parameter", "Theme variable", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBonboarding answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BonboardingSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BonboardingSearchHitDto[] Hits { get; init; }

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

/// <summary>An axis-aligned rectangle in CSS viewport pixels.</summary>
public record BonboardingRectDto
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}

/// <summary>
/// Where Bit.Bonboarding's own placement engine puts a popover, for a target, a popover size and a
/// viewport - the answer the running library gives for the same measurements.
/// </summary>
public record BonboardingPlacementAnalysisDto
{
    /// <summary>The placement that was requested, after parsing.</summary>
    public required string Requested { get; init; }

    /// <summary>The side the popover ended up on: Top, Right, Bottom, Left or Center.</summary>
    public required string Side { get; init; }

    /// <summary>The alignment along the target's cross axis: Start, Center or End.</summary>
    public required string Align { get; init; }

    /// <summary>The final popover rectangle in viewport coordinates.</summary>
    public required BonboardingRectDto Popover { get; init; }

    /// <summary>The rectangle the popover was positioned against: the target grown by the spotlight padding.</summary>
    public BonboardingRectDto? Spotlight { get; init; }

    /// <summary>Whether an arrow is drawn.</summary>
    public required bool ShowArrow { get; init; }

    /// <summary>Distance from the popover's leading edge to the arrow's centre, in pixels.</summary>
    public double ArrowOffset { get; init; }

    /// <summary>True when the requested placement was used without falling back to another side.</summary>
    public required bool UsedPreferred { get; init; }

    /// <summary>Square pixels of the popover that could not be kept on screen; 0 when it fits cleanly.</summary>
    public required double Overflow { get; init; }

    /// <summary>What the numbers mean in practice, and the knob to turn when the outcome is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
