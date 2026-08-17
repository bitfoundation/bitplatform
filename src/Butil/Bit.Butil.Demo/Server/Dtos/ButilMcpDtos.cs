namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>One page of the documentation site (mirrors an entry of the client's DocsNav).</summary>
public record ButilDocsPageDto
{
    /// <summary>The sidebar group the page belongs to, e.g. "Storage".</summary>
    public required string Group { get; init; }

    /// <summary>The value to pass to GetButilDocsPage, e.g. "clipboard".</summary>
    public required string Slug { get; init; }

    /// <summary>The page's path on the live documentation site.</summary>
    public required string Url { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    /// <summary>The Bit.Butil public types the page documents - empty for a guide page.</summary>
    public required string[] Services { get; init; }

    /// <summary>How widely the underlying browser API is implemented, e.g. "Chromium only".</summary>
    public required string BrowserSupport { get; init; }

    /// <summary>The preconditions the API imposes: HTTPS, a permission prompt, a user gesture.</summary>
    public required string[] Requires { get; init; }
}

/// <summary>One heading of the library's README, which doubles as its reference guide.</summary>
public record ButilGuideSectionDto
{
    /// <summary>The heading text, e.g. "Subscriptions are disposable". Pass it to GetButilGuideSection.</summary>
    public required string Heading { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The owning level-2 section, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section (including its sub-sections).</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of the Bit.Butil assembly.</summary>
public record ButilApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Service, Static class, Interface, Enum, Attribute, Delegate, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    /// <summary>True for a class marked [ButilService] - the ones you inject by their own name.</summary>
    public required bool IsInjectable { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (property, method, field or enum value) of a public Bit.Butil type.</summary>
public record ButilApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Property, Method, Field, Event or EnumValue.</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string key, string value)".</summary>
    public string? Signature { get; init; }

    /// <summary>The constant value of a const field, or the value a property has on a fresh instance.</summary>
    public string? Default { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Butil type.</summary>
public record ButilApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    /// <summary>How to obtain one, e.g. "@inject Bit.Butil.Clipboard clipboard" - null when it is not a service.</summary>
    public string? Inject { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    /// <summary>The documentation page covering this type, when the site has one.</summary>
    public string? DocsUrl { get; init; }

    public required ButilApiMemberDto[] Members { get; init; }
}

/// <summary>What GetButilApiDetails answers: the type's reference, or why there is none.</summary>
public record ButilApiDetailsResultDto
{
    /// <summary>The full reference of the type, when a public type goes by the requested name.</summary>
    public ButilApiTypeDetailsDto? Details { get; init; }

    /// <summary>Set instead of Details when nothing matched - it names the closest candidates.</summary>
    public string? Message { get; init; }
}

/// <summary>One row of the browser-support matrix: an API, and what it needs from the page.</summary>
public record ButilCapabilityDto
{
    /// <summary>The documented API, e.g. "Clipboard" or "Local &amp; Session Storage".</summary>
    public required string Api { get; init; }

    /// <summary>The Bit.Butil types behind it - what you actually inject or call.</summary>
    public required string[] Services { get; init; }

    /// <summary>Which engines implement it: "All engines", "Varies by engine", "Chromium only", ...</summary>
    public required string BrowserSupport { get; init; }

    /// <summary>The preconditions the calling page has to satisfy, one sentence each.</summary>
    public required string[] Requires { get; init; }

    public required string Summary { get; init; }

    /// <summary>The documentation page for the API, on the live site.</summary>
    public required string DocsUrl { get; init; }
}

/// <summary>What using one Butil API entails, beyond the signatures of its members.</summary>
public record ButilApiInspectionDto
{
    /// <summary>The name that was looked up.</summary>
    public required string Query { get; init; }

    /// <summary>False when nothing in Bit.Butil goes by that name - Message then says what to try.</summary>
    public required bool IsKnown { get; init; }

    public string? Message { get; init; }

    /// <summary>The documented API this resolved to, e.g. "Clipboard".</summary>
    public string? Api { get; init; }

    /// <summary>The Bit.Butil types behind it.</summary>
    public string[]? Services { get; init; }

    /// <summary>The injection line to put in a component, when the API is an injectable service.</summary>
    public string[]? Inject { get; init; }

    public string? BrowserSupport { get; init; }

    /// <summary>The preconditions the calling page has to satisfy before the call can succeed.</summary>
    public string[]? Requires { get; init; }

    /// <summary>What is easy to get wrong: prerendering, disposal, gestures, fast invoke.</summary>
    public string[]? Notes { get; init; }

    /// <summary>Members whose result has to be disposed - a subscription or a handle on real hardware.</summary>
    public string[]? Disposables { get; init; }

    /// <summary>The follow-up calls that return the full text: the API reference and the docs page.</summary>
    public string[]? NextCalls { get; init; }
}

/// <summary>The combined consequences of building a feature on a set of Butil APIs.</summary>
public record ButilFeaturePlanDto
{
    /// <summary>One entry per requested API, in the order they were passed.</summary>
    public required ButilApiInspectionDto[] Apis { get; init; }

    /// <summary>Names that matched nothing - a typo, or an API this library does not wrap.</summary>
    public required string[] Unknown { get; init; }

    /// <summary>True when at least one API only works over HTTPS or on localhost.</summary>
    public required bool RequiresSecureContext { get; init; }

    /// <summary>True when at least one API prompts the user, so a denial has to be a supported outcome.</summary>
    public required bool RequiresPermission { get; init; }

    /// <summary>True when at least one API only works from inside a click handler.</summary>
    public required bool RequiresUserGesture { get; init; }

    /// <summary>The APIs that do not work in every engine, each with the engines that do implement it.</summary>
    public required string[] EngineLimited { get; init; }

    /// <summary>The ordered checklist for shipping this feature.</summary>
    public required string[] Checklist { get; init; }
}

/// <summary>A source file of the demo/samples, retrievable through GetButilSourceFile.</summary>
public record ButilSourceFileDto
{
    /// <summary>The path to pass to GetButilSourceFile, e.g. "Demo/Client/Pages/ClipboardPage.razor".</summary>
    public required string Path { get; init; }

    /// <summary>Demo (this documentation site) or Sample (the minimal hosting-model samples).</summary>
    public required string Kind { get; init; }

    /// <summary>The file's own header comment or page title, when it has one.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }
}

/// <summary>One result of a search across everything this MCP server knows about Bit.Butil.</summary>
public record ButilSearchHitDto
{
    /// <summary>What was found: "Guide section", "Docs page", "API service", "API method", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}
