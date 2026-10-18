namespace Bit.Bsqlite.Demo.Server.Dtos;

/// <summary>
/// One heading of one of the documents this server hands out. The documents are read through
/// GetBsqliteDocument (and the bsqlite://docs resources); this record is how the catalog, the search
/// index and the completions describe a section to each other.
/// </summary>
public record BsqliteDocumentSectionDto
{
    /// <summary>The document the section belongs to, e.g. "storage".</summary>
    public required string Document { get; init; }

    /// <summary>The heading text, e.g. "Selection".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBsqliteDocument as the section for exactly this one: the heading itself, or -
    /// for a heading the document uses twice - "Parent > Heading".
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 or 4 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The heading this one sits under, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>One document this server can hand out: the README, a guide under docs/, or a package's README.</summary>
public record BsqliteDocumentDto
{
    /// <summary>What to pass to GetBsqliteDocument, e.g. "storage", "readme" or "Bit.Bsqlite.Testing".</summary>
    public required string Name { get; init; }

    /// <summary>The document's own title, its first heading.</summary>
    public required string Title { get; init; }

    /// <summary>Readme (the repository README), Guide (docs/) or Package (a package's own README).</summary>
    public required string Kind { get; init; }

    /// <summary>The document's opening paragraph, cut to its first sentence.</summary>
    public string? Summary { get; init; }

    public required int Lines { get; init; }

    /// <summary>The keys of its sections, in reading order.</summary>
    public required string[] Sections { get; init; }
}

/// <summary>A public type of one of the Bit.Bsqlite packages.</summary>
public record BsqliteApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>The namespace to import - Bsqlite spreads its types over several, and the DI entry points live in Microsoft's.</summary>
    public required string Namespace { get; init; }

    /// <summary>The package that ships the type, e.g. "Bit.Bsqlite" or "Bit.Bsqlite.Storage.Opfs".</summary>
    public required string Package { get; init; }

    /// <summary>Interface, Enum, Exception, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (constructor, property, method, event or enum value) of a public Bit.Bsqlite type.</summary>
public record BsqliteApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Constructor, Property, Field, Method, Event or EnumValue.</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The parameter list, e.g. "(string databaseName, CancellationToken cancellationToken = default)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for a C# <c>required</c> member: an object initializer that leaves it out does not compile.</summary>
    public bool Required { get; init; }

