namespace Bit.Balette.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBaletteGuideSection (and the balette://guide resources); this record is how the
/// catalog, the search index and the completions describe a section to each other.
/// </summary>
public record BaletteGuideSectionDto
{
    /// <summary>The heading text, e.g. "Search and ranking".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBaletteGuideSection for exactly this section: the heading itself, or - for a
    /// heading the README uses more than once - "Parent > Heading".
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 or 4 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The heading this one sits under, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of the Bit.Balette assembly.</summary>
public record BaletteApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Balette type.</summary>
public record BaletteApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string query = null, string pageCommandId = null)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Balette type.</summary>
public record BaletteApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BaletteApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBaletteSourceFile.</summary>
public record BaletteSourceFileDto
{
    /// <summary>The path to pass to GetBaletteSourceFile, e.g. "Demo/Client/Pages/Commands.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Page (a working demo page), Shared (the commands, state and sources the pages share), Hosting (the wiring of the app) or Library (a shipped file).</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page introduction, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One CSS custom property of the shipped stylesheet.</summary>
public record BaletteThemeVariableDto
{
    /// <summary>The property's name, e.g. "--bit-balette-accent".</summary>
    public required string Name { get; init; }

    /// <summary>The group the stylesheet files it under, e.g. "Layout" or "Light palette".</summary>
    public required string Group { get; init; }

    /// <summary>The value under the default (light) theme, as declared on <c>:root</c>.</summary>
    public required string LightValue { get; init; }

    /// <summary>The value under the dark theme, when the dark rule overrides it.</summary>
    public string? DarkValue { get; init; }

    /// <summary>
    /// Where else the value is set: a stylesheet rule that redeclares the property for a state (the
    /// detail pane widens the dialog), or the component writing it inline from an option - in which
    /// case overriding it in CSS does not reach the elements under that inline declaration.
    /// </summary>
    public string[]? AlsoSetBy { get; init; }
}

/// <summary>
/// One element the shipped components mark with a <c>data-balette</c> attribute - the stable hook an
/// unstyled palette, an ItemTemplate or a test is written against.
/// </summary>
public record BaletteMarkupHookDto
{
    /// <summary>The attribute's value, e.g. "item" for <c>data-balette="item"</c>.</summary>
    public required string Value { get; init; }

    /// <summary>The element it sits on, e.g. "div" or "dialog".</summary>
    public required string Element { get; init; }

    /// <summary>The first <c>bit-balette-*</c> class the element carries, which the default stylesheet styles it by.</summary>
    public string? Class { get; init; }

    /// <summary>The other <c>data-*</c> attributes the element carries - its state, e.g. "data-active".</summary>
    public required string[] States { get; init; }

    /// <summary>The component whose markup renders it, e.g. "Balette" or "BaletteTrigger".</summary>
    public required string Component { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Balette.</summary>
public record BaletteSearchHitDto
{
    /// <summary>What was found: "Guide section", "API component", "API parameter", "Theme variable", "CSS class", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBalette answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BaletteSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BaletteSearchHitDto[] Hits { get; init; }

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

/// <summary>One term of an inspected query, the way the palette reads it.</summary>
public record BaletteQueryTermDto
{
    /// <summary>The term as it was typed, operator included, e.g. "!delete".</summary>
    public required string Text { get; init; }

    /// <summary>The text the term is matched by once its operator is taken off, e.g. "delete".</summary>
    public required string Term { get; init; }

    /// <summary>fuzzy, substring, exact, prefix, suffix or negate.</summary>
    public required string Operator { get; init; }

    /// <summary>True when the term only matches with the same case - a fuzzy term with a capital letter (smart case).</summary>
    public required bool CaseSensitive { get; init; }
}

/// <summary>What one term of the query scored against one command, and the field that earned it.</summary>
public record BaletteTermScoreDto
{
    public required string Term { get; init; }

    /// <summary>The term's weighted score: the best field's score times that field's weight. Null when the term failed, or is a negation.</summary>
    public double? Score { get; init; }

    /// <summary>The field that scored best: title, keyword 'x', subtitle, group, description or tag 'x'; for a negation, "not present".</summary>
    public string? Field { get; init; }
}

/// <summary>One command of an inspected search: whether it matched, how it scored, and where the palette lists it.</summary>
public record BaletteInspectedCommandDto
{
    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public string? Group { get; init; }

    /// <summary>The command's id, explicit or derived from its group and title.</summary>
    public required string Id { get; init; }

    /// <summary>True when every term of the query matched.</summary>
    public required bool Matched { get; init; }

    /// <summary>When it did not match: the term that failed, and why.</summary>
    public string? Reason { get; init; }

    /// <summary>True for an AlwaysShow command that is listed although it did not match; its score and rank are minus infinity, so both are left out.</summary>
    public bool? AlwaysShown { get; init; }

    /// <summary>The match score: the sum of every term's best weighted field score; 0 on the empty query.</summary>
    public double? Score { get; init; }

    /// <summary>
    /// What BaletteRanker sorted by: score + priority x PriorityWeight + ln(1 + frecency) x FrecencyWeight
    /// with a query, priority x 1000 + frecency without one.
    /// </summary>
    public double? Rank { get; init; }

    /// <summary>The command's place in the ranking, 1 being the best.</summary>
    public int? Order { get; init; }

    /// <summary>The row the palette shows it on, 1 being the first, once the ranking is grouped into headings. Null when it is not listed.</summary>
    public int? Row { get; init; }

    /// <summary>The command's Priority, which the rank adds in.</summary>
    public required int Priority { get; init; }

    /// <summary>What the match was made on, as the palette's own search lab says it: title, subtitle, keyword 'x', or group, description or tag.</summary>
    public string? MatchedOn { get; init; }

    /// <summary>The keyword shown under the title when a keyword scored best.</summary>
    public string? MatchedKeyword { get; init; }

    /// <summary>The characters of the title the palette highlights.</summary>
    public int[]? TitlePositions { get; init; }

    /// <summary>The characters of the subtitle the palette highlights.</summary>
    public int[]? SubtitlePositions { get; init; }

    /// <summary>The title with every highlighted run in [brackets], e.g. "[G]o to [S]ettings".</summary>
    public string? HighlightedTitle { get; init; }

    /// <summary>The subtitle with every highlighted run in [brackets].</summary>
    public string? HighlightedSubtitle { get; init; }

    /// <summary>What each term contributed, so a rank can be traced back to the field that earned it.</summary>
    public BaletteTermScoreDto[]? Terms { get; init; }
}

/// <summary>
/// What Bit.Balette's own matcher and ranker make of a query over a list of commands - the answer the
/// running palette gives for the same query - plus the order it lists them in.
/// </summary>
public record BaletteSearchInspectionDto
{
    /// <summary>The query split into terms, each with its operator.</summary>
    public required BaletteQueryTermDto[] Terms { get; init; }

    /// <summary>The matcher that ran: fuzzy, fuzzy-ignore-case or substring.</summary>
    public required string Matcher { get; init; }

    /// <summary>Where the commands came from: this demo's registry, or the list that was passed in.</summary>
    public required string Commands { get; init; }

    /// <summary>The ranking formula, with the weights it was computed with.</summary>
    public required string Ranking { get; init; }

    /// <summary>How many commands were searched.</summary>
    public required int Searched { get; init; }

    /// <summary>How many of them matched every term.</summary>
    public required int Matched { get; init; }

    /// <summary>The listed commands in the order the palette shows them, then the ones that did not match, in the order they were given.</summary>
    public required BaletteInspectedCommandDto[] Results { get; init; }

    /// <summary>True when Results was cut at the requested limit.</summary>
    public bool HasMore { get; init; }

    /// <summary>What the numbers mean in practice, and what to change when the outcome is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
