using Bit.Borm.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Borm.Demo.Client;

/// <summary>
/// One documentation page: <paramref name="Url"/> is its address, <paramref name="Keywords"/> feeds
/// the header's search palette, and <paramref name="PageType"/> is the component the address renders -
/// which is what the "Edit this page on GitHub" link under every page is built from.
/// </summary>
public record DocsPageInfo(string Url, string Title, string Description, string Keywords, Type PageType);

public record DocsSection(string Title, DocsPageInfo[] Pages);

/// <summary>
/// A link in the header. <paramref name="MatchesDocs"/> marks the entry that stands for the docs as a
/// whole: it is the current section on every page no other entry claims.
/// </summary>
public record SiteNavItem(string Href, string Label, bool MatchesDocs = false);

/// <summary>
/// What the shared site chrome in Layout/ - the header, the docs shell, the palette and the footer -
/// says about this particular demo. Those components are the same file in every Utilities demo; this
/// class and <see cref="DocsCatalog"/> are what each demo fills in for itself.
/// </summary>
public static class SiteInfo
{
    /// <summary>The library's name, as the header's wordmark and the breadcrumb's root show it.</summary>
    public const string Name = "Borm";

    public const string PackageId = "Bit.Borm";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Borm";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Borm is a schema-driven form engine for Blazor: describe a form once - from a C# type, a fluent builder " +
        "or JSON Schema - and it renders, validates and keeps the state of it, with every decision made in C#. " +
        "Every page of this site is a working form.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["wizard", "repeater", "json schema", "validation", "combobox"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/json-schema", "JSON Schema"),
        new("/mcp-server", "MCP server"),
    ];

    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public static string ProjectUrl => $"{RepositoryUrl}/tree/develop/{ProjectPath}";

    public static string NuGetUrl => $"https://www.nuget.org/packages/{PackageId}";

    /// <summary>
    /// The page's source file on GitHub. Razor gives a component the namespace of its folder, so the
    /// path below Demo/Client is the type's namespace past this assembly's root, one folder a segment.
    /// </summary>
    public static string SourceUrlOf(Type page)
    {
        var root = typeof(SiteInfo).Namespace!;
        var relative = page.FullName!.StartsWith(root + ".", StringComparison.Ordinal)
            ? page.FullName[(root.Length + 1)..]
            : page.Name;

        return $"{RepositoryUrl}/blob/develop/{ProjectPath}/Demo/Client/{relative.Replace('.', '/')}.razor";
    }
}