    /// <summary>True for a protected member - what a derived class overrides, as with the conformance kit.</summary>
    public bool Protected { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bsqlite type.</summary>
public record BsqliteApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Namespace { get; init; }

    public required string Package { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BsqliteApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBsqliteSourceFile.</summary>
public record BsqliteSourceFileDto
{
    /// <summary>The path to pass to GetBsqliteSourceFile, e.g. "Demo/Client/Pages/Storage.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Page (a working demo page), Hosting (the wiring of the app), Model (the shared EF Core model) or Shared.</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One capability a storage backend declares, read off the provider instance.</summary>
public record BsqliteCapabilityDto
{
    /// <summary>The property of BsqliteStorageCapabilities, e.g. "CanChangePageSize".</summary>
    public required string Name { get; init; }

    /// <summary>The declared value, as C# would print it: "true", "Synchronous", "DedicatedWorker, Window".</summary>
    public required string Value { get; init; }

    /// <summary>What the capability means, from the library's own documentation of the property.</summary>
    public string? Summary { get; init; }
}

/// <summary>
/// One first-party storage backend, read off an instance of its provider type - so every capability
/// here is the one the library's selection and guards act on, not one someone copied into a table.
/// </summary>
public record BsqliteStorageProviderDto
{
    /// <summary>The name selection uses and the sticky binding records, e.g. "opfs".</summary>
    public required string Name { get; init; }

    /// <summary>The provider type, e.g. "BsqliteOpfsStorageProvider".</summary>
    public required string TypeName { get; init; }

    public required string Namespace { get; init; }

    public required string Package { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    /// <summary>The VFS name the worker opens the database with, when the provider declares one.</summary>
    public string? VfsName { get; init; }

    /// <summary>The ES module the worker imports to register the VFS; null for a backend that needs no JavaScript.</summary>
    public string? VfsModule { get; init; }

    /// <summary>The module probe and admin run on the main thread, when the provider declares one.</summary>
    public string? AdminModule { get; init; }

    public required BsqliteCapabilityDto[] Capabilities { get; init; }

    /// <summary>The statements the library's own guards refuse on this backend, each with the guard's message.</summary>
    public required BsqliteSqlVerdictDto[] Guarded { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Bsqlite.</summary>
public record BsqliteSearchHitDto
{
    /// <summary>What was found: "Guide section", "API class", "API method", "Storage backend", "Setup guide", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning document, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBsqlite answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BsqliteSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BsqliteSearchHitDto[] Hits { get; init; }

    /// <summary>True when the ranking was cut short at the requested limit, so a narrower query would show more.</summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// The words the query was actually searched by - punctuation, duplicates and filler words removed.
    /// Present only when nothing matched, which is the one time it explains something the caller could
    /// not work out from the query it just sent.
    /// </summary>
    public string[]? Terms { get; init; }

    /// <summary>Set only when Hits is empty: the reason, and the tool to fall back to.</summary>
    public string? Message { get; init; }

    /// <summary>Titles that nearly matched, when nothing matched outright - a query worth retrying with.</summary>
    public string[]? DidYouMean { get; init; }
}

/// <summary>One candidate of a planned storage selection, as described and as the library decided it.</summary>
public record BsqliteCandidateReportDto
{
    public required string Name { get; init; }

    /// <summary>The position in the preference order, 1 being Prefer(...); null for a provider that is registered but not a candidate.</summary>
    public int? Rank { get; init; }

    /// <summary>Whether a provider of this name is registered (AddStorageProvider).</summary>
    public required bool Registered { get; init; }

    /// <summary>True for one of the four first-party backends, whose capabilities are read off the real provider type.</summary>
    public required bool FirstParty { get; init; }

    /// <summary>Whether data on it survives a reload, as its capabilities declare. Null when it is not registered.</summary>
    public bool? Persistent { get; init; }

    /// <summary>Whether its probe reports it usable in the described browser. Null when it is not registered.</summary>
    public bool? Available { get; init; }

    /// <summary>The engine build it needs: Synchronous or AsyncCapable. Null when it is not registered.</summary>
    public string? EngineBuild { get; init; }

    /// <summary>
    /// The library's verdict on it (BsqliteStorageCandidateStatus): NotProbed, NotRegistered, Unavailable,
    /// RejectedAsNonPersistent, RejectedByExistingBinding or Selected. Null when selection never got to it -
    /// an existing database is decided by its binding alone.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>The library's own explanation of the verdict, or the probe's reason, or why it was not examined.</summary>
    public string? Explanation { get; init; }
}

/// <summary>What happened to an automatic migration during a planned open.</summary>
public record BsqliteMigrationReportDto
{
    public required string From { get; init; }

    public required string To { get; init; }

    /// <summary>Migrated (the binding moved), Failed (nothing moved, the database opened where it was) or NotAttempted.</summary>
    public required string Outcome { get; init; }

    /// <summary>Whether a database image was actually copied: false when the source held nothing its admin could export.</summary>
    public bool? Copied { get; init; }

    /// <summary>The library's own error, when the migration failed.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// What Bit.Bsqlite's own storage selection does with a described configuration and browser - the
/// answer the library gives at runtime, computed by running its resolver, its migrator and its session
/// factory against stand-in providers on this server.
/// </summary>
public record BsqliteStoragePlanDto
{
    /// <summary>Whether the database opens.</summary>
    public required bool Opens { get; init; }

    /// <summary>The backend it opens on.</summary>
    public string? Provider { get; init; }

    /// <summary>The engine build the worker would download for that backend: Synchronous or AsyncCapable.</summary>
    public string? EngineBuild { get; init; }

    /// <summary>The VFS module the worker would import; null for a backend that needs no JavaScript.</summary>
    public string? VfsModule { get; init; }

    /// <summary>Whether the most preferred candidate was chosen.</summary>
    public bool? IsFirstChoice { get; init; }

    /// <summary>Whether the sticky binding decided, rather than the preference order.</summary>
    public bool? WasDecidedByExistingData { get; init; }

    /// <summary>A backend ranked above the chosen one that is available but does not hold the data.</summary>
    public string? BetterProviderAvailable { get; init; }

    /// <summary>The sticky binding before the open: null for a database that does not exist yet.</summary>
    public string? BindingBefore { get; init; }

    /// <summary>The sticky binding after the open - written only once the open succeeded.</summary>
    public string? BindingAfter { get; init; }

    /// <summary>The automatic migration, when the migration mode and a better backend called for one.</summary>
    public BsqliteMigrationReportDto? Migration { get; init; }

    /// <summary>Every provider the plan involves: the candidates in preference order, then any other registered or bound one.</summary>
    public required BsqliteCandidateReportDto[] Candidates { get; init; }

    /// <summary>
    /// Why the database does not open: the message of the BsqliteStorageUnavailableException the
    /// application would get, or the configuration error the builder throws at startup.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>The registration this plan describes, as the C# that produces it.</summary>
    public required string Configuration { get; init; }

    /// <summary>What the library logged while deciding - a fallback is a warning, a better backend left unused is information.</summary>
    public required string[] Log { get; init; }

    /// <summary>What the outcome means in practice, and what to change when it is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}

/// <summary>Whether one backend's guards accept a statement.</summary>
public record BsqliteSqlVerdictDto
{
    /// <summary>The backend, or the statement when the verdict is listed per backend.</summary>
    public required string Name { get; init; }

    public required bool Accepted { get; init; }

    /// <summary>The BsqliteException message the command layer throws before the statement leaves for the worker.</summary>
    public string? Error { get; init; }
}

/// <summary>What the library's static SQL rules make of one piece of SQL - nothing of it is executed.</summary>
public record BsqliteSqlReportDto
{
    public required string Sql { get; init; }

    /// <summary>The verdict of BsqliteFeatureGuards on every first-party backend, under the limits its capabilities declare.</summary>
    public required BsqliteSqlVerdictDto[] Backends { get; init; }

    /// <summary>The tables BsqliteTableNames reads out of it - what a live query over it watches.</summary>
    public required string[] Tables { get; init; }

    /// <summary>Whether BsqliteTableNames counts it as a write, which is what notifies live queries when a transport cannot.</summary>
    public required bool LooksLikeWrite { get; init; }

    /// <summary>Whether it contains DDL, which re-runs every live query over the tables it names.</summary>
    public required bool LooksLikeSchemaChange { get; init; }
}

/// <summary>What CheckBsqliteSql answers: one report per piece of SQL checked.</summary>
public record BsqliteSqlCheckDto
{
    /// <summary>The SQL passed in, or - when none was - every statement the guards exist for, as a reference.</summary>
    public required BsqliteSqlReportDto[] Statements { get; init; }

    /// <summary>Why nothing was checked, when the input could not be.</summary>
    public string? Error { get; init; }

    /// <summary>What the verdicts mean, and what to do instead of a refused statement.</summary>
    public required string[] Notes { get; init; }
}

/// <summary>One tool, prompt or resource this server exposes, as the demo's MCP page lists it.</summary>
public record BsqliteMcpMemberDto
{
    public required string Name { get; init; }

    public string? Title { get; init; }

    public required string Description { get; init; }

    /// <summary>The arguments it takes, as "name: type", with "?" marking the optional ones.</summary>
    public required string[] Parameters { get; init; }
}

/// <summary>Everything the MCP server exposes, read off the attributes that declare it.</summary>
public record BsqliteMcpCatalogDto
{
    public required BsqliteMcpMemberDto[] Tools { get; init; }

    public required BsqliteMcpMemberDto[] Prompts { get; init; }

    public required BsqliteMcpMemberDto[] Resources { get; init; }
}
