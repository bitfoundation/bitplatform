namespace Bit.Bortable.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBortableGuideSection (and the bortable://guide resources); this record is how the
/// catalog, the search index and the completions describe a section to each other.
/// </summary>
public record BortableGuideSectionDto
{
    /// <summary>The heading text, e.g. "Shared lists".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBortableGuideSection for exactly this section: the heading itself, or - for a
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

/// <summary>A public type of the Bit.Bortable assembly.</summary>
public record BortableApiTypeDto
{
    /// <summary>The type's C# name, generic parameters included: "Bortable&lt;TItem&gt;", "BortablePull".</summary>
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Bortable type.</summary>
public record BortableApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(this IServiceCollection services)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bortable type.</summary>
public record BortableApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BortableApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBortableSourceFile.</summary>
public record BortableSourceFileDto
{
    /// <summary>The path to pass to GetBortableSourceFile, e.g. "Demo/Client/Pages/Shared.razor".</summary>
    public required string Path { get; init; }

    /// <summary>
    /// Page (a demo page, or a component one of them renders), Shared (what the pages share), Hosting
    /// (the wiring of the app) or Library (a file the package itself ships).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>The file's own summary, header comment or page introduction, when it has one.</summary>
    public string? Description { get; init; }

    /// <summary>The route the file is served at, for a routable page.</summary>
    public string? Route { get; init; }

    public required int Lines { get; init; }
}

/// <summary>
/// One CSS custom property of the stylesheet Bit.Bortable ships, read out of the file itself - so every
/// default here is the one a page gets by linking <c>bit-bortable.css</c>, not one someone wrote down.
/// </summary>
public record BortableThemePropertyDto
{
    /// <summary>The property, e.g. "--bit-bortable-accent-rgb".</summary>
    public required string Name { get; init; }

    /// <summary>
    /// "Declared on :root" for one of the inputs the stylesheet declares, or "Optional override" for one
    /// it never declares and only reads with a fallback, inside the state rule that paints with it.
    /// </summary>
    public required string Group { get; init; }

    /// <summary>The default: the value declared on <c>:root</c>, or the fallback the reading rule writes.</summary>
    public required string Value { get; init; }

    /// <summary>The other custom properties the default reads - the derived colours all read the accent.</summary>
    public required string[] Reads { get; init; }

    /// <summary>The custom properties whose default reads this one.</summary>
    public required string[] ReadBy { get; init; }

    /// <summary>What the stylesheet's own comments say about the property, or about the rule that reads it.</summary>
    public string? Comment { get; init; }

    /// <summary>The rules that paint with it, directly or through a fallback, as "selector { declaration }".</summary>
    public required string[] PaintedBy { get; init; }

    /// <summary>
    /// The component parameters whose default class those rules style, e.g. "Bortable&lt;TItem&gt;.ChosenClass":
    /// a list that sets one to a class of its own is no longer painted by them.
    /// </summary>
    public required string[] Parameters { get; init; }
}

/// <summary>One class of the stylesheet - the container, the item wrapper, or one of the drag states.</summary>
public record BortableStateClassDto
{
    /// <summary>The class name without its dot, e.g. "bit-bortable-ghost".</summary>
    public required string Name { get; init; }

    /// <summary>Which element carries it, in the README's own words, when the README's table names it.</summary>
    public string? SetOn { get; init; }

    /// <summary>The parameter whose default it is ("GhostClass"), or null for a class the component always renders under this name.</summary>
    public string? Parameter { get; init; }

    /// <summary>The rules that style it, as "selector { declarations }".</summary>
    public required string[] Rules { get; init; }

    /// <summary>What the stylesheet's comments say about those rules.</summary>
    public string? Comment { get; init; }

    /// <summary>The custom properties those rules paint with.</summary>
    public required string[] Properties { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Bortable.</summary>
public record BortableSearchHitDto
{
    /// <summary>What was found: "Guide section", "API component", "API parameter", "Theme property", "CSS class", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBortable answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BortableSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BortableSearchHitDto[] Hits { get; init; }

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

// ---- The drag lab: what goes in ----------------------------------------------------------

/// <summary>
/// What InspectBortableDrag reads: the lists, and the one drag to perform over them. Every name is
/// the camelCase of the Bortable parameter it sets, so the JSON reads like the markup it stands for.
/// </summary>
public record BortableDragScenarioDto
{
    /// <summary>The lists, each rendered as a real <c>Bortable&lt;string&gt;</c>, in this order.</summary>
    public BortableListSetupDto[]? Lists { get; init; }

    /// <summary>The drag to perform.</summary>
    public BortableDragGestureDto? Drag { get; init; }
}

/// <summary>One list: its items and the parameters that decide what a drag may do with them.</summary>
public record BortableListSetupDto
{
    /// <summary>The list's Id, reported as FromId and ToId.</summary>
    public string? Id { get; init; }

    /// <summary>The items, unique within the list (Blazor's @key).</summary>
    public string[]? Items { get; init; }

    public string? Group { get; init; }

    /// <summary>"move" (the default), "none" or "clone" - BortablePull.</summary>
    public string? Pull { get; init; }

    public bool? Put { get; init; }

    public bool? Sort { get; init; }

    public bool? Disabled { get; init; }

    public bool? Swap { get; init; }

    public bool? MultiDrag { get; init; }

    /// <summary>The items the list's Filter predicate returns true for - the locked ones.</summary>
    public string[]? Filter { get; init; }

    /// <summary>When set, the list's Clone factory is <c>item =&gt; item + cloneSuffix</c>.</summary>
    public string? CloneSuffix { get; init; }

    /// <summary>When true, Items is an array - a read-only IList.</summary>
    public bool? ReadOnly { get; init; }
}

/// <summary>One drag: what is picked up, where it goes, and how it ends.</summary>
public record BortableDragGestureDto
{
    /// <summary>The source list's id; may be left out when only one list holds the item.</summary>
    public string? From { get; init; }

    /// <summary>The item picked up.</summary>
    public string? Item { get; init; }

    /// <summary>Items of the source list clicked first, to select them for a multi-drag.</summary>
    public string[]? Selected { get; init; }

    /// <summary>The list the pointer is taken to; the source list when left out.</summary>
    public string? To { get; init; }

    /// <summary>Where the item should land in the target list, counted as the list will read after the drop.</summary>
    public int? Index { get; init; }

    /// <summary>The item of a swap list (<c>swap: true</c>) to trade places with.</summary>
    public string? SwapWith { get; init; }

    /// <summary>"drop" (the default: released over the target list) or "outside" (released anywhere else: dragend only).</summary>
    public string? Release { get; init; }
}

// ---- The drag lab: what comes out --------------------------------------------------------

/// <summary>One list as the drag found it and as it left it.</summary>
public record BortableListResultDto
{
    public required string Id { get; init; }

    public string? Group { get; init; }

    public required string[] Before { get; init; }

    public required string[] After { get; init; }

    /// <summary>True when the drag changed the list's contents or order.</summary>
    public required bool Changed { get; init; }

    /// <summary>How many times the list raised ItemsChanged - the @bind-Items notification.</summary>
    public required int ItemsChanged { get; init; }
}

/// <summary>An item wrapper (or the drop placeholder) as the list rendered it.</summary>
public record BortableRenderedItemDto
{
    /// <summary>The item; for the placeholder, the dragged item it previews.</summary>
    public required string Item { get; init; }

    /// <summary>The drag-state classes on the wrapper, beyond bit-bortable-item.</summary>
    public string? Classes { get; init; }

    /// <summary>True for the drop placeholder previewing where the dragged item will land.</summary>
    public bool? Placeholder { get; init; }

    /// <summary>False when the wrapper renders draggable="false": the browser would not start a drag from it.</summary>
    public bool? Draggable { get; init; }
}

/// <summary>A list's markup after a step: its item wrappers in DOM order, the placeholder included.</summary>
public record BortableRenderedListDto
{
    public required string List { get; init; }

    public required BortableRenderedItemDto[] Items { get; init; }
}

/// <summary>One DOM event the lab dispatched to a list, and what the list made of it.</summary>
public record BortableDragStepDto
{
    public required int Step { get; init; }

    /// <summary>"click", "dragstart", "dragover", "drop" or "dragend".</summary>
    public required string Event { get; init; }

    /// <summary>The element the event was dispatched to.</summary>
    public required string Target { get; init; }

    /// <summary>The calls the list made into the browser layer while handling it, with the lab's answer.</summary>
    public string[]? Browser { get; init; }

    /// <summary>
    /// Whether the list did what the step asked: a click toggled the selection, a dragstart started a
    /// drag, a dragover reordered, previewed or highlighted, a drop or dragend completed the drag.
    /// </summary>
    public required bool Accepted { get; init; }

    /// <summary>What happened, and - when nothing did - why not.</summary>
    public required string Outcome { get; init; }

    /// <summary>The lists whose markup this step changed, as they render after it.</summary>
    public BortableRenderedListDto[]? Rendered { get; init; }
}

/// <summary>One callback a list raised - a lifecycle event or the ItemsChanged of @bind-Items.</summary>
public record BortableDragEventDto
{
    /// <summary>The step that raised it.</summary>
    public required int Step { get; init; }

    /// <summary>"OnStart", "OnChange", "OnUpdate", "OnRemove", "OnAdd", "OnSort", "OnEnd" or "ItemsChanged".</summary>
    public required string Event { get; init; }

    /// <summary>The Id of the list it was raised on.</summary>
    public required string List { get; init; }

    public string? Item { get; init; }

    /// <summary>BortableEventArgs.Items - or, for ItemsChanged, the list's contents as handed to the binding.</summary>
    public string[]? Items { get; init; }

    public int? OldIndex { get; init; }

    public int? NewIndex { get; init; }

    public string? FromId { get; init; }

    public string? ToId { get; init; }

    public string? FromGroup { get; init; }

    public string? ToGroup { get; init; }

    public bool? CrossedLists { get; init; }
}

/// <summary>
/// What Bit.Bortable did with one drag - the answer the running component gives for the same lists
/// and the same gesture - step by step and event by event.
/// </summary>
public record BortableDragReportDto
{
    /// <summary>True when the input was read and the drag was run.</summary>
    public required bool Valid { get; init; }

    /// <summary>Why the input could not be run, when it could not.</summary>
    public string? Error { get; init; }

    /// <summary>The size of the input, in UTF-8 bytes.</summary>
    public required int InputBytes { get; init; }

    /// <summary>What the drag attempted: "reorder", "move", "clone" or "swap", prefixed "multi-drag" when it carried a selection.</summary>
    public string? Kind { get; init; }

    /// <summary>True when any list changed.</summary>
    public bool? Changed { get; init; }

    /// <summary>The drag's result in one sentence.</summary>
    public string? Outcome { get; init; }

    public BortableListResultDto[]? Lists { get; init; }

    public BortableDragStepDto[]? Steps { get; init; }

    /// <summary>Every callback the lists raised, in the order they raised them.</summary>
    public BortableDragEventDto[]? Events { get; init; }

    /// <summary>The Razor that declares these lists in an app.</summary>
    public string? Markup { get; init; }

    /// <summary>The scenario that was run when no json was passed - the input format, by example.</summary>
    public string? Sample { get; init; }

    /// <summary>What the outcome means in practice, and what to change when it is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
