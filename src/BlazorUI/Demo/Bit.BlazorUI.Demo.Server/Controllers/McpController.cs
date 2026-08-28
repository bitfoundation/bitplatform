using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.AspNetCore.Components.Web;
using Bit.BlazorUI.Demo.Server.Services.Mcp;

namespace Bit.BlazorUI.Demo.Server.Controllers;

/// <summary>
/// The bit BlazorUI MCP server: the tools an AI agent calls to build a Blazor UI with this library
/// without guessing at it.
/// <para>
/// Every tool answers from the packages loaded in this process and from this site's own content -
/// the nav that decides which components exist, the assemblies that decide what they are, the demo
/// pages that carry the hand-written parameter tables and the worked examples, and the theming
/// reference rendered by the very components it documents. So an agent gets what the current
/// version actually does rather than a snapshot someone wrote down. The same methods are exposed as
/// plain HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
/// </para>
/// <para>
/// There are seven of them, and the count is the design rather than what was left over. A tool's
/// description is paid for in every request of every session a client has this server connected,
/// and it is paid for again in the model's attention every time it chooses between two tools that
/// sound alike. So a listing is not a tool - it is what a retrieval tool answers when it is asked
/// for nothing in particular; a single-item lookup is not a tool when a tool that takes a set
/// already resolves each member of it; and nothing here restates what the server's own
/// <c>instructions</c> have already put in the model's context before the first call.
/// </para>
/// <para>
/// Every one of them carries the same four annotations, because every one of them is the same kind
/// of call: it reads, it reads only from this process, and asking twice gives the same answer. A
/// client that is told a tool is read-only can run it without stopping to ask a person first, which
/// is the difference between an agent that consults the documentation and one that guesses rather
/// than interrupt; and OpenWorld = false says the answers come from this build, so a disagreement
/// with a search result is this library's version of the truth.
/// </para>
/// <para>
/// Every one of them answers in Markdown rather than in JSON, and none publishes an output schema.
/// A component's parameter table is sixty rows of four fields: as JSON that is the four field names
/// repeated sixty times, and a tool declared with UseStructuredContent would send the whole thing
/// twice - once as structuredContent and once, byte for byte, as text - because the protocol asks a
/// server to keep answering clients that cannot read a schema. A Markdown table names the columns
/// once, crosses the wire once, and is the shape a model reads best.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class McpController : AppControllerBase
{
    [AutoInject] private HtmlRenderer htmlRenderer = default!;
    [AutoInject] private ILogger<McpController> logger = default!;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBitBlazorUI), Title = "Search everything about bit BlazorUI",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Searches everything known about bit BlazorUI at once - every component with the names other libraries use for it, every parameter of every component, every worked example, every public type and enum value, and every chapter of the theming reference - and returns the best matches, each with the exact follow-up tool call that returns its full text. Ask it by capability rather than by name: 'let the user pick a date range', 'toast notification', 'searchable multi select with chips', 'virtualized table with sorting', 'dark mode', 'file upload with progress'.")]
    public string SearchBitBlazorUI(
        [Description("What the UI has to do, in your own words - the capability rather than a component name.")] string query,
        [Description("How many matches to return. The default is enough to choose from without reading a catalog.")] int limit = 12)
        => BlazorUISearchIndex.Search(query, limit);

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIComponent), Title = "Full API of one component",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the full reference of one bit BlazorUI component: which package it ships in and what that package needs wired up, every parameter with its type, its default and what it does, its public members, the classes and child components it owns (its per-part Classes/Styles bag, its item class), the library types its parameters take, the parameters it inherits, which of them are two-way bindable and what constrains its type arguments, and the titles of its worked examples. Accepts the type name, the name without the Bit prefix, the demo page's route or one of the aliases ('Select', 'Toast', 'Skeleton'). Omit the name to get the catalog of every component instead, grouped by category with its package and a line on what it is for.")]
    public string GetBitBlazorUIComponent(
        [Description("The component, e.g. 'BitDropdown', 'DatePicker', 'Toast'. Omitted, the whole catalog is returned. 'BitComponentBase', 'BitInputBase' and 'BitTextInputBase' are the inherited parameter sets each component's answer names.")] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return BlazorUIMarkdown.ComponentCatalog();

        var component = BlazorUIComponentCatalog.Find(name);

        if (component is not null) return BlazorUIMarkdown.Component(component);

        // A near miss is answered with the neighbours rather than with a refusal: the name an agent
        // guessed is usually one edit away from the one this library chose, and a listing of ten
        // candidates costs a fraction of the catalog it would otherwise go and fetch.
        var candidates = BlazorUIComponentCatalog.Similar(name);

        return candidates.Length > 0
            ? $"bit BlazorUI has no component called '{name}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"bit BlazorUI has no component called '{name}'. Call GetBitBlazorUIComponent with no name for the full catalog, or SearchBitBlazorUI to find it by what it does.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIComponentExamples), Title = "Working code for one component",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the worked examples of one component as the Razor and C# they are written in - the same code the documentation site runs, one section per feature, each with the paragraph that explains it. This is the only place the non-obvious parts show up: which parameters go together, what the templates receive, how the component is bound in an EditForm. Narrow it with 'example' rather than reading all of them; the section titles come back from GetBitBlazorUIComponent. A multi-API component answers with its first tab unless another is named.")]
    public string GetBitBlazorUIComponentExamples(
        [Description("The component, e.g. 'BitDataGrid', 'SearchBox'.")] string name,
        [Description("Optional. A section title, part of one, or a tab name of a multi-API component ('Item', 'Custom', 'Option'). Omitted, every section is returned - or, for a multi-API component, every section of its first tab.")] string? example = null)
    {
        var component = BlazorUIComponentCatalog.Find(name);

        if (component is null)
        {
            var candidates = BlazorUIComponentCatalog.Similar(name);

            return candidates.Length > 0
                ? $"bit BlazorUI has no component called '{name}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"bit BlazorUI has no component called '{name}'. Call GetBitBlazorUIComponent with no name for the full catalog.";
        }

        return BlazorUIMarkdown.Examples(component, example);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIType), Title = "Full reference of one type",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the full reference of one public type of the library, read straight out of the shipped assembly rather than inferred from its name: an enum with every value and what it means, a class with its properties and methods, an injectable service, or a static catalog of constants - 'BitColor', 'BitVariant', 'BitDropdownItem', 'BitModalService', 'BitThemePresets'. Dotted names reach a nested catalog, e.g. 'BitCss.Var.Color.Primary'. Omit the name to list the library-wide types instead; the ones named after a component are documented by GetBitBlazorUIComponent for that component.")]
    public string GetBitBlazorUIType(
        [Description("The type, with or without the Bit prefix and with or without its generic arguments, e.g. 'BitColor', 'BitDropdownItem<TValue>', 'ModalService'.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BlazorUIMarkdown.TypeCatalog();

        var type = BlazorUITypeCatalog.Find(typeName);

        if (type is not null) return BlazorUIMarkdown.Type(type);

        var candidates = BlazorUITypeCatalog.Similar(typeName);

        return candidates.Length > 0
            ? $"bit BlazorUI has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"bit BlazorUI has no public type called '{typeName}'. Call GetBitBlazorUIType with no name for the library-wide types, or SearchBitBlazorUI to find it by what it does.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUISetupGuide), Title = "Setup guide for one hosting model",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets everything needed to add bit BlazorUI to a Blazor app in one hosting model: the packages, the namespace import, the service registration, the stylesheet and script tags, the optional packages with the whole of what each one takes, and the check that proves it worked. How many DI containers register the services, which file holds the host page and whether the services may be singletons all differ per hosting model, and every one of those mistakes compiles.")]
    public string GetBitBlazorUISetupGuide(
        [Description("'web-app' (a server project plus a .Client project), 'wasm' (standalone WebAssembly), 'server' (Blazor Server) or 'hybrid' (MAUI, WPF, WinForms).")] string hostingModel)
    {
        return BlazorUISetupGuide.Get(hostingModel)
            ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BlazorUISetupGuide.HostingModels)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBitBlazorUIThemingGuide), Title = "One chapter of the theming reference",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets one chapter of the bit BlazorUI theming reference, rendered from the documentation site itself: the design tokens and how to override one, the packaged presets (Fluent, Fluent 2, Material, Cupertino) and how to author your own, deriving a whole palette from one brand color, contrast checking, density and RTL, the C# and JavaScript APIs, and how to stop a server-rendered app flashing the wrong theme on the first frame. A theme here is data rather than a stylesheet fork, so the answer to a question about color, dark mode, spacing or brand fit is almost never 'write CSS'. Omit the chapter to get the index of them.")]
    public async Task<string> GetBitBlazorUIThemingGuide(
        [Description("A chapter of the reference, e.g. 'Design tokens', 'Presets', 'Color derivation and contrast', 'The C# API', 'Server-side rendering'. Sub-headings resolve too. Omitted, the index is returned.")] string? section = null)
        => await BlazorUIThemingGuide.Get(htmlRenderer, logger, section);

    [HttpGet]
    [McpServerTool(Name = nameof(FindBitBlazorUIIcons), Title = "Find a glyph by what it shows",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Finds BitIconName glyphs by what they depict. The set is Microsoft's Fabric (MDL2) icons and there are over two thousand of them, so they are searched rather than listed. Names are matched word by word, so 'add friend' finds AddFriend. A glyph name that does not exist is not a compile error, it is an empty box on the page.")]
    public string FindBitBlazorUIIcons(
        [Description("What the glyph shows, e.g. 'save', 'chevron down', 'shopping cart', 'calendar'.")] string query,
        [Description("How many names to return.")] int limit = 40)
        => BlazorUIIconCatalog.Search(query, limit);
}
