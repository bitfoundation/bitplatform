namespace Bit.BlazeDb.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBlazeDbGuideSection (and the blazedb://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BlazeDbGuideSectionDto
{
    /// <summary>The heading text, e.g. "Encryption at rest".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBlazeDbGuideSection for exactly this section: the heading itself, or - for a
    /// heading the README uses twice - "Parent > Heading".
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 or 4 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The heading this one sits under, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of one of the Bit.BlazeDb assemblies.</summary>
public record BlazeDbApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    /// <summary>The package the type ships in: Bit.BlazeDb, Bit.BlazeDb.Browser or Bit.BlazeDb.EntityFrameworkCore.</summary>
    public required string Package { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (property, field, method, event or enum value) of a public Bit.BlazeDb type.</summary>
public record BlazeDbApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue.</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(BlazeDbDatabaseOptions options, CancellationToken cancellationToken = default)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for a member the compiler requires to be set (<c>required</c>, or [EditorRequired]).</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.BlazeDb type.</summary>
public record BlazeDbApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public required string Package { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BlazeDbApiMemberDto[] Members { get; init; }
}

/// <summary>A source file of the demo, retrievable through GetBlazeDbSourceFile.</summary>
public record BlazeDbSourceFileDto
{
    /// <summary>The path to pass to GetBlazeDbSourceFile, e.g. "Demo/Client/Data/DemoModels.cs".</summary>
    public required string Path { get; init; }

    /// <summary>Page (a demo page driving a real database), Data (the models and services the pages share) or Hosting.</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page lead, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.BlazeDb.</summary>
public record BlazeDbSearchHitDto
{
    /// <summary>What was found: "Guide section", "API class", "API method", "Diagnostic", "Setup guide", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or package.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBlazeDb answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BlazeDbSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BlazeDbSearchHitDto[] Hits { get; init; }

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
public record BlazeDbInspectionDiagnosticDto
{
    /// <summary>The id, e.g. "BLZ001" for the BlazeDb generator or "CS0103" for the C# compiler.</summary>
    public required string Id { get; init; }

    /// <summary>Error or Warning.</summary>
    public required string Severity { get; init; }

    public required string Message { get; init; }

    /// <summary>"generator" for a BlazeDb diagnostic, "compiler" for one the C# compiler reports on the result.</summary>
    public required string Source { get; init; }

    /// <summary>1-based line of the inspected source the diagnostic points at, when it points into it.</summary>
    public int? Line { get; init; }
}

/// <summary>A property the generated serializer writes, and how it is stored.</summary>
public record BlazeDbInspectedFieldDto
{
    /// <summary>The property's name.</summary>
    public required string Property { get; init; }

    /// <summary>The property's C# type, e.g. "string?" or "List&lt;int&gt;".</summary>
    public required string Type { get; init; }

    /// <summary>The field number that identifies the property in the stored bytes.</summary>
    public required int FieldNumber { get; init; }

    /// <summary>True when the number was pinned with [BlazeDbField]; false when it follows declaration order.</summary>
    public required bool Explicit { get; init; }

    /// <summary>VarInt, Fixed32, Fixed64 or LengthDelimited.</summary>
    public required string WireType { get; init; }

    /// <summary>True for the [BlazeDbKey] property.</summary>
    public bool IsKey { get; init; }
}

/// <summary>A secondary index the generator emitted into the type's Indexes class.</summary>
public record BlazeDbInspectedIndexDto
{
    /// <summary>The index's name, which is also its member of the generated Indexes class.</summary>
    public required string Name { get; init; }

    /// <summary>Hash (equality lookups) or Ordered (ranges and ordering).</summary>
    public required string Kind { get; init; }

    public required bool Unique { get; init; }

    /// <summary>The properties the index keys on: one, or several for a compound index.</summary>
    public required string[] Members { get; init; }

    /// <summary>The type of the index key, e.g. "bool" or "(string? Category, bool Done)".</summary>
    public required string KeyType { get; init; }

    /// <summary>The expression that names this index in code, e.g. "TodoItem.Indexes.Done".</summary>
    public required string Reference { get; init; }
}

/// <summary>One [BlazeDbTable] type found in the inspected source, and what the generator made of it.</summary>
public record BlazeDbInspectedTableDto
{
    /// <summary>The type's name.</summary>
    public required string TypeName { get; init; }

    /// <summary>True when the generator emitted a partial for it - false when it was rejected.</summary>
    public required bool Generated { get; init; }

    /// <summary>The table's stored name, from [BlazeDbTable("...")] or the type name.</summary>
    public string? TableName { get; init; }

    /// <summary>The [BlazeDbKey] property, and the C# type of the key.</summary>
    public string? Key { get; init; }

    public string? KeyType { get; init; }

    /// <summary>The properties the row bytes carry, in field-number order. Empty when nothing was generated.</summary>
    public required BlazeDbInspectedFieldDto[] Fields { get; init; }

    /// <summary>The secondary indexes, in the order the descriptor registers them.</summary>
    public required BlazeDbInspectedIndexDto[] Indexes { get; init; }

    /// <summary>The members the generated partial adds that application code uses, with their types, e.g. "TodoItem.Table : BlazeDbTableDescriptor&lt;Guid, TodoItem&gt;".</summary>
    public required string[] GeneratedMembers { get; init; }
}

/// <summary>What InspectBlazeDbTable answers: the verdict of the real BlazeDb generator on a piece of source.</summary>
public record BlazeDbTableInspectionDto
{
    /// <summary>True when the generator reported no error and the generated code compiles without one.</summary>
    public required bool IsValid { get; init; }

    public required BlazeDbInspectedTableDto[] Tables { get; init; }

    /// <summary>Everything reported, generator diagnostics first.</summary>
    public required BlazeDbInspectionDiagnosticDto[] Diagnostics { get; init; }

    public required string[] Notes { get; init; }

    /// <summary>The generated source, only when it was asked for.</summary>
    public string? GeneratedCode { get; init; }
}
