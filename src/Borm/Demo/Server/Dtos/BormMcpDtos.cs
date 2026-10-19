namespace Bit.Borm.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBormGuideSection (and the borm://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BormGuideSectionDto
{
    /// <summary>The heading text, e.g. "Async and cascading options".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBormGuideSection for exactly this section: the heading itself, or - for a
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

/// <summary>A public type of Bit.Borm, or of its Bit.Borm.FluentValidation companion package.</summary>
public record BormApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    /// <summary>The NuGet package the type ships in: "Bit.Borm", or "Bit.Borm.FluentValidation" for the integration.</summary>
    public required string Package { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method or enum value) of a public Bit.Borm type.</summary>
public record BormApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string field, BormConditionOperator op, object value = null)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Borm type.</summary>
public record BormApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public required string Package { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BormApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBormSourceFile.</summary>
public record BormSourceFileDto
{
    /// <summary>The path to pass to GetBormSourceFile, e.g. "Demo/Client/Pages/CascadingOptionsDemo.razor".</summary>
    public required string Path { get; init; }

    /// <summary>
    /// Page (a demo page - a working form), Shared (the models, the validator and the custom renderer
    /// the pages share), Hosting (the wiring of the app) or Library (a file the package itself ships).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>The heading a page shows the person reading it, e.g. "Per-row conditions".</summary>
    public string? Title { get; init; }

    /// <summary>The file's own summary, header comment or page introduction, when it has one.</summary>
    public string? Description { get; init; }

    /// <summary>The route the file is served at, for a routable page.</summary>
    public string? Route { get; init; }

    public required int Lines { get; init; }
}

/// <summary>
/// One CSS custom property of the theme Bit.Borm ships, read out of its stylesheet - so every value
/// here is the one a page gets by linking <c>bit-borm.css</c>, not one someone wrote down.
/// </summary>
public record BormThemePropertyDto
{
    /// <summary>The property, e.g. "--bit-borm-accent".</summary>
    public required string Name { get; init; }

    /// <summary>The value the stylesheet gives it on <c>.bit-borm-form</c> - the light theme.</summary>
    public required string Value { get; init; }

    /// <summary>
    /// What it resolves to in the dark theme - under <c>prefers-color-scheme: dark</c> and under
    /// <c>data-theme="dark"</c> - or null when the dark theme leaves it as it is.
    /// </summary>
    public string? DarkValue { get; init; }

    /// <summary>
    /// Every other rule of the stylesheet that sets the property, as "selector" or "@media (...) selector":
    /// the rules an override has to match, one for one, to win in every colour mode.
    /// </summary>
    public required string[] SetBy { get; init; }

    /// <summary>The one property its value consists of (<c>var(--x)</c>), when it defaults to another property.</summary>
    public string? DefaultsTo { get; init; }

    /// <summary>The properties whose own value reads this one, so overriding it moves them too.</summary>
    public required string[] Feeds { get; init; }

    /// <summary>What the guide's table of custom properties says it is used for.</summary>
    public string? Purpose { get; init; }

    /// <summary>What the stylesheet's own comments say about the property, where they say something.</summary>
    public string? Comment { get; init; }

    /// <summary>The rules that paint with it, as "selector { declaration }".</summary>
    public required string[] PaintedBy { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Borm.</summary>
public record BormSearchHitDto
{
    /// <summary>What was found: "Guide section", "API class", "API method", "Theme property", "Demo page", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type, group or route.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBorm answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BormSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BormSearchHitDto[] Hits { get; init; }

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

/// <summary>A name and how many times it occurs - a field type.</summary>
public record BormCountDto
{
    public required string Name { get; init; }

    public required int Count { get; init; }
}

/// <summary>
/// One field of the form a schema becomes, as the importer built it - the answer the running library
/// gives for that part of the document.
/// </summary>
public record BormSchemaFieldDto
{
    /// <summary>
    /// The field's data path, e.g. "address.city". A repeater's row is written "lines[]", so
    /// "lines[].product" is the product field every row of "lines" has.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>The BormFieldType it renders as, e.g. "Select", "Array", "Tags".</summary>
    public required string Type { get; init; }

    /// <summary>The CLR type its value is stored as, when the document pins one (an integer enum, a boolean, a number).</summary>
    public string? ValueType { get; init; }

    public string? Label { get; init; }

    /// <summary>True when the field is always required. A conditional requirement is in RequiredWhen.</summary>
    public bool Required { get; init; }

    /// <summary>True when the field is always read-only (absent otherwise). A conditional lock is in ReadOnlyWhen.</summary>
    public bool? ReadOnly { get; init; }

    /// <summary>The id of the wizard step that shows the field, for a top-level field of a wizard.</summary>
    public string? Step { get; init; }

    /// <summary>The group name it shares with its neighbours - one fieldset per run of them.</summary>
    public string? Group { get; init; }

    /// <summary>The default value, as JSON.</summary>
    public string? Default { get; init; }

    /// <summary>The validation rules the field carries, e.g. "required", "maxLength 80", "range 1..5", "pattern ^[a-z]+$".</summary>
    public string[]? Rules { get; init; }

    /// <summary>When the field is shown, in the x-visibleWhen form: {"field":...,"op":...,"value":...} or {"all"/"any":[...]}.</summary>
    public string? VisibleWhen { get; init; }

    /// <summary>When the field is required, in the same form - including what if/then/else and dependentRequired became.</summary>
    public string? RequiredWhen { get; init; }

    /// <summary>When the field is disabled, in the same form.</summary>
    public string? DisabledWhen { get; init; }

    /// <summary>When the field is read-only, in the same form.</summary>
    public string? ReadOnlyWhen { get; init; }

    /// <summary>True when hiding the field clears its value (absent otherwise).</summary>
    public bool? ClearOnHide { get; init; }

    /// <summary>The choices it offers, as "value = Label" (the first of them, when there are many).</summary>
    public string[]? Options { get; init; }

    /// <summary>How many choices it offers in all, when Options lists only the first of them.</summary>
    public int? OptionCount { get; init; }

    /// <summary>The custom renderer key it asks the registry for (x-renderer).</summary>
    public string? Renderer { get; init; }
}

/// <summary>One wizard step of the form a schema becomes.</summary>
public record BormSchemaStepDto
{
    public required string Id { get; init; }

    public string? Title { get; init; }

    /// <summary>The top-level fields the step shows, in order.</summary>
    public required string[] Fields { get; init; }

    /// <summary>When the step is shown at all, in the x-visibleWhen form.</summary>
    public string? VisibleWhen { get; init; }
}

/// <summary>One problem BormDefinition.Validate() found in the form the schema became.</summary>
public record BormSchemaDiagnosticDto
{
    /// <summary>"Error" (the form will throw, bind wrongly or lose data) or "Warning" (it renders, but probably not as meant).</summary>
    public required string Severity { get; init; }

    /// <summary>Where the problem is, as a field path; empty for the form as a whole.</summary>
    public required string Path { get; init; }

    /// <summary>The library's own message, exactly as Validate() returns it.</summary>
    public required string Message { get; init; }
}

/// <summary>
/// What Bit.Borm makes of a JSON Schema document - the answer the running library gives for the same
/// text: the form it imports to, what the schema checks find wrong with it, and what it exports back.
/// </summary>
public record BormSchemaReportDto
{
    /// <summary>True when BormJsonSchemaImporter.TryImport accepted the document.</summary>
    public required bool Valid { get; init; }

    /// <summary>The importer's own message, exactly as TryImport hands it to an application, when it refused the document.</summary>
    public string? Error { get; init; }

    /// <summary>The size of the input, in UTF-8 bytes.</summary>
    public required int InputBytes { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    /// <summary>The number of grid columns the form is laid out in (x-columns), when it is more than one.</summary>
    public int? Columns { get; init; }

    /// <summary>Every field the form has, nested ones and repeater rows included.</summary>
    public int? FieldCount { get; init; }

    /// <summary>Every field counted by its BormFieldType.</summary>
    public BormCountDto[]? FieldTypes { get; init; }

    /// <summary>Every field of the form, in schema order, depth first.</summary>
    public BormSchemaFieldDto[]? Fields { get; init; }

    /// <summary>The wizard steps (x-steps), when the form is a wizard.</summary>
    public BormSchemaStepDto[]? Steps { get; init; }

    /// <summary>What BormDefinition.Validate() reports for the imported form.</summary>
    public BormSchemaDiagnosticDto[]? Diagnostics { get; init; }

    /// <summary>The document BormJsonSchemaExporter.Export gives back for the imported form.</summary>
    public string? Export { get; init; }

    /// <summary>
    /// True when the exported document imports again to the same fields - same paths, types,
    /// requiredness, rules and conditions. False means something did not survive the round trip;
    /// a note says what.
    /// </summary>
    public bool? RoundTrips { get; init; }

    /// <summary>A schema written by the library, returned when no schema was passed - the format, by example.</summary>
    public string? Sample { get; init; }

    /// <summary>What the findings mean in practice, and what to change when the outcome is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}
