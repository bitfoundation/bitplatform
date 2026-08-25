namespace Bit.Bswup.Demo.Server.Dtos;

/// <summary>
/// One heading of the library's README. The README is reachable as a resource rather than through
/// a tool - the documentation site says everything it says, in pages sized to a question - so this
/// is the shape behind a resource listing, not behind a tool result.
/// </summary>
public record BswupGuideSectionDto
{
    /// <summary>The heading text, e.g. "JavaScript API".</summary>
    public required string Heading { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The owning level-2 section, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>
/// One configurable knob of Bswup: a <c>bit-bswup.js</c> script-tag attribute, a <c>self.*</c>
/// setting of the service-worker file, or a parameter of the <c>BswupProgress</c> component.
/// <para>
/// Which of the three it is, where the value is written, and which documentation page carries the
/// prose are the same for every entry one tool returns, so they are said once in that tool's
/// description instead of once per entry. Repeated here they were a fifth of the reply - a caller
/// reading twenty-four settings was told twenty-four times that they go in service-worker.js.
/// </para>
/// </summary>
public record BswupOptionDto
{
    /// <summary>The name exactly as written where it is configured, e.g. "updateInterval".</summary>
    public required string Name { get; init; }

    public string? Type { get; init; }

    /// <summary>The value that applies when the option is left unset, when the shipped code declares one.</summary>
    public string? Default { get; init; }

    public string? Summary { get; init; }

    /// <summary>The caveats worth knowing before using it - what the summary cannot fit.</summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// True when the shipped Bswup script (or, for a component parameter, the shipped assembly)
    /// declares this name - so it is answered from the current build rather than from a
    /// description that could have gone stale.
    /// </summary>
    public required bool VerifiedFromSource { get; init; }
}

/// <summary>
/// Everything the service-worker file can configure: the settings themselves, plus the built-in
/// asset filters and the <c>self.mode</c> presets the shipped worker applies around them.
/// <para>
/// The three lists beyond <see cref="Settings"/> are null unless the settings they explain are in
/// the answer. Asked about one setting by name, a caller gets that setting: eighteen asset
/// patterns and four presets alongside it would be most of the reply and none of the answer.
/// </para>
/// </summary>
public record BswupServiceWorkerSettingsDto
{
    /// <summary>The <c>self.*</c> settings that were asked for, with type, default and caveats.</summary>
    public required BswupOptionDto[] Settings { get; init; }

    /// <summary>The asset include patterns the shipped worker applies before the file's own.</summary>
    public string[]? DefaultAssetsInclude { get; init; }

    /// <summary>The asset exclude patterns the shipped worker applies before the file's own. An exclude beats an include.</summary>
    public string[]? DefaultAssetsExclude { get; init; }

    /// <summary>The <c>self.mode</c> presets, with the settings each of them fills in.</summary>
    public BswupModeDto[]? Modes { get; init; }

    /// <summary>The rules that decide whether a setting takes effect at all.</summary>
    public required string[] Notes { get; init; }
}

/// <summary>
/// A preset bundle of service-worker settings selectable with a single <c>self.mode</c> value.
/// That a preset never overrides a setting the file assigns itself holds for all four of them, so
/// it is said once in the notes of the reply rather than once per preset.
/// </summary>
public record BswupModeDto
{
    /// <summary>The value to assign to <c>self.mode</c>, e.g. "FullOffline".</summary>
    public required string Name { get; init; }

    /// <summary>The settings the preset fills in, in the order the worker applies them.</summary>
    public required Dictionary<string, string> Settings { get; init; }
}

/// <summary>One lifecycle message Bswup hands to the page's handler function.</summary>
public record BswupEventDto
{
    /// <summary>The constant on the global <c>BswupMessage</c> object, e.g. "downloadProgress".</summary>
    public required string Name { get; init; }

    /// <summary>The string the handler actually receives, e.g. "DOWNLOAD_PROGRESS".</summary>
    public required string Message { get; init; }

    /// <summary>The shape of the <c>data</c> argument that comes with the message.</summary>
    public string? Payload { get; init; }

    public string? Summary { get; init; }

    /// <summary>Set when the constant still exists only so old handlers keep resolving it.</summary>
    public string? Deprecated { get; init; }

    public required bool VerifiedFromSource { get; init; }
}

/// <summary>One member of the global <c>BitBswup</c> object the page script installs.</summary>
public record BswupJsApiDto
{
    public required string Name { get; init; }

    /// <summary>The call as written in page code, e.g. "BitBswup.forceRefresh(cacheFilter?)".</summary>
    public required string Signature { get; init; }

    /// <summary>What the call resolves with, e.g. "Promise&lt;boolean&gt;".</summary>
    public string? Returns { get; init; }

    public string? Summary { get; init; }

    public required bool VerifiedFromSource { get; init; }
}

/// <summary>An element id the built-in progress script drives inside the splash markup.</summary>
public record BswupProgressElementDto
{
    public required string Id { get; init; }

    public required string Role { get; init; }

    /// <summary>True for the elements a custom <c>ChildContent</c> splash must not re-render itself.</summary>
    public bool RenderedByComponent { get; init; }
}

/// <summary>Everything the built-in progress UI exposes: parameters, driven elements, runtime config.</summary>
public record BswupProgressUiDto
{
    /// <summary>The parameters of the <c>BswupProgress</c> component, read off the shipped assembly.</summary>
    public required BswupOptionDto[] Parameters { get; init; }

    /// <summary>The ids <c>bit-bswup.progress.js</c> looks for - the contract a custom splash writes against.</summary>
    public required BswupProgressElementDto[] Elements { get; init; }

    /// <summary>The runtime toggle call, and what it accepts.</summary>
    public required string RuntimeConfig { get; init; }

    /// <summary>The script and stylesheet the component needs on the page to do anything.</summary>
    public required string[] Requires { get; init; }

    public required string[] Notes { get; init; }
}

/// <summary>A source file of the demo, the samples or the library, retrievable through GetBswupSourceFile.</summary>
public record BswupSourceFileDto
{
    /// <summary>The path to pass to GetBswupSourceFile, e.g. "Demo/Client/wwwroot/service-worker.published.js".</summary>
    public required string Path { get; init; }

    /// <summary>Library (the shipped Bswup sources), Demo (this documentation site) or Sample (the minimal samples).</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bswup.</summary>
public record BswupSearchHitDto
{
    /// <summary>What was found: "Guide section", "Docs page", "Service worker setting", "Event", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, page or category.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}

/// <summary>One <c>self.&lt;name&gt; = ...</c> assignment found in an inspected service-worker file.</summary>
public record BswupSettingAssignmentDto
{
    public required string Name { get; init; }

    /// <summary>The assigned expression, verbatim (collapsed to one line).</summary>
    public required string Value { get; init; }

    /// <summary>False when the shipped worker declares no setting by this name - almost always a typo.</summary>
    public required bool Recognized { get; init; }

    /// <summary>True when the assignment sits after the <c>importScripts</c> line, where the engine can no longer see it.</summary>
    public required bool AfterImport { get; init; }

    public string? Summary { get; init; }
}

/// <summary>The result of checking a service-worker file against the shipped Bswup worker.</summary>
public record BswupServiceWorkerInspectionDto
{
    /// <summary>False when the file never imports the Bswup engine - the one mandatory line.</summary>
    public required bool ImportsBswup { get; init; }

    /// <summary>The import as written, when there is one.</summary>
    public string? Import { get; init; }

    public required BswupSettingAssignmentDto[] Settings { get; init; }

    /// <summary>Findings that make the worker behave incorrectly - fix these before shipping.</summary>
    public required string[] Problems { get; init; }

    /// <summary>Findings that are legal but are usually not what the author meant.</summary>
    public required string[] Warnings { get; init; }

    /// <summary>Behavior of this configuration that is easy to be surprised by.</summary>
    public required string[] Notes { get; init; }

    /// <summary>
    /// What this configuration does with the asset URLs the caller passed, or null when none were.
    /// It rides on the inspection rather than living in a tool of its own because the question is
    /// always about a file that is being checked anyway - and sending that file twice is the one
    /// cost neither side can do anything about after the fact.
    /// </summary>
    public BswupAssetAnalysisDto? Assets { get; init; }
}

/// <summary>What the service worker does with one asset URL under the inspected configuration.</summary>
public record BswupAssetDecisionDto
{
    public required string Url { get; init; }

    /// <summary>True when the asset is precached (or, in passive mode, managed and filled on first use).</summary>
    public required bool Cached { get; init; }

    /// <summary>The pattern that decided it, and which list the pattern came from.</summary>
    public required string Reason { get; init; }
}

/// <summary>The result of running a set of asset URLs through the include/exclude lists.</summary>
public record BswupAssetAnalysisDto
{
    /// <summary>The include patterns in effect, defaults first - exactly the order the worker builds them in.</summary>
    public required string[] Include { get; init; }

    /// <summary>The exclude patterns in effect, defaults first. An exclude beats an include.</summary>
    public required string[] Exclude { get; init; }

    public required BswupAssetDecisionDto[] Assets { get; init; }

    public required string[] Notes { get; init; }
}

/// <summary>
/// The body of the POST form of InspectBswupServiceWorker. A whole service-worker file does not fit
/// in a query string - the GET mirror of that tool is for snippets and for reading in a browser.
/// </summary>
public record BswupInspectRequestDto
{
    /// <summary>The full content of the service-worker.js file to check, verbatim.</summary>
    public required string Script { get; init; }

    /// <summary>
    /// Optional asset URLs to decide under that same file - one per line, or separated by commas
    /// or semicolons. Left out, the reply is the inspection alone.
    /// </summary>
    public string? AssetUrls { get; init; }
}
