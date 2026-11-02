namespace Bit.Bdnd.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBdndGuideSection (and the bdnd://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BdndGuideSectionDto
{
    /// <summary>The heading text, e.g. "Drop results".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBdndGuideSection for exactly this section: the heading itself, or - for a
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

/// <summary>A public type of the Bit.Bdnd assembly.</summary>
public record BdndApiTypeDto
{
    /// <summary>The C# name, generic parameters included: "BdndDraggable&lt;TItem&gt;".</summary>
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or constant) of a public Bit.Bdnd type.</summary>
public record BdndApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string handlerId, bool shallow = false)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bdnd type.</summary>
public record BdndApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BdndApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBdndSourceFile.</summary>
public record BdndSourceFileDto
{
    /// <summary>The path to pass to GetBdndSourceFile, e.g. "Demo/Client/Pages/Sortable/SortableCard.razor".</summary>
    public required string Path { get; init; }

    /// <summary>
    /// Page (a demo page, each one a working example), Shared (what the pages share), Hosting (the
    /// wiring of the app) or Library (a file the package itself ships).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>The heading a demo page shows its reader, e.g. "Sortable - Simple", when it has one.</summary>
    public string? Title { get; init; }

    /// <summary>The file's own summary, header comment or page introduction, when it has one.</summary>
    public string? Description { get; init; }

    /// <summary>The route the file is served at, for a routable page.</summary>
    public string? Route { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Bdnd.</summary>
public record BdndSearchHitDto
{
    /// <summary>What was found: "Guide section", "API component", "API parameter", "Setup guide", "Demo page", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or route.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBdnd answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BdndSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BdndSearchHitDto[] Hits { get; init; }

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

/// <summary>
/// What one drop target of the scenario reported - read off the <c>BdndDropState</c> the real
/// <c>BdndDropTarget</c> handed its child content, and off the callbacks it raised.
/// </summary>
public record BdndDropTargetReportDto
{
    /// <summary>The target's id in the scenario.</summary>
    public required string Id { get; init; }

    /// <summary>
    /// The position in <c>Targets</c> of the target this one is nested in, or null for a root target. A
    /// position rather than an id: ids may repeat, and a parent's id would be repeated for every child.
    /// </summary>
    public int? ParentIndex { get; init; }

    /// <summary>How many targets this one is nested in (0 for a root).</summary>
    public required int Depth { get; init; }

    /// <summary>
    /// The item types the target was rendered with (Accept and AcceptTypes combined, duplicates
    /// dropped, native names resolved to their <c>BdndNativeTypes</c> constants).
    /// </summary>
    public required string[] Accept { get; init; }

    /// <summary>True when the pointer is inside the target: it is the hovered one or one of its ancestors.</summary>
    public required bool Hovered { get; init; }

    /// <summary><c>BdndDropState.CanDrop</c> while the item hovered: the type matches and no CanDropPredicate said no - hovered or not.</summary>
    public required bool CanDrop { get; init; }

    /// <summary><c>BdndDropState.IsOver</c> while the item hovered: the pointer is over it or a target nested in it, and it accepts the type.</summary>
    public required bool IsOver { get; init; }

    /// <summary><c>BdndDropState.IsOverShallow</c> while the item hovered: over it, and over no hovered target nested in it.</summary>
    public required bool IsOverShallow { get; init; }

    /// <summary>The 1-based position at which this target's OnDrop (or, for a native drag, OnNativeDrop) ran; null when it did not run.</summary>
    public int? DropOrder { get; init; }

    /// <summary>What <c>args.Monitor.DidDrop()</c> answered inside this target's OnDrop - true when an inner target had already handled the drop.</summary>
    public bool? InnerAlreadyDropped { get; init; }

    /// <summary>The value this target's OnDrop published as <c>args.Result</c>, when it published one.</summary>
    public string? Result { get; init; }

    /// <summary>For a native drag: the <c>BdndNativeDropEventArgs.ItemType</c> its OnNativeDrop received.</summary>
    public string? NativeItemType { get; init; }
}

/// <summary>What the draggable's <c>OnEnd</c> received in its <c>BdndDragEndContext</c>.</summary>
public record BdndDragEndReportDto
{
    /// <summary><c>BdndDragEndContext.DidDrop</c>: a target accepted the drop. False for a drop on nothing, or on a target that cannot drop.</summary>
    public required bool DidDrop { get; init; }

    /// <summary><c>BdndDragEndContext.DropResult</c>: the result the drop ended with - the outermost non-null one.</summary>
    public string? DropResult { get; init; }
}

/// <summary>
/// What Bit.Bdnd does with one drag - the answer the running components give for the same draggable,
/// the same tree of drop targets and the same pointer position.
/// </summary>
public record BdndDropReportDto
{
    /// <summary>True when the scenario was read and the drag ran.</summary>
    public required bool Valid { get; init; }

    /// <summary>Why the scenario could not be run: the JSON parser's own message, or what the scenario is missing.</summary>
    public string? Error { get; init; }

    /// <summary>The size of the input, in UTF-8 bytes.</summary>
    public required int InputBytes { get; init; }

    /// <summary>The dragged item's type, as the draggable was rendered with it (a native type for an OS drag).</summary>
    public string? ItemType { get; init; }

    /// <summary>True for a native drag - files, text, a link or HTML from outside the app - which no BdndDraggable starts.</summary>
    public bool? Native { get; init; }

    /// <summary>The DataTransfer types the drag carried: the draggable's own <c>application/x-bit-bdnd-*</c> tag, or the native type token.</summary>
    public string[]? DataTransferTypes { get; init; }

    /// <summary>The <c>dataTransfer.effectAllowed</c> the draggable's JS registration was given - what the browser draws the cursor from.</summary>
    public string? EffectAllowed { get; init; }

    /// <summary>True when the drag began: the draggable recognised its own dragstart and its <c>BdndDragState.IsDragging</c> turned true.</summary>
    public bool? DragStarted { get; init; }

    /// <summary>True when the draggable's OnBegin ran.</summary>
    public bool? BeginFired { get; init; }

    /// <summary>The hovered targets from the outermost to the one under the pointer; empty when the item was dropped outside every target.</summary>
    public string[]? HoverPath { get; init; }

    /// <summary>
    /// True when the browser raises a drop event at all - when some target in the hovered chain can
    /// drop. With none, the browser shows the no-drop cursor and the drop never happens.
    /// </summary>
    public bool? DropFired { get; init; }

    /// <summary>The targets whose OnDrop (or OnNativeDrop) ran, in the order they ran.</summary>
    public string[]? DropOrder { get; init; }

    /// <summary>What the draggable's OnEnd received; null for a native drag or a drag that never began.</summary>
    public BdndDragEndReportDto? End { get; init; }

    /// <summary>Every target of the scenario, in document order.</summary>
    public BdndDropTargetReportDto[]? Targets { get; init; }

    /// <summary>A sample scenario, returned when no scenario was passed - the format, by example.</summary>
    public string? Sample { get; init; }

    /// <summary>What the numbers mean in practice, and what to change when the outcome is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
