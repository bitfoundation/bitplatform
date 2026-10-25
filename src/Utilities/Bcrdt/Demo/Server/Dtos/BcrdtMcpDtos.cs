using System.Text.Json.Nodes;

namespace Bit.Bcrdt.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README, which doubles as its reference guide. The guide is read
/// through GetBcrdtGuideSection (and the bcrdt://guide resources); this record is how the catalog,
/// the search index and the completions describe a section to each other.
/// </summary>
public record BcrdtGuideSectionDto
{
    /// <summary>The heading text, e.g. "Choosing a data type".</summary>
    public required string Heading { get; init; }

    /// <summary>
    /// What to pass to GetBcrdtGuideSection for exactly this section: the heading itself, or - for a
    /// heading the README uses more than once ("Persistence") - "Parent > Heading".
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 or 4 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The heading this one sits under, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of one of the four Bit.Bcrdt packages.</summary>
public record BcrdtApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    /// <summary>The package (assembly) the type ships in, e.g. "Bit.Bcrdt.SignalR".</summary>
    public required string Package { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, field, method, event or enum value) of a public Bit.Bcrdt type.</summary>
public record BcrdtApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, Method, Event or EnumValue. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the signature's return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string documentId, BcrdtOpenDocumentOptions options = null, ...)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member has on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters and C# required members.</summary>
    public bool Required { get; init; }