/// <summary>
/// The single source of truth for the site's information architecture. The docs nav panel, the
/// header's search palette, the previous/next pager, the breadcrumb and the footer's link columns are
/// all driven from here, so adding a page means adding exactly one entry.
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("/getting-started", "Getting started",
                "Install the package, register AddBorm(), link the stylesheet and render a first form from an annotated model.",
                "install setup nuget package AddBorm program.cs stylesheet bit-borm.css imports render mode first form BormView",
                typeof(GettingStarted)),
            new("/quickstart", "From a C# type",
                "BormSchemaGenerator.Generate<T>() turns a model and its DataAnnotations into a complete form.",
                "schema generator generate poco model dataannotations reflection required range display inherited zero config",
                typeof(Quickstart)),
            new("/builder", "Fluent builder + conditions",
                "BormBuilder.For<T>() configures fields with typed expressions, and VisibleWhen shows the company fields for business accounts.",
                "fluent builder BormBuilder expression field required email pattern radio VisibleWhen condition conditional",
                typeof(BuilderDemo)),
            new("/json-schema", "JSON Schema",
                "A live JSON Schema editor: the form regenerates as you type, and Export writes it back out, conditions and steps intact.",
                "json schema import export round trip $ref allOf if then dependentRequired enum format x-widget x-visibleWhen x-steps TryImport",
                typeof(JsonSchemaDemo)),
        ]),
        new("Fields & layout",
        [
            new("/layout", "Layout & field types",
                "Columns and ColumnSpan lay a form out on a responsive grid, with [Flags] and List<TEnum> multi-selects and a nested object group.",
                "layout grid columns ColumnSpan responsive flags enum multiselect checkbox list nested object group autocomplete",
                typeof(LayoutDemo)),
            new("/field-polish", "Affixes, counters & live updates",
                "Prefixes and suffixes inside the border, a character counter, static headings, a clear button, a password reveal and a switch.",
                "prefix suffix affix CharacterCount counter UpdateOnInput debounce Static heading Clearable Revealable password AsSwitch Suggest datalist",
                typeof(FieldPolishDemo)),
            new("/combobox-tags", "Combobox, tags & change handlers",
                "A type-to-filter combobox, chips for a list of strings, and an OnChange handler that fills in another field.",
                "combobox AsCombobox allowCustom datalist tags chips AsTags OnChange SetSibling ClearSibling listener handler",
                typeof(ComboboxAndTagsDemo)),
            new("/files", "File upload",
                "IBrowserFile fields with an accept list, size and type rules checked again after the picker, and the chosen files listed back.",
                "file upload IBrowserFile InputFile accept MaxFileSize multiple attachments",
                typeof(FileUploadDemo)),
            new("/composition", "Groups, custom layout & resilience",
                "Named fieldsets, a page that lays out BormField one at a time, per-field undo, and an options service that is allowed to fail.",
                "group fieldset legend GroupName BormField custom layout ResetField IsDirty OptionsLoadFailed InvalidateOptions AsSearch",
                typeof(CompositionDemo)),
            new("/custom-renderer", "Custom renderer",
                "A star-rating component registered under a key with RegisterCustom, validated like any built-in control.",
                "custom renderer RegisterCustom CustomRenderer BormInputBase registry widget star rating",
                typeof(CustomRendererDemo)),
        ]),
        new("Repeaters & wizards",
        [
            new("/array", "Array / repeater",
                "A repeating editor with add, remove and reorder, computed line and invoice totals, and focus that follows the rows.",
                "array repeater list items add remove reorder move computed total Items min itemNoun Disabled",
                typeof(ArrayDemo)),
            new("/per-row-conditions", "Per-row conditions",
                "A condition on a repeater's item template means that row: each contact switches its own fields.",
                "per row condition repeater scoped VisibleWhen RequiredWhen ClearOnHide SeedMinItems duplicable",
                typeof(PerRowConditionsDemo)),
            new("/wizard", "Multi-step wizard",
                "Steps that validate before advancing, a step that only appears for business accounts, and a clickable stepper.",
                "wizard steps stepper Step multi-step per-step validation conditional step Next Back",
                typeof(WizardDemo)),
        ]),
        new("Validation",
        [
            new("/validation", "Validation modes",
                "When a field revalidates before and after the first submit, an error summary, Reset, and a debounced async rule.",
                "validation trigger revalidation OnBlur OnChange OnSubmit error summary reset MustAsync async debounce IsValidatingField read-only",
                typeof(ValidationModesDemo)),
            new("/dependent-fields", "Dependent fields",
                "RevalidateOn settles a cross-field rule from either side, and clearing a hidden field updates the computed estimate.",
                "dependent fields RevalidateOn cross-field compare Must confirm email dates computed ClearOnHide deps",
                typeof(DependentFieldsDemo)),
            new("/settled-answers", "Locked fields, tidy values & date windows",
                "ReadOnlyWhen locks a settled answer, Trim and Normalize fix values instead of complaining, and date ranges are enforced.",
                "ReadOnlyWhen read-only lock Trim Normalize date time Range min max IsSubmitSuccessful",
                typeof(SettledAnswersDemo)),
            new("/data-annotations", "DataAnnotations & IValidatableObject",
                "UseDataAnnotations() runs a model's cross-property IValidatableObject rules, composed with a FluentValidation validator.",
                "dataannotations IValidatableObject UseDataAnnotations attributes Required Range cross-property form-level compose",
                typeof(DataAnnotationsDemo)),
            new("/fluent-validation", "FluentValidation",
                "UseFluentValidation() runs an existing AbstractValidator<T> and maps every failure onto its field.",
                "fluentvalidation AbstractValidator UseFluentValidation Bit.Borm.FluentValidation validator rules cross-field",
                typeof(FluentValidationDemo)),
        ]),
        new("State & data",
        [
            new("/cascading", "Async & cascading options",
                "OptionsFrom loads choices on demand and reloads them when the field they depend on changes.",
                "async cascading options OptionsFrom dependsOn load country city select RequiredWhen ClearOnHide",
                typeof(CascadingOptionsDemo)),
            new("/state-api", "Editing an existing record",
                "Reset(values) rebases a loaded record, with Snapshot, ValidateFieldAsync, GetFieldState and the repeater operations.",
                "BormState state api Reset Snapshot draft ValidateFieldAsync GetFieldState SwapArrayItems ClearArrayItems SingleErrorPerField ReadOnly dirty",
                typeof(StateApiDemo)),
        ]),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing.",
                "mcp model context protocol ai agent tools resources prompts claude copilot server InspectBormSchema",
                typeof(McpServer)),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = [.. Sections.SelectMany(s => s.Pages)];

    /// <summary>The current address relative to the base, without its query string or fragment.</summary>
    public static string PathOf(NavigationManager nav)
    {
        var path = nav.ToBaseRelativePath(nav.Uri);
        var cut = path.IndexOfAny(['?', '#']);
        return (cut >= 0 ? path[..cut] : path).Trim('/');
    }

    /// <summary>Whether a link's address and a base-relative path name the same page, ignoring case as the router does.</summary>
    public static bool IsSamePath(string href, string path)
        => string.Equals(href.Trim('/'), path.Trim('/'), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Finds the catalog entry for a base-relative path, or null when the page is not in the catalog. A
    /// page whose route takes a parameter is listed by its bare address, so a deeper path falls back to
    /// the entry it starts with.
    /// </summary>
    public static DocsPageInfo? FindByPath(string path)
        => AllPages.FirstOrDefault(p => IsSamePath(p.Url, path))
           ?? AllPages.FirstOrDefault(p => path.Trim('/').StartsWith(p.Url.Trim('/') + "/", StringComparison.OrdinalIgnoreCase));

    /// <summary>Previous/next pages in reading order for the bottom-of-page pager.</summary>
    public static (DocsPageInfo? Previous, DocsPageInfo? Next) NeighborsOf(DocsPageInfo page)
    {
        var index = Array.IndexOf(AllPages, page);
        if (index < 0) return (null, null);
        return (index > 0 ? AllPages[index - 1] : null,
                index < AllPages.Length - 1 ? AllPages[index + 1] : null);
    }

    /// <summary>Section a page belongs to (the breadcrumb's middle step and the palette's group label).</summary>
    public static string SectionOf(DocsPageInfo page)
        => Sections.First(s => s.Pages.Contains(page)).Title;
}
