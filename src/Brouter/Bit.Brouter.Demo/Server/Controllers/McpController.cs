using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Dtos;
using Bit.Brouter.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Brouter.Demo.Server.Controllers;

/// <summary>
/// The Brouter MCP server: the tools an AI agent calls to build features with Bit.Brouter without
/// guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Brouter, the README, the docs pages rendered by the very router
/// they describe, and the demo's source files - so an agent gets what the current version actually
/// does rather than a snapshot someone wrote down. The same methods are exposed as plain HTTP GET
/// endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController(HtmlRenderer htmlRenderer, IOptions<BrouterOptions> brouterOptions) : ControllerBase
{
    // The docs pages are rich enough that a couple of them would otherwise dominate a client's
    // context window; the ones in this demo land far below the cap.
    private const int MaxDocumentLength = 40_000;

    // The rendered Markdown of every docs page served so far, keyed by slug.
    private static readonly ConcurrentDictionary<string, string> _renderedPages = new(StringComparer.Ordinal);

    private static readonly string BrouterVersion =
        typeof(BrouterLink).Assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BrouterLink).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterOverview))]
    [Description("Start here. Explains what Bit.Brouter is, how to install and register it, shows a minimal working router, and lists which of the other Brouter tools to call for what.")]
    public string GetBrouterOverview()
    {
        var builder = new StringBuilder();

        var readme = BrouterSourceCatalog.Readme;
        var firstSection = readme.IndexOf("\n## ", StringComparison.Ordinal);
        builder.AppendLine(firstSection > 0 ? readme[..firstSection].Trim() : readme).AppendLine();

        // Which build the answers come from: every tool below reflects THIS assembly, not a remembered version.
        builder.AppendLine($"_These tools answer from Bit.Brouter {BrouterVersion}, loaded in this server._").AppendLine();

        AppendGuideSection(builder, "Install");
        AppendGuideSection(builder, "Quick start");
        AppendGuideSection(builder, "Features");

        builder.AppendLine("""
            ---

            ## Which tool to call

            - `SearchBrouter` - **the default entry point.** One query across the guide, the docs pages, every public
              type and member, the constraints and the demo's sources; each hit carries the exact follow-up call.
              Reach for it whenever you do not already know the section, slug or type name you want.
            - `GetBrouterSetupGuide` - the complete wiring for one Blazor render mode ('server', 'wasm', 'auto',
              'standalone-wasm'), as the real files of a working project. Start here when adding Brouter to an app.
            - `GetBrouterGuideSections` / `GetBrouterGuideSection` - the library's own reference guide, one topic at a
              time, with copy-pasteable code. The fastest route to a working implementation of a specific feature
              (guards, loaders, keep-alive, view transitions, typed routes, migration, ...).
            - `GetBrouterDocsList` / `GetBrouterDocsPage` - the documentation site's pages: the same topics written as
              narrative documentation, with the live routes that demonstrate them.
            - `GetBrouterApiList` / `GetBrouterApiDetails` - the exact public API: every component parameter with its
              type and default value, every service member, option and enum, straight out of the shipped assembly.
              Call this before writing code against a member you are not sure about.
            - `GetBrouterRouteConstraints` - every constraint usable in a route template, with a passing and a failing
              example for each.
            - `InspectBrouterRouteTemplate` / `AnalyzeBrouterRouteTable` - check a template, or a whole set of them,
              with Brouter's own parser before you ship it: parameters, constraints, specificity ranking, ambiguous
              pairs, and the exact error for an invalid template.
            - `GetBrouterTypedRoutes` - what the optional source generator emits: compile-time-safe URL builders and
              route-name constants, shown from a real project.
            - `GetBrouterSourceFiles` / `GetBrouterSourceFile` - real, working source: the whole route table of this
              site (`Demo/Client/AppRouter.razor`), every demo page behind it, and the minimal hosting samples for
              each Blazor render mode (`Sample/Wasm/...`, `Sample/Server/...`, `Sample/Auto/...`).

            ## Rules of thumb when writing Brouter code

            - Register the services once per Blazor DI container with `AddBitBrouterServices` - in a Blazor Web App
              that means both the server and the client project - and add `@using Bit.Brouter` to `_Imports.razor`.
            - Route templates use the built-in Blazor router's grammar, so `@page` templates port over verbatim.
            - Declared routes (`<Broute Path="...">`) and discovered `@page` components can be mixed: set
              `AppAssembly`/`AdditionalAssemblies` on `<Brouter>` to enable discovery.
            - A route renders either a `Component` or a `<Content>` fragment, never both.
            - Nested routes need a `<BrouterOutlet />` in the parent's content, otherwise children have nowhere to go.
            """);

        return builder.ToString();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBrouter))]
    [Description("Searches everything known about Bit.Brouter at once - the reference guide, the documentation pages, every public type and member, the route constraints and the demo's source files - and returns the best matches, each with the exact follow-up tool call that returns its full text. Use this first whenever you do not already know which section, page or type holds the answer. Example queries: 'block navigation unsaved changes', 'cache loader data', 'keep component alive', 'query string binding'.")]
    public BrouterSearchHitDto[] SearchBrouter(string query, int limit = 12)
    {
        return BrouterSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterSetupGuide))]
    [Description("Gets the complete wiring needed to add Bit.Brouter to a Blazor app in one render mode, as the real files of a working project: 'server' (Blazor Web App, InteractiveServer), 'wasm' (InteractiveWebAssembly), 'auto' (InteractiveAuto) or 'standalone-wasm'. Call this before writing any setup code - which DI container registers the services and where the catch-all host page lives differ per render mode.")]
    public string GetBrouterSetupGuide(string renderMode)
    {
        return BrouterSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BrouterSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterTypedRoutes))]
    [Description("Explains the optional Bit.Brouter.Generators source generator and shows its real output: the compile-time-safe URL builders and route-name constants it emitted for this documentation site's own route table. Call it when asked for typed/compile-safe URLs or BrouterRoutes.")]
    public object GetBrouterTypedRoutes()
    {
        return (object?)BrouterTypedRoutesCatalog.TypedRoutes
            ?? "The typed-route generator did not run for this build, so there is no generated output to show. " +
               "Call GetBrouterGuideSection(heading: \"Typed routes (source generator)\") for how it works.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterDocsList))]
    [Description("Lists the pages of the Bit.Brouter documentation site with their descriptions and search keywords. Use it to pick the slug to pass to GetBrouterDocsPage.")]
    public BrouterDocsPageDto[] GetBrouterDocsList()
    {
        return [.. DocsCatalog.Sections.SelectMany(section => section.Pages.Select(page => new BrouterDocsPageDto
        {
            Section = section.Title,
            Slug = page.Slug,
            Url = page.Url,
            Title = page.Title,
            Description = page.Description,
            Keywords = page.Keywords
        }))];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterDocsPage))]
    [Description("Gets one page of the Bit.Brouter documentation site as Markdown, including its code samples. Pass a slug from GetBrouterDocsList, e.g. 'guards', 'data-loading' or 'route-templates'. Omit it for the documentation overview.")]
    public async Task<string> GetBrouterDocsPage(string? slug = null)
    {
        // The overview's own slug is the empty string; agents reach for a word instead.
        if (slug is "overview" or "index" or "docs") slug = string.Empty;

        var page = DocsCatalog.FindBySlug(slug);

        if (page is null)
        {
            var slugs = string.Join(", ", DocsCatalog.AllPages.Select(p => p.Slug.Length == 0 ? "(empty)" : p.Slug));

            return $"No documentation page has the slug '{slug}'. Available slugs: {slugs}.";
        }

        // Rendering a page and flattening it costs far more than serving it; the pages are static,
        // so the first caller pays for it and everyone after reads the same Markdown.
        if (_renderedPages.TryGetValue(page.Slug, out var cached)) return cached;

        // The page is rendered by the same component the site serves, so the documentation an agent
        // reads is the documentation a human reads - there is no second copy that could go stale.
        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await htmlRenderer.RenderComponentAsync(page.PageType);

            return component.ToHtmlString();
        });

        // The page renders its own <h1>, so only its source is prepended here.
        var markdown = Truncate($"Bit.Brouter documentation page: {page.Url}\n\n{html.ToMarkdown()}");

        _renderedPages[page.Slug] = markdown;

        return markdown;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterGuideSections))]
    [Description("Lists every section of the Bit.Brouter reference guide (the library's README), with its heading and size. Use it to pick the heading to pass to GetBrouterGuideSection.")]
    public BrouterGuideSectionDto[] GetBrouterGuideSections()
    {
        return BrouterSourceCatalog.GuideSections;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterGuideSection))]
    [Description("Gets one section of the Bit.Brouter reference guide as Markdown, with its code samples - e.g. 'Async guards', 'Data loader', 'Keep-alive routes', 'Typed routes (source generator)'. Sub-sections are included. Heading matching ignores case and punctuation.")]
    public string GetBrouterGuideSection(string heading)
    {
        var section = BrouterSourceCatalog.GetGuideSection(heading);

        if (section is null)
        {
            var headings = string.Join(", ", BrouterSourceCatalog.GuideSections.Select(s => $"'{s.Heading}'"));

            return $"The guide has no section called '{heading}'. Available sections: {headings}.";
        }

        return Truncate(section);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterApiList))]
    [Description("Lists every public type of the Bit.Brouter library - components, services, options, enums and value types - with its kind and summary. Use it to pick the type to pass to GetBrouterApiDetails.")]
    public BrouterApiTypeDto[] GetBrouterApiList()
    {
        return BrouterApiCatalog.Types;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterApiDetails))]
    [Description("Gets the full reference of one Bit.Brouter type: its Blazor parameters with types and default values, its properties, methods, events or enum values, each with its documentation. Call it before using a member you are unsure about, e.g. 'Broute', 'Brouter', 'BrouterLink', 'IBrouter', 'BrouterOptions', 'BrouterNavigationContext'.")]
    public BrouterApiDetailsResultDto GetBrouterApiDetails(string typeName)
    {
        var details = BrouterApiCatalog.GetTypeDetails(typeName);

        if (details is not null) return new BrouterApiDetailsResultDto { Details = details };

        var candidates = BrouterApiCatalog.Types
            .Where(t => t.Name.Contains(typeName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .ToArray();

        return new BrouterApiDetailsResultDto
        {
            Message = candidates.Length > 0
                ? $"Bit.Brouter has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"Bit.Brouter has no public type called '{typeName}'. Call GetBrouterApiList for the full list."
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterRouteConstraints))]
    [Description("Lists every route constraint usable inside a Bit.Brouter route template - the built-in type and validation constraints, a custom one, and constraint chaining - each with the rule it enforces plus a passing and a failing example.")]
    public BrouterConstraintDto[] GetBrouterRouteConstraints()
    {
        return [.. ConstraintCatalog.All.Select(constraint => new BrouterConstraintDto
        {
            Token = constraint.Token,
            Category = constraint.Category,
            Rule = constraint.Rule,
            PassExample = constraint.PassExample,
            FailExample = constraint.FailExample,
            // The example is one path segment: escaped, so a value carrying a slash, a space or a
            // '#' still produces the URL that exercises the constraint rather than a different one.
            TryUrl = $"/c/{constraint.Kind}/{Uri.EscapeDataString(constraint.PassExample)}"
        })];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBrouterRouteTemplate))]
    [Description("Parses a route template with Bit.Brouter's own parser and reports what it means: its segments, parameter names, constraints, default values, specificity, and notes about behavior that is easy to get wrong. An invalid template comes back with the exact error the router would throw. Example inputs: '/users/{id:int}', '/files/{name}.{ext?}', '/assets/{*path:nonfile}'.")]
    public BrouterTemplateInspectionDto InspectBrouterRouteTemplate(string template)
    {
        // The app's own constraint registry, so custom constraints registered in
        // AddBitBrouterServices (this demo registers "slug") resolve exactly as they do at runtime.
        return BrouterTemplateInspector.Inspect(template, brouterOptions.Value.Constraints);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(AnalyzeBrouterRouteTable))]
    [Description("Parses a whole set of route templates together and reports how they relate: the order in which the router prefers them when several match the same URL (by specificity), any templates that are indistinguishable - Brouter throws at registration for those - and the exact error for each invalid one. Pass the templates separated by newlines, commas or semicolons. Use it after adding routes to an existing table.")]
    public BrouterRouteTableAnalysisDto AnalyzeBrouterRouteTable(string templates)
    {
        var parts = (templates ?? string.Empty).Split(['\n', '\r', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return BrouterTemplateInspector.Analyze(parts, brouterOptions.Value.Constraints);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterSourceFiles))]
    [Description("Lists the working Bit.Brouter source files this server can hand out: the whole route table of the documentation site, every page it routes to, and the minimal hosting samples for each Blazor render mode. Use it to pick the path to pass to GetBrouterSourceFile.")]
    public BrouterSourceFileDto[] GetBrouterSourceFiles()
    {
        return BrouterSourceCatalog.SourceFiles;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBrouterSourceFile))]
    [Description("Gets one source file listed by GetBrouterSourceFiles, verbatim - e.g. 'Demo/Client/AppRouter.razor' for a complete, working route table that exercises nearly every Bit.Brouter feature.")]
    public string GetBrouterSourceFile(string path)
    {
        var content = BrouterSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BrouterSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBrouterSourceFiles for the full list.";
        }

        return Truncate(content);
    }

    /// <summary>
    /// A renamed README heading must not silently leave a blank gap in the overview - the agent is
    /// told where the text went instead.
    /// </summary>
    private static void AppendGuideSection(StringBuilder builder, string heading)
    {
        builder.AppendLine(BrouterSourceCatalog.GetGuideSection(heading)
                           ?? $"_The guide's \"{heading}\" section was not found in this build. " +
                              $"Call GetBrouterGuideSections for the sections it does have._")
               .AppendLine();
    }

    private static string Truncate(string text)
    {
        return text.Length <= MaxDocumentLength
            ? text
            : $"{text[..MaxDocumentLength]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