    /// <summary>
    /// True for a protected member: not callable on an instance, but the API of a type you derive from -
    /// BcrdtComponentBase's Track and RenderInterval, BcrdtAbstractType's CreateInstance and CreateEvent.
    /// </summary>
    public bool Protected { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bcrdt type.</summary>
public record BcrdtApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public required string Package { get; init; }

    public string? BaseType { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BcrdtApiMemberDto[] Members { get; init; }
}

/// <summary>A source file this server can hand out, retrievable through GetBcrdtSourceFile.</summary>
public record BcrdtSourceFileDto
{
    /// <summary>The path to pass to GetBcrdtSourceFile, e.g. "Demo/Client/Pages/ConflictLab.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Page (a working demo page), Sample (the code a page shows under its replicas: the same feature as an application writes it, one participant at a time), Shared (the replicas, transports and components the pages share) or Hosting (the wiring of the app).</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page introduction, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Bcrdt.</summary>
public record BcrdtSearchHitDto
{
    /// <summary>What was found: "Guide section", "API class", "API method", "Setup guide", "Lab", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or package.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>What SearchBcrdt answers: the ranked hits, or - when there are none - why, and what to try instead.</summary>
public record BcrdtSearchResultDto
{
    /// <summary>The matches, best first. Empty when nothing matched - Message then says what to do about it.</summary>
    public required BcrdtSearchHitDto[] Hits { get; init; }

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

/// <summary>One update a replica produced while it was cut off: an operation, committed as its own transaction.</summary>
public record BcrdtMergeUpdateDto
{
    /// <summary>The operation, as the lab read it, e.g. "map meta set title \"Draft\"".</summary>
    public required string Operation { get; init; }

    /// <summary>The size of the update that one transaction produced, in bytes - what a sync provider broadcasts for it.</summary>
    public required int Bytes { get; init; }
}

/// <summary>One replica of an inspected merge: what it did on its own, and what it held before it heard from the others.</summary>
public record BcrdtMergeReplicaDto
{
    /// <summary>The label the operations named it by, e.g. "A".</summary>
    public required string Label { get; init; }

    /// <summary>
    /// The client id the replica's document was given. Fixed per replica so the result is repeatable; in an
    /// application it is random per session, which is why a "one value wins" outcome should be treated as arbitrary.
    /// </summary>
    public required ulong ClientId { get; init; }

    /// <summary>The replica's own operations, in the order it made them, each with the size of its update.</summary>
    public required BcrdtMergeUpdateDto[] Updates { get; init; }

    /// <summary>The replica's document before the merge, as <c>BcrdtDocument.ToJson()</c> projects it.</summary>
    public JsonNode? Before { get; init; }

    /// <summary>The formatting of every root text that carries marks, as its <c>ToDelta()</c>, which <c>ToJson()</c> leaves out.</summary>
    public JsonNode? FormattingBefore { get; init; }

    /// <summary>
    /// What the replica sends a peer that holds only the shared starting state, in bytes:
    /// <c>EncodeStateAsUpdate(baseStateVector)</c> - its whole offline session as one delta.
    /// </summary>
    public required int DeltaBytes { get; init; }
}

/// <summary>What the merge decided for one thing more than one replica changed concurrently.</summary>
public record BcrdtMergeDecisionDto
{
    /// <summary>What was contested, e.g. "map 'meta', key 'title'" or "text 'notes'".</summary>
    public required string Target { get; init; }

    /// <summary>The replicas that changed it.</summary>
    public required string[] Replicas { get; init; }

    /// <summary>The rule that decided it: "one value wins", "both survive", "add wins", ...</summary>
    public required string Outcome { get; init; }

    /// <summary>The decision in plain words, with what every replica now holds.</summary>
    public required string Detail { get; init; }
}

/// <summary>
/// What Bit.Bcrdt's own engine makes of concurrent edits: each replica's state before the merge, the state
/// they converge to, whether every delivery order converged, the size of every update, and what was decided
/// for each thing more than one replica changed.
/// </summary>
public record BcrdtMergeReportDto
{
    /// <summary>The operations that ran, one per line, labelled with their replica - the input, normalized.</summary>
    public required string[] Operations { get; init; }

    /// <summary>True when no operations were passed and the lab ran its own sample scenario.</summary>
    public bool Sample { get; init; }

    /// <summary>Set when the operations could not be read or run; nothing else then describes a merge.</summary>
    public string? Error { get; init; }

    /// <summary>The state every replica started from, built by the "base" operations and synced to all of them first.</summary>
    public JsonNode? Base { get; init; }

    /// <summary>The replicas, in the order the operations first named them.</summary>
    public BcrdtMergeReplicaDto[]? Replicas { get; init; }

    /// <summary>True when every replica, under every delivery order tried, ended up with identical state.</summary>
    public bool Converged { get; init; }

    /// <summary>How many delivery orders were tried: every order of the replicas' batches, plus every update delivered newest first.</summary>
    public int Schedules { get; init; }

    /// <summary>The delivery orders whose result differed from the first one. Empty unless the engine has a bug.</summary>
    public string[]? Diverged { get; init; }

    /// <summary>The state every replica holds after the merge, as <c>BcrdtDocument.ToJson()</c> projects it.</summary>
    public JsonNode? Merged { get; init; }

    /// <summary>The formatting of every root text that carries marks after the merge, as its <c>ToDelta()</c>.</summary>
    public JsonNode? MergedFormatting { get; init; }

    /// <summary>The merged document encoded whole, in bytes - what a brand-new peer is sent.</summary>
    public int MergedBytes { get; init; }

    /// <summary>What the merge decided for each thing more than one replica changed.</summary>
    public BcrdtMergeDecisionDto[]? Decisions { get; init; }

    /// <summary>What the outcome means, and what to change when it is not the one wanted.</summary>
    public required string[] Notes { get; init; }
}

/// <summary>One block of a decoded update: a run of content by one author, or a range it stands in for.</summary>
public record BcrdtUpdateBlockDto
{
    /// <summary>The block's first clock; it covers [Clock, Clock + Length).</summary>
    public required long Clock { get; init; }

    public required int Length { get; init; }

    /// <summary>String, Any, Type, Format, Embed, Binary or Deleted for an item; GarbageCollected or Skip for a range without content.</summary>
    public required string Content { get; init; }

    /// <summary>The id of the left neighbour at insertion time, as "client@clock".</summary>
    public string? Origin { get; init; }

    /// <summary>The id of the right neighbour at insertion time, as "client@clock".</summary>
    public string? RightOrigin { get; init; }

    /// <summary>The container, written only when there is no origin: a root name ("root 'notes'") or the item holding a nested type.</summary>
    public string? Parent { get; init; }

    /// <summary>The map key, written only alongside the parent.</summary>
    public string? Key { get; init; }

    /// <summary>What the block carries, shortened: the text, the values, the nested type's kind, the mark.</summary>
    public string? Preview { get; init; }

    /// <summary>The block's encoded size in bytes.</summary>
    public required int Bytes { get; init; }
}

/// <summary>The blocks one author contributed to an update.</summary>
public record BcrdtUpdateClientDto
{
    /// <summary>The author's client id. A string, because client ids use all 64 bits and JSON numbers do not survive that in every client.</summary>
    public required string Client { get; init; }

    /// <summary>The first clock the update carries for this client.</summary>
    public required long FromClock { get; init; }

    /// <summary>The clock after the last unit the update carries for this client (exclusive).</summary>
    public required long ToClock { get; init; }

    /// <summary>The blocks, in clock order. Cut at the report's block limit; BlockCount says how many there are.</summary>
    public required BcrdtUpdateBlockDto[] Blocks { get; init; }

    public required int BlockCount { get; init; }

    /// <summary>This client's group header plus its blocks, in bytes.</summary>
    public required int Bytes { get; init; }
}

/// <summary>One range of an update's delete set.</summary>
public record BcrdtDeleteRangeDto
{
    public required string Client { get; init; }

    public required long Clock { get; init; }

    public required long Length { get; init; }
}

/// <summary>One entry of an awareness (presence) message.</summary>
public record BcrdtAwarenessEntryDto
{
    public required string Client { get; init; }

    /// <summary>The client's awareness counter; a higher one replaces what a peer holds.</summary>
    public required long Clock { get; init; }

    /// <summary>The published state, or null for a client that left.</summary>
    public JsonNode? State { get; init; }
}

/// <summary>
/// A decoded update or sync-protocol message, read with Bit.Bcrdt's own decoders: what kind it is, the
/// blocks per client, the delete set, the state vector it implies and its sizes - or why it is rejected.
/// </summary>
public record BcrdtUpdateInspectionDto
{
    /// <summary>"update" for a bare update, otherwise the protocol message kind: SyncStep1, SyncStep2, Update, SyncStep1Reply, AwarenessUpdate, AwarenessQuery, Auth, Ping or Pong.</summary>
    public required string Kind { get; init; }

    /// <summary>True when the payload decoded; false when the library rejected it - Error then says why.</summary>
    public required bool Valid { get; init; }

    /// <summary>The <c>BcrdtFormatException</c> message, or why the text is not base64 or hex.</summary>
    public string? Error { get; init; }

    /// <summary>The payload that was decoded, as base64 - the sample's, when none was passed.</summary>
    public string? Payload { get; init; }

    /// <summary>True when no payload was passed and a sample update made by the real engine was decoded.</summary>
    public bool Sample { get; init; }

    public required int TotalBytes { get; init; }

    /// <summary>The message envelope's version byte, for a protocol message.</summary>
    public int? Version { get; init; }

    /// <summary>The bytes after the two-byte envelope, for a protocol message.</summary>
    public int? PayloadBytes { get; init; }

    /// <summary>The update's block groups and the delete set, in bytes (an update, SyncStep2 or Update).</summary>
    public int? BlockBytes { get; init; }

    public int? DeleteSetBytes { get; init; }

    /// <summary>The blocks per author (an update, SyncStep2 or Update).</summary>
    public BcrdtUpdateClientDto[]? Clients { get; init; }

    /// <summary>True when Clients lists fewer blocks than the update carries; absent otherwise.</summary>
    public bool? BlocksTruncated { get; init; }

    /// <summary>The deleted ranges, per client (an update, SyncStep2 or Update).</summary>
    public BcrdtDeleteRangeDto[]? DeleteSet { get; init; }

    /// <summary>
    /// For an update: the state vector it implies - per client, how far it holds an unbroken history from clock 0.
    /// For SyncStep1 and SyncStep1Reply: the state vector the sender announces.
    /// </summary>
    public Dictionary<string, long>? StateVector { get; init; }

    /// <summary>SyncStep1Reply: the digest of the sender's delete set, as hex.</summary>
    public string? DeletionDigest { get; init; }

    /// <summary>AwarenessUpdate: the presence entries it carries.</summary>
    public BcrdtAwarenessEntryDto[]? Awareness { get; init; }

    /// <summary>Auth: whether the peer was permitted.</summary>
    public bool? Permitted { get; init; }

    /// <summary>Auth: the reason given.</summary>
    public string? Reason { get; init; }

    /// <summary>What the payload means for a receiver, and anything unusual about it.</summary>
    public required string[] Notes { get; init; }
}
