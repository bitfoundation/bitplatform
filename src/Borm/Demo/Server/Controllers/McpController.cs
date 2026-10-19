using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Borm.Demo.Server.Dtos;
using Bit.Borm.Demo.Server.Services;

namespace Bit.Borm.Demo.Server.Controllers;

/// <summary>
/// The Borm MCP server: the tools an AI agent calls to write form code with Bit.Borm without
/// guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Borm, the README, the shipped stylesheet, the library's own JSON
/// Schema importer, checks and exporter, and the demo's source files - so an agent gets what the
/// current version actually does rather than a snapshot someone wrote down. The same methods are
/// exposed as plain HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from
/// a browser.
/// </para>
/// <para>
/// One rule holds across the reference tools: a tool takes the key of the thing you want and
/// answers with that thing; leave the key out and it answers with the index of what there is. That
/// is what a listing tool used to be - and a listing tool costs a name, a description and a schema
/// in every request an agent makes, forever, to say what an optional argument says for nothing.
/// </para>
/// <para>
/// Each tool carries the two annotations that mean something for a tool which only reads:
/// <c>ReadOnly</c>, which is what lets a client run it without stopping to ask a person, and
/// <c>OpenWorld = false</c>, which says the answers come from a closed, known body of material
/// rather than from the internet.
/// </para>
/// <para>
/// Only the two tools whose answer is machine-actionable - a ranked hit carrying its own follow-up
/// call, a schema report - set <c>UseStructuredContent</c> and publish an output schema. The rest
/// answer in Markdown: a structured answer goes over the wire twice, once as the object and once as
/// the JSON text the spec asks for on its behalf, and documentation read as Markdown is smaller than
/// either half.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // The longest real answer is BormState's reference - some ninety members, each with its
    // documentation - at about 23,000 characters; BormFieldBuilder's, the Web App setup guide (which
    // quotes the demo's hosting files), the README's longest sections and the demo's longest pages all
    // land below it. The cap sits well above every one of them so each arrives whole, and is there so a
    // pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 60_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBorm), Title = "Search everything about Bit.Borm", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Borm at once - the reference guide, every public type and member, the setup guides, the theme's custom properties and the stylesheet's classes, and the demo's working forms - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'show a field when a checkbox is ticked', 'cascading options', 'wizard steps', 'repeater with a computed total', 'import a json schema safely', 'confirm password', 'dark mode'.")]
    public BormSearchResultDto SearchBorm(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BormSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBormSetupGuide), Title = "Setup guide for one hosting model", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Borm to a Blazor app in one hosting model: 'web-app' (a Blazor Web App with an InteractiveWebAssembly or InteractiveAuto client, which is how this demo is built and comes with its real files), 'webassembly' (standalone Blazor WebAssembly), 'server' (Blazor Server, or a Web App rendering InteractiveServer) or 'hybrid' (MAUI, WPF, Windows Forms). Call this before writing setup code - AddBorm() must be registered in every container that renders a form (both of a Web App's), the stylesheet link is required, and the page must render interactively.")]
    public string GetBormSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("web-app", "webassembly", "server", "hybrid")]
        [Description("The app's Blazor hosting model.")] string hostingModel)
    {
        return Truncate(BormSetupGuide.Get(hostingModel)
            ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BormSetupGuide.HostingModels)}.");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBormGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Borm reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'Generating a schema from a C# type', 'The fluent builder', 'JSON Schema', 'Untrusted documents', 'Validation', 'Conditional fields and wizards', 'Async and cascading options', 'Computed values', 'Layout', 'Custom renderers', 'The BormView component', 'BormState at a glance', 'Theming', 'Hosting models'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBormGuideSection(
        [Description("The section's heading, e.g. 'Async and cascading options'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BormSourceCatalog.RenderGuideIndex();

        var section = BormSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BormSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBormGuideSection with no heading for the index of sections, or SearchBorm to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBormApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Borm as Markdown, read out of the shipped assemblies: one type with every Blazor parameter, property, method, event or enum value it has - each with its C# type or signature, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BormView', 'BormField', 'BormState', 'BormDefinition', 'BormFieldDefinition', 'BormFieldBuilder', 'BormBuilder<TModel>', 'BormFieldType', 'BormConditionOperator', 'BormSchemaGenerator', 'BormJsonSchemaImporter', 'IBormFieldRendererRegistry', 'BormMessageKeys', 'BormFluentValidationIntegration'.")]
    public string GetBormApi(
        [Description("The type's name without its namespace, e.g. 'BormState', or 'BormBuilder<TModel>' for the typed builder. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BormApiCatalog.RenderIndex();

        var rendered = BormApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BormApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Borm has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Borm has no public type called '{typeName}'. Every public type is prefixed 'Borm' (e.g. 'BormView' rather than 'DynamicForm', 'BormDefinition' rather than 'FormSchema'); call GetBormApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBormTheme), Title = "Theme custom properties", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the CSS custom properties Bit.Borm's look is built from, read out of the shipped stylesheet: every --bit-borm-* property with its light value, its dark value and what it is used for, and the rules that make up the colour modes (prefers-color-scheme, data-theme). Pass one property for it in full - every rule that sets it or paints with it, and the override to write in every colour mode. Restyle forms by overriding these on .bit-borm-form rather than by overriding the library's selectors.")]
    public string GetBormTheme(
        [Description("The custom property, e.g. '--bit-borm-accent' (or just 'accent'). Omit it for the table of every property.")] string? property = null)
    {
        if (string.IsNullOrWhiteSpace(property)) return Truncate(BormThemeCatalog.RenderIndex());

        var rendered = BormThemeCatalog.Render(property);

        if (rendered is not null) return Truncate(rendered);

        var word = property.Trim().TrimStart('-').Replace("bit-borm-", string.Empty, StringComparison.OrdinalIgnoreCase);

        var candidates = BormThemeCatalog.Properties
            .Where(p => word.Length > 0 && p.Name.Contains(word, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The stylesheet declares no custom property called '{property}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The stylesheet declares no custom property called '{property}'. Every one is named --bit-borm-*; call GetBormTheme with no property for the table of them.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBormSchema), Title = "Inspect a JSON Schema as a Borm form", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs a JSON Schema document through Bit.Borm's own BormJsonSchemaImporter.TryImport and BormDefinition.Validate() and reports what the library makes of it: whether it imports (and the importer's own error when it does not), every field of the form it becomes - path, type, label, required, rules, visible/required/disabled/read-only conditions, options, wizard step - the diagnostics, what the importer drops without a word (unknown x- extensions, conditions it cannot read, dangling $refs, patterns .NET cannot compile), and the document BormJsonSchemaExporter.Export gives back, imported again to check it round-trips. Call it with no schema for a sample, the quickest way to see the x- extensions in use.")]
    public BormSchemaReportDto InspectBormSchema(
        [Description("The JSON Schema document, as text (up to 2 MB): draft-07 to 2020-12, with Borm's x- extensions (x-widget, x-visibleWhen, x-steps...). Omit it to get a sample schema instead.")] string? schema = null)
    {
        return BormSchemaLab.Inspect(schema);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBormSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Borm source file verbatim - e.g. 'Demo/Client/Pages/BuilderDemo.razor' (the fluent builder and conditions), 'Demo/Client/Pages/CascadingOptionsDemo.razor' (async and cascading options), 'Demo/Client/Pages/WizardDemo.razor', 'Demo/Client/Pages/ArrayDemo.razor' (a repeater with computed totals), 'Demo/Client/Pages/JsonSchemaDemo.razor', 'Demo/Client/Forms/Models.cs' (the models the pages bind to), or 'Library/Styles/bit-borm.css' (the shipped stylesheet). Omit the path for the index of files: the demo pages, each a working form, what they share, and the hosting files of a Blazor Web App. These are worked examples - for what a member means, GetBormApi answers in a fraction of the characters.")]
    public string GetBormSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/WizardDemo.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return Truncate(BormSourceCatalog.RenderSourceIndex());

        var content = BormSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BormSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBormSourceFile with no path for the index of files.";
        }

        return Truncate(content);
    }

    internal static string Truncate(string text)
    {
        if (text.Length <= MaxDocumentLength) return text;

        // Never between the two halves of a surrogate pair: half a pair is not text, and a client
        // that re-encodes the answer turns it into a replacement character or rejects it outright.
        var cut = char.IsHighSurrogate(text[MaxDocumentLength - 1]) ? MaxDocumentLength - 1 : MaxDocumentLength;

        return $"{text[..cut]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
