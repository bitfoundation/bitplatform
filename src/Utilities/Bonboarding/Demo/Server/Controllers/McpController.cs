using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Bonboarding.Demo.Server.Dtos;
using Bit.Bonboarding.Demo.Server.Services;

namespace Bit.Bonboarding.Demo.Server.Controllers;

/// <summary>
/// The Bonboarding MCP server: the tools an AI agent calls to build tours with Bit.Bonboarding
/// without guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Bonboarding, the README, the shipped stylesheet, the library's
/// own placement engine, and the demo's source files - so an agent gets what the current version
/// actually does rather than a snapshot someone wrote down. The same methods are exposed as plain
/// HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
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
/// call, a computed placement - set <c>UseStructuredContent</c> and publish an output schema. The
/// rest answer in Markdown: a structured answer goes over the wire twice, once as the object and once
/// as the JSON text the spec asks for on its behalf, and documentation read as Markdown is smaller
/// than either half.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // The README's longest sections and the demo's longest pages land far below the cap; it is
    // there so a pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 40_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBonboarding), Title = "Search everything about Bit.Bonboarding", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Bonboarding at once - the reference guide, every public type and member, the theme variables and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'advance when the user clicks', 'element not rendered yet', 'branching', 'resume progress', 'dark theme'.")]
    public BonboardingSearchResultDto SearchBonboarding(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BonboardingSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBonboardingSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Bonboarding to a Blazor app in one render mode: 'server' (Blazor Web App, InteractiveServer), 'wasm' (InteractiveWebAssembly), 'auto' (InteractiveAuto) or 'standalone-wasm'. The Web App modes come with the real files of this demo. Call this before writing any setup code - which DI containers register the services, where the stylesheet and the BonboardingRoot host go, and where tours may start differ per render mode.")]
    public string GetBonboardingSetupGuide(
        // Declared rather than merely described: the four values then sit in the JSON schema itself,
        // where a model cannot pass a fifth past them, and a client offers them as completions.
        [AllowedValues("server", "wasm", "auto", "standalone-wasm")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return BonboardingSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BonboardingSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBonboardingGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Bonboarding reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'Dynamic and async targets', 'Conditional and branching flows', 'Multi-page tours', 'Persistence', 'Headless and custom rendering', 'API reference'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBonboardingGuideSection(
        [Description("The section's heading, e.g. 'Multi-page tours'. Case and punctuation are ignored. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BonboardingSourceCatalog.RenderGuideIndex();

        var section = BonboardingSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BonboardingSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Heading}'")
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBonboardingGuideSection with no heading for the index of sections.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBonboardingApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Bonboarding as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BonboardingTour', 'BonboardingStep', 'BonboardingRoot', 'BonboardingStepDefinition', 'IBonboardingService', 'IBonboardingTourSession', 'BonboardingOptions'.")]
    public string GetBonboardingApi(
        [Description("The type's name without its namespace, e.g. 'BonboardingStep' or 'BonboardingOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BonboardingApiCatalog.RenderIndex();

        var rendered = BonboardingApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BonboardingApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Bonboarding has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Bonboarding has no public type called '{typeName}'. Every public type is prefixed 'Bonboarding' (e.g. 'BonboardingTourDefinition' rather than 'TourDefinition'); call GetBonboardingApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBonboardingThemeVariables), Title = "List the theme variables", ReadOnly = true, OpenWorld = false)]
    [Description("Lists every CSS custom property the default Bit.Bonboarding theme is built from - palette, shape, typography, metrics and motion - with its light value and its dark-theme override, read out of the shipped stylesheet, as Markdown tables. Restyle tours by overriding these on .bit-bonboarding-root rather than by overriding the library's selectors.")]
    public string GetBonboardingThemeVariables()
    {
        return BonboardingThemeCatalog.Render();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBonboardingPlacement), Title = "Compute where a popover lands", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs Bit.Bonboarding's own placement engine on a set of measurements and reports where the popover goes: its side, alignment and rectangle, the arrow, whether the requested placement was kept or the engine had to slide or flip, and how much could not fit - with notes on which option to change. Use it to choose a Placement or PlacementFallbacks for a target near an edge, or to explain why a popover did not land where it was asked to. All lengths are CSS pixels in viewport coordinates.")]
    public BonboardingPlacementAnalysisDto InspectBonboardingPlacement(
        [Description("Left edge of the target element, in viewport pixels (getBoundingClientRect().left).")] double targetX,
        [Description("Top edge of the target element, in viewport pixels.")] double targetY,
        [Description("Width of the target element. 0 means there is no target, which shows the step as a centred card.")] double targetWidth,
        [Description("Height of the target element.")] double targetHeight,
        [Description("Width of the popover. The default stylesheet makes it 22rem (352px at a 16px root font size), narrower on small screens.")] double popoverWidth = 352,
        [Description("Height of the popover - it depends on the step's content; a title, two lines of text and the footer are about 180px.")] double popoverHeight = 180,
        [Description("Viewport width.")] double viewportWidth = 1280,
        [Description("Viewport height.")] double viewportHeight = 800,
        [Description("The requested placement: Auto, Top, TopStart, TopEnd, Bottom, BottomStart, BottomEnd, Left, LeftStart, LeftEnd, Right, RightStart, RightEnd or Center.")] string placement = "Auto",
        [Description("Optional explicit fallback order, comma-separated (e.g. 'Top, Right'), as PlacementFallbacks would hold it. Omit it to let the engine derive one.")] string? fallbacks = null,
        [Description("Gap between the spotlight and the popover (the Offset option). Default 12.")] double offset = BonboardingDefaults.Offset,
        [Description("Minimum distance kept from the viewport edges (the ViewportPadding option). Default 16.")] double viewportPadding = BonboardingDefaults.ViewportPadding,
        [Description("Arrow size; 0 hides it (the ArrowSize option). Default 10.")] double arrowSize = BonboardingDefaults.ArrowSize,
        [Description("Breathing room the spotlight adds around the target, which the popover is positioned against (the SpotlightPadding option). Default 8.")] double spotlightPadding = BonboardingDefaults.SpotlightPadding,
        [Description("True for a right-to-left layout, which mirrors Start and End on the top and bottom sides.")] bool rightToLeft = false)
    {
        var requested = BonboardingPlacementLab.ParsePlacement(placement);

        var notes = new List<string>();

        if (requested is null)
        {
            notes.Add($"'{placement}' is not a placement, so Auto was used. Use one of: {string.Join(", ", BonboardingPlacementLab.PlacementNames)}.");
        }

        List<BonboardingPlacement>? fallbackList = null;

        if (string.IsNullOrWhiteSpace(fallbacks) is false)
        {
            fallbackList = [];

            foreach (var item in fallbacks.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (BonboardingPlacementLab.ParsePlacement(item) is { } parsed) fallbackList.Add(parsed);
                else notes.Add($"The fallback '{item}' is not a placement and was ignored.");
            }
        }

        var analysis = BonboardingPlacementLab.Analyze(
            new BonboardingRect(targetX, targetY, Math.Max(0, targetWidth), Math.Max(0, targetHeight)),
            new BonboardingSize(Math.Max(0, popoverWidth), Math.Max(0, popoverHeight)),
            new BonboardingRect(0, 0, Math.Max(0, viewportWidth), Math.Max(0, viewportHeight)),
            requested ?? BonboardingPlacement.Auto,
            fallbackList,
            Math.Max(0, offset),
            Math.Max(0, viewportPadding),
            Math.Max(0, arrowSize),
            Math.Max(0, spotlightPadding),
            rightToLeft,
            hasTarget: targetWidth > 0 && targetHeight > 0);

        return notes.Count == 0 ? analysis : analysis with { Notes = [.. notes, .. analysis.Notes] };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBonboardingSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Bonboarding source file verbatim - e.g. 'Demo/Client/Pages/Flows.razor' (conditional and branching tours), 'Demo/Client/Pages/Dynamic.razor' (late and missing targets), 'Demo/Client/Shared/WorkspaceTour.cs' (a multi-page tour as data) or 'Demo/Client/Pages/Headless.razor' (fully custom popovers). Omit the path for the index of files: the demo pages, each a working tour, and the hosting files of a Blazor Web App. These are worked examples - for what a parameter means, GetBonboardingApi answers in a fraction of the characters.")]
    public string GetBonboardingSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/Home.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BonboardingSourceCatalog.RenderSourceIndex();

        var content = BonboardingSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BonboardingSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBonboardingSourceFile with no path for the index of files.";
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
