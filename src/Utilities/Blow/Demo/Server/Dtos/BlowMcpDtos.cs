namespace Bit.Blow.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBlowGuideSection (and the blow://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BlowGuideSectionDto
{
    /// <summary>The heading text, e.g. "Saving and restoring".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBlowGuideSection for exactly this section: the heading itself, or - for a
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

/// <summary>A public type of the Bit.Blow assembly.</summary>
public record BlowApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Blow type.</summary>
public record BlowApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(BlowRect bounds, double padding = 0.1)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Blow type.</summary>
public record BlowApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BlowApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBlowSourceFile.</summary>
public record BlowSourceFileDto
{
    /// <summary>The path to pass to GetBlowSourceFile, e.g. "Demo/Client/Pages/Examples/SaveRestoreExample.razor".</summary>
    public required string Path { get; init; }

    /// <summary>
    /// Example (a page of the examples gallery), Page (a hand-written demo page), Shared (what the
    /// pages share), Hosting (the wiring of the app) or Library (a file the package itself ships).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>The file's own summary, header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    /// <summary>The route the file is served at, for a routable page.</summary>
    public string? Route { get; init; }

    public required int Lines { get; init; }
}

/// <summary>
/// One CSS custom property of the theme Bit.Blow ships, read out of its stylesheet - so every value
/// here is the one a page gets by linking <c>bit-blow.css</c>, not one someone wrote down.
/// </summary>
public record BlowThemePropertyDto
{
    /// <summary>The property, e.g. "--bit-blow-node-bg".</summary>
    public required string Name { get; init; }

    /// <summary>
    /// "Palette" for a property the light theme declares and the rules read, or "Dark palette" for
    /// one of the <c>--bit-blow-dark-*</c> values the dark colour mode copies over the palette.
    /// </summary>
    public required string Group { get; init; }

    /// <summary>The value the stylesheet gives it on <c>.bit-blow</c>.</summary>
    public required string Value { get; init; }

    /// <summary>
    /// For a palette property, what it resolves to under <c>ColorMode="BlowColorMode.Dark"</c> (and
    /// <c>System</c> on a dark OS) - null when dark mode leaves it as it is.
    /// </summary>
    public string? DarkValue { get; init; }

    /// <summary>The <c>--bit-blow-dark-*</c> property dark mode takes the value from, so overriding that one restyles dark mode.</summary>
    public string? DarkSource { get; init; }

    /// <summary>For a dark-palette property, the palette properties it is copied over in dark mode.</summary>
    public string[]? Feeds { get; init; }

    /// <summary>What the stylesheet's own comments say about the property, where they say something.</summary>
    public string? Comment { get; init; }

    /// <summary>The rules that paint with it, as "selector { declaration }".</summary>
    public required string[] PaintedBy { get; init; }

    /// <summary>The component parameters whose default value reads it, e.g. "BlowMiniMap.NodeColor".</summary>
    public required string[] Parameters { get; init; }

    /// <summary>The components whose own markup paints with it, e.g. "BlowBackground" for the pattern colour.</summary>
    public required string[] Components { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Blow.</summary>
public record BlowSearchHitDto
{
    /// <summary>What was found: "Guide section", "API component", "API parameter", "Theme property", "Example", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBlow answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BlowSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BlowSearchHitDto[] Hits { get; init; }

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

/// <summary>An axis-aligned rectangle in flow units.</summary>
public record BlowRectDto
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }
}

/// <summary>A pan/zoom transform: screen = flow * Zoom + (X, Y).</summary>
public record BlowViewportDto
{
    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Zoom { get; init; }
}

/// <summary>A name and how many times it occurs - a node type, an edge type.</summary>
public record BlowCountDto
{
    public required string Name { get; init; }

    public required int Count { get; init; }
}

/// <summary>One problem BlowCanvas raised through its OnError callback while it rendered the flow.</summary>
public record BlowFlowProblemDto
{
    /// <summary>The BlowErrorCode value, e.g. "missing-edge-endpoint".</summary>
    public required string Code { get; init; }

    /// <summary>The canvas's own message, exactly as an application's OnError handler receives it.</summary>
    public required string Message { get; init; }
}

/// <summary>A node that other nodes name as their parent, and those children.</summary>
public record BlowSubFlowDto
{
    public required string ParentId { get; init; }

    /// <summary>The parent's node type - usually "group".</summary>
    public required string ParentType { get; init; }

    /// <summary>How many parents sit above this one (0 for a root).</summary>
    public required int Depth { get; init; }

    /// <summary>The ids of the nodes whose ParentId is this node, in list order.</summary>
    public required string[] Children { get; init; }
}

/// <summary>Where a layout put one node.</summary>
public record BlowNodePositionDto
{
    public required string Id { get; init; }

    /// <summary>The node's parent, when it has one: X and Y are then relative to that parent's top-left.</summary>
    public string? ParentId { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    /// <summary>The side the layered layout turned the node's source handle to, when it did.</summary>
    public string? SourcePosition { get; init; }

    /// <summary>The side the layered layout turned the node's target handle to, when it did.</summary>
    public string? TargetPosition { get; init; }
}

/// <summary>What one of the library's own layouts made of the flow.</summary>
public record BlowLayoutReportDto
{
    /// <summary>The layout that ran, e.g. "layered-left-to-right".</summary>
    public required string Algorithm { get; init; }

    /// <summary>The C# that reproduces it on a live graph, one call per set of siblings.</summary>
    public required string Code { get; init; }

    /// <summary>How many sets of siblings were laid out: the root nodes, and each parent's children.</summary>
    public required int Groups { get; init; }

    /// <summary>The new position of every node that was laid out.</summary>
    public required BlowNodePositionDto[] Positions { get; init; }

    /// <summary>The bounds of the laid-out flow, resolved by the canvas the way it resolves the input.</summary>
    public BlowRectDto? Bounds { get; init; }
}

/// <summary>
/// What Bit.Blow makes of a flow - the answer the running library gives for the same nodes and
/// edges - plus, optionally, what one of its layouts would do with them.
/// </summary>
public record BlowFlowReportDto
{
    /// <summary>True when the JSON deserialized into a BlowSnapshot with System.Text.Json.</summary>
    public required bool Valid { get; init; }

    /// <summary>System.Text.Json's own message (with the JSON path) when the input could not be read.</summary>
    public string? Error { get; init; }

    /// <summary>The size of the input, in UTF-8 bytes.</summary>
    public required int InputBytes { get; init; }

    public int? Nodes { get; init; }

    public int? Edges { get; init; }

    /// <summary>Nodes with Hidden set: not rendered, not fitted, not laid out.</summary>
    public int? HiddenNodes { get; init; }

    /// <summary>Edges that are not drawn: hidden themselves, or running to a hidden node.</summary>
    public int? HiddenEdges { get; init; }

    /// <summary>Every node counted by its Type ("default", "input", "output", "group", or a custom one).</summary>
    public BlowCountDto[]? NodeTypes { get; init; }

    /// <summary>Every edge counted by its BlowEdgeType.</summary>
    public BlowCountDto[]? EdgeTypes { get; init; }

    /// <summary>
    /// BlowGraphUtils.GetNodesBounds over the visible nodes, after the canvas resolved their
    /// absolute positions (parents included) and took their measured sizes.
    /// </summary>
    public BlowRectDto? Bounds { get; init; }

    /// <summary>The viewport the input carried, when it carried one.</summary>
    public BlowViewportDto? Viewport { get; init; }

    /// <summary>Every node that is a parent, with its children - the sub-flow structure.</summary>
    public BlowSubFlowDto[]? SubFlows { get; init; }

    /// <summary>The deepest nesting of a node inside parents (0 when the flow is flat).</summary>
    public int? MaxDepth { get; init; }

    /// <summary>What BlowCanvas raised through OnError while rendering the flow - each distinct problem once.</summary>
    public BlowFlowProblemDto[]? Problems { get; init; }

    /// <summary>The layout that was asked for, when one was.</summary>
    public BlowLayoutReportDto? Layout { get; init; }

    /// <summary>A small flow written by the library, returned when no flow was passed - the format, by example.</summary>
    public string? Sample { get; init; }

    /// <summary>What the numbers mean in practice, and what to change when the outcome is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
