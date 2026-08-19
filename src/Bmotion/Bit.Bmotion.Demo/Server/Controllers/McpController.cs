using System.Text;
using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Bit.Bmotion.Demo.Server.Dtos;
using Bit.Bmotion.Demo.Server.Services;
using Bit.Bmotion.Demo.Client.Shared;

namespace Bit.Bmotion.Demo.Server.Controllers;

/// <summary>
/// The Bmotion MCP server: the tools an AI agent calls to build animations with Bit.Bmotion
/// without guessing at its API - or at what the motion will look like.
/// <para>
/// Documentation tools alone are not enough for an animation library. An agent can read every
/// parameter of <c>Bm.Spring</c> and still not know that
/// <c>Bm.Spring(stiffness: 400, damping: 2)</c> wobbles for five seconds, or that animating
/// <c>height</c> stops working the moment the app is served over Blazor Server. Both facts live in
/// the engine, not in prose. So the tools here fall into two halves: the ones that answer from
/// this build's own text - the guide, the XML documentation compiled into the assembly, the demo's
/// sources - and the ones that answer by <b>running the real engine off-screen</b> and reporting
/// what it did.
/// </para>
/// <para>
/// Every method is also a plain HTTP GET endpoint under <c>/api/mcp/...</c>, which makes each of
/// them inspectable from a browser - and is what the demo's own MCP page calls to show the tools
/// working live.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // Long enough for the largest guide section, short enough that a couple of tool calls cannot
    // crowd out a client's context window. McpResources reads the same bound: the resources hand
    // out the same documents, so a client that pins one instead of calling the tool gets the same
    // text rather than an unbounded one.
    //
    // 40,000 was one bound too far: it is around ten thousand tokens for a single answer, and the
    // longest demo pages are long enough to spend all of it. 15,000 still clears the largest guide
    // section (13,148 characters, "Components") whole, and what it does cut - the half-dozen demo
    // pages above it - GetBmotionSourceFile can now be asked for a line range of instead, which is
    // a better answer than a page and a half of markup nobody asked for.
    public const int MaxDocumentLength = 15_000;

    private static readonly string BmotionVersion =
        typeof(Bm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Bm).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionOverview))]
    [Description("Explains what Bit.Bmotion is, how to install and register it, the one thing that decides whether an animation works on Blazor Server, and the rules that are not visible in any signature. Worth reading once per session; for a specific question, call SearchBmotion instead.")]
    public string GetBmotionOverview()
    {
        var builder = new StringBuilder();

        var readme = BmotionSourceCatalog.Readme;
        var firstSection = readme.IndexOf("\n## ", StringComparison.Ordinal);

        builder.AppendLine(firstSection > 0 ? readme[..firstSection].Trim() : readme).AppendLine();

        // Which build the answers come from: every tool below reflects THIS assembly, not a
        // remembered version.
        builder.AppendLine($"_These tools answer from Bit.Bmotion {BmotionVersion}, loaded in this server._").AppendLine();

        AppendGuideSection(builder, "Installation");
        AppendGuideSection(builder, "Quick Start");

        // Deliberately no tool directory here. The client already holds every tool's description
        // from tools/list, and restating them was the largest single block of this answer - paid on
        // every call, to say what the caller could already read. What is left is the part that is
        // nowhere else: the order to work in, and the rules that are not visible in any signature.
        builder.AppendLine("""
            ---

            ## How to work

            1. `SearchBmotion` with the request in the words it arrived in. Every hit carries the exact
               follow-up call, so it is the one tool worth calling before you know any names.
            2. `GetBmotionRecipe` if the search turned up a recipe: they are complete and carry the caveat
               that is not visible in the code.
            3. `GetBmotionApiDetails` for every type you are about to use. In an animation library the
               default values *are* the behaviour.
            4. `SimulateBmotionTransition` to choose the transition on measurements rather than adjectives.
            5. `AnalyzeBmotionAnimation` before finishing, on anything that is not WebAssembly-only - it is
               what decides whether the animation plays or silently snaps on Blazor Server.
            6. `ReviewBmotionCode` on what you wrote.

            ## Rules of thumb when writing Bmotion code

            - **Only transform components (`x`, `y`, `z`, `scale`, `rotate`, `skew`, `perspective`) and `opacity`
              can be handed to the browser compositor.** Those animations play everywhere, including Blazor Server.
              Everything else - colour, `width`/`height`, keyframe arrays, drag, motion values - needs the C# frame
              loop, which exists only on WebAssembly. On Server it becomes an instant jump, with no error anywhere.
            - Prefer `x`/`y` over `top`/`left`, and `scale` over `width`/`height`. Same visual result, no layout
              cost, and it stays on the compositor.
            - `Initial` runs on mount only. To replay an entrance, change the element's `@key`.
            - `Exit` needs a presence component (`BmotionAnimatePresence`, `BmotionPresenceGroup`,
              `BmotionPresenceSwitch`) around it. Wrapping the content in `@if` instead is the single most common
              Bmotion bug: Blazor removes the element before the animation can start.
            - Gesture overlays (`WhileHover`, `WhileTap`, ...) revert on their own. Do not also write the resting
              state into them.
            - Orchestration - `staggerChildren`, `delayChildren`, `when`, `childStagger` - belongs on the
              **container's** transition, not on the children.
            - A spring takes either form, never both. `Bm.Spring(bounce:, duration:)` describes the feel and
              derives the physics from it - either argument alone is enough, the other one defaults. Passing
              `stiffness`/`damping` as well does not add to it: those values are then never used.
            - Register the services in every DI container the components run in - in a Blazor Web App that means
              both the server and the client project - and add `@using Bit.Bmotion` to `_Imports.razor`.
            """);

        return builder.ToString();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBmotion))]
    [Description("Start here. Searches everything known about Bit.Bmotion at once - the guide, every public type and member, the animatable properties, the easing presets, the ready-made recipes and the demo's source files - and returns the best matches, each with the exact follow-up tool call that returns its full text. Call it whenever you do not already know which section, type or recipe holds the answer. Example queries: 'make a list appear one item at a time', 'animate something out before it is removed', 'drag within bounds', 'why does my animation not run on the server'.")]
    public Task<BmotionSearchHitDto[]> SearchBmotion(string query, int limit = 12)
    {
        return BmotionSearchIndex.SearchAsync(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionSetupGuide))]
    [Description("Gets the complete wiring needed to add Bit.Bmotion to a Blazor app in one render mode, as the real files of a working project: 'wasm' (Blazor Web App, InteractiveWebAssembly), 'server' (InteractiveServer), 'auto' (InteractiveAuto) or 'standalone-wasm'. Call this before writing any setup code - which DI containers register the services, and which library features work at all, both differ per render mode.")]
    public string GetBmotionSetupGuide(string renderMode)
    {
        return BmotionSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BmotionSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionRecipes))]
    [Description("Lists every ready-made Bit.Bmotion pattern this server can hand out complete, with its id, its intent and the terms it covers - entrances, exits, gestures, scroll, layout, drag, text and more. Use it to pick the id to pass to GetBmotionRecipe. A recipe is usually the fastest route from a request to correct code, so this is worth a call before writing an animation by hand.")]
    public BmotionRecipeDto[] GetBmotionRecipes()
    {
        return BmotionRecipeCatalog.Summaries;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionRecipe))]
    [Description("Gets one Bit.Bmotion recipe in full: the Razor markup, and the caveat that is not visible in the code - the missing @key, the presence component it needs, the render mode it will not survive. Pass an id from GetBmotionRecipes, e.g. 'staggered-list', 'exit-animation', 'modal-dialog', 'reveal-on-scroll', 'shared-element'.")]
    public BmotionRecipeResultDto GetBmotionRecipe(string id)
    {
        var recipe = BmotionRecipeCatalog.Find(id);

        if (recipe is not null) return new BmotionRecipeResultDto { Recipe = recipe };

        var ids = string.Join(", ", BmotionRecipeCatalog.All.Select(entry => entry.Id));

        return new BmotionRecipeResultDto
        {
            Message = $"There is no recipe called '{id}'. Available recipes: {ids}. " +
                      "Call SearchBmotion with what you are trying to build if none of them fit."
        };
    }

    // Comparing used to be a second tool. It was the same call with a plural argument - simulating
    // one transition is comparing one - and an agent that had read only one of the two descriptions
    // either ran three separate simulations or never learned it could ask for three at once.
    public const int MaxSimulatedTransitions = 8;

    [HttpGet]
    [McpServerTool(Name = nameof(SimulateBmotionTransition))]
    [Description("Runs one or more transitions on the real Bit.Bmotion engine, off-screen, and reports what the motion actually does: how long it takes to come to rest, how far it overshoots its target, how many times it wobbles, and the shape of the curve. No transition states its own settle time - a spring's falls out of the physics, and a tween's duration says nothing about how the value gets there - so this is the only way to know what one feels like without opening a browser. Pass several, separated by semicolons or newlines, to measure them side by side and choose between them on evidence rather than on the names of the arguments. Accepts 'spring(stiffness: 260, damping: 12)', 'spring(bounce: 0.4, duration: 0.6)', 'tween(0.4, InOut)' or 'inertia(velocity: 500)'.")]
    public async Task<BmotionSimulationDto[]> SimulateBmotionTransition(
        [Description("The transition, e.g. 'spring(stiffness: 260, damping: 12)'. A Bm.Spring(...) call copied out of Razor works verbatim. Several transitions separated by semicolons or newlines are measured side by side, e.g. 'spring(stiffness: 260, damping: 12); spring(bounce: 0.2, duration: 0.4); tween(0.3, BackOut)'.")] string transition,
        [Description("The value the animation starts at. The default of 0 to 100 reads as a percentage of the distance travelled.")] double from = 0,
        [Description("The value the animation targets.")] double to = 100,
        [Description("Also return the sampled (seconds, value) points of the curve. Off by default: the sparkline and the measurements answer 'what does this feel like', and the raw samples are only worth their size when the curve is being plotted or differentiated.")] bool includeSamples = false)
    {
        var specs = (transition ?? string.Empty)
            .Split([';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // Simulating dozens at once would spend more of the client's context than any comparison
            // is read with; the interesting comparisons are between two and four candidates.
            .Take(MaxSimulatedTransitions)
            .ToArray();

        // An empty argument is still a question - the lab answers it with the default tween, and
        // saying nothing at all would read as a server that failed rather than a spec that was blank.
        if (specs.Length == 0) specs = [string.Empty];

        // Every run owns its engine and its frame clock (see BmotionMotionLab), so the comparison
        // costs one simulation rather than the sum of them. Task.WhenAll keeps the order asked for.
        var results = await Task.WhenAll(specs.Select(spec => BmotionMotionLab.SimulateAsync(spec, from, to)));

        return includeSamples ? results : [.. results.Select(result => result with { Samples = [] })];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(AnalyzeBmotionAnimation))]
    [Description("Starts an animation on the real Bit.Bmotion engine and reports which playback path the engine chose for it: the browser compositor (Web Animations API), or the C# per-frame loop. That choice is what decides whether the animation plays on Blazor Server or silently collapses into an instant state change there - nothing in the build output or the browser console says which. Call it before finishing any animation for an app that is not WebAssembly-only. When the answer is the frame loop, it also says why, and what to change.")]
    public Task<BmotionPlaybackDto> AnalyzeBmotionAnimation(
        [Description("The animated properties by their Bm.To(...) argument names, separated by commas, e.g. 'x, opacity' or 'width, backgroundColor'.")] string properties,
        [Description("The transition, e.g. 'spring(stiffness: 200, damping: 20)' or 'tween(0.4, InOut)'. Defaults to a plain tween.")] string? transition = null)
    {
        var names = (properties ?? string.Empty)
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        return BmotionMotionLab.AnalyzePlaybackAsync(names, transition);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(ReviewBmotionCode))]
    [Description("Reviews Razor or C# that uses Bit.Bmotion and reports the mistakes that compile cleanly and then do nothing: an Exit with no presence component around it, an animated element in a loop with no @key, a spring whose stiffness the engine discards, a nested-quote attribute that does not parse as intended, properties that will snap rather than animate on Blazor Server. Every finding names the line and the correction. Run this on animation code before calling it done - none of these produce a compiler warning, an exception or a console message.")]
    public BmotionReviewDto ReviewBmotionCode(
        [Description("The Razor markup or C# to review. A component, a fragment, or just the <Bmotion> element in question.")] string code)
    {
        var text = code ?? string.Empty;

        // Every rule below is a pass over every line, so the reviewed body is bounded by the same
        // limit as the documents this server hands out. What was cut is reported rather than dropped:
        // a review that silently covered the first half of a file would read as a clean bill of health.
        if (text.Length <= MaxDocumentLength) return BmotionCodeReview.Review(text);

        var review = BmotionCodeReview.Review(text[..MaxDocumentLength]);

        return review with
        {
            Passed = false,
            Findings =
            [
                new BmotionReviewFindingDto
                {
                    Severity = "Warning",
                    Rule = "code-too-long",
                    Message = $"Only the first {MaxDocumentLength} characters were reviewed; the code passed in is " +
                              $"{text.Length} characters long, so anything after that was not checked.",
                    Fix = "Review the component or the fragment in question rather than a whole file, and call this " +
                          "tool once per piece."
                },
                .. review.Findings
            ]
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionAnimatableProperties))]
    [Description("Lists the properties Bit.Bmotion can animate, with the CSS each one writes, an example value, and - measured by running it through the real engine - whether the browser compositor can own it and therefore whether it animates or jumps on Blazor Server. Use it when choosing what to animate, or to find the compositor-friendly equivalent of a property that is not. Narrow it with 'filter' rather than reading all 48: most questions are about one category, one name, or only about what survives Blazor Server.")]
    public async Task<BmotionPropertyDto[]> GetBmotionAnimatableProperties(
        [Description("Optional. A category ('Transform', 'Visual', 'Layout', 'Typography', 'SVG', 'Motion path', 'Custom'), 'compositor' for only the properties the compositor can own, 'frame-loop' for only the ones that jump on Blazor Server, or any substring of a property name. Omitted, every property is returned.")] string? filter = null)
    {
        var properties = await BmotionPropertyCatalog.GetAsync();

        if (string.IsNullOrWhiteSpace(filter)) return properties;

        var term = filter.Trim();

        var matches = term switch
        {
            _ when term.Equals("compositor", StringComparison.OrdinalIgnoreCase)
                => properties.Where(property => property.CompositorEligible),
            _ when term.Equals("frame-loop", StringComparison.OrdinalIgnoreCase)
                 || term.Equals("frameloop", StringComparison.OrdinalIgnoreCase)
                => properties.Where(property => property.CompositorEligible is false),
            _ => properties.Where(property => property.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
                                           || property.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
        };

        // A filter that matched nothing must not read as "Bmotion cannot animate that". Falling back
        // to the whole list would hide the miss; the caller is told what the filter does instead.
        return [.. matches];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionEasings))]
    [Description("Lists BmEase presets with each curve sampled from the library's own easing implementation - eleven points, a text sparkline, and whether the curve leaves the 0-1 range and so makes the element overshoot. Call it when choosing an easing: the names alone do not say how BackOut differs from ExpoOut, or which presets are unusable on an element with a hard edge. Narrow it with 'filter' rather than reading all 32.")]
    public async Task<BmotionEasingDto[]> GetBmotionEasings(
        [Description("Optional. A family ('Back', 'Expo', 'Elastic', 'Bounce', 'Circ', 'Sine', 'Quad', 'Quart', 'Quint', 'Cubic', 'Linear', 'Anticipate'), a direction ('In', 'Out', 'InOut'), 'overshoots' for only the presets that travel past the target, or any substring of a preset name. Omitted, every preset is returned.")] string? filter = null)
    {
        var easings = await BmotionEasingCatalog.GetAsync();

        if (string.IsNullOrWhiteSpace(filter)) return easings;

        var term = filter.Trim();

        var matches = term.Equals("overshoots", StringComparison.OrdinalIgnoreCase)
            ? easings.Where(easing => easing.Overshoots)
            // Direction is matched exactly: "In" is a direction shared by a third of the presets,
            // and as a substring it would also pull in every "InOut" and every "Linear".
            : easings.Where(easing => easing.Direction.Equals(term, StringComparison.OrdinalIgnoreCase)
                                   || easing.Family.Equals(term, StringComparison.OrdinalIgnoreCase)
                                   || easing.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

        return [.. matches];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionGuideSections))]
    [Description("Lists every section of the Bit.Bmotion guide (the library README), with its heading and size. Use it to pick the heading to pass to GetBmotionGuideSection.")]
    public BmotionGuideSectionDto[] GetBmotionGuideSections()
    {
        return BmotionSourceCatalog.GuideSections;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionGuideSection))]
    [Description("Gets one section of the Bit.Bmotion guide as Markdown, with its code samples - e.g. 'Variants', 'Drag', 'Layout & shared elements', 'Scroll timelines', 'Motion values', 'Accessibility'. Sub-sections are included. Heading matching ignores case and punctuation.")]
    public string GetBmotionGuideSection(string heading)
    {
        var section = BmotionSourceCatalog.GetGuideSection(heading);

        if (section is null)
        {
            var headings = string.Join(", ", BmotionSourceCatalog.GuideSections.Select(entry => $"'{entry.Heading}'"));

            return $"The guide has no section called '{heading}'. Available sections: {headings}.";
        }

        return Truncate(section);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionApiList))]
    [Description("Lists every public type of the Bit.Bmotion library - components, services, transitions, targets, options and enums - with its kind and a one-line summary. Use it to pick the type to pass to GetBmotionApiDetails, which returns that type's full documentation and every member.")]
    public BmotionApiTypeDto[] GetBmotionApiList()
    {
        return BmotionApiCatalog.TypeSummaries;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionApiDetails))]
    [Description("Gets the full reference of one Bit.Bmotion type: its Blazor parameters with types and default values, its properties, methods, events or enum values, each with its documentation - read straight out of the shipped assembly. Call it before using a member you are unsure about, e.g. 'Bmotion', 'Bm', 'BmSpring', 'BmTween', 'BmotionAnimatePresence', 'BmVariants', 'BmDrag', 'BmScrollTimeline', 'BmotionAnimateService'. In an animation library the default values are the behaviour, so guessing at them produces motion that is subtly wrong rather than code that fails.")]
    public BmotionApiDetailsResultDto GetBmotionApiDetails(string typeName)
    {
        var details = BmotionApiCatalog.GetTypeDetails(typeName);

        if (details is not null) return new BmotionApiDetailsResultDto { Details = details };

        var candidates = BmotionApiCatalog.Types
            .Where(type => type.Name.Contains(typeName ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Select(type => type.Name)
            .ToArray();

        return new BmotionApiDetailsResultDto
        {
            Message = candidates.Length > 0
                ? $"Bit.Bmotion has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"Bit.Bmotion has no public type called '{typeName}'. Call GetBmotionApiList for the full list."
        };
    }

    // The demo pages used to be a tool of their own. Every one of them was already in the source
    // file listing under the same path, so the two answers overlapped by twenty entries and differed
    // only in that one of them knew what the page was about. That knowledge moved here instead.
    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionSourceFiles))]
    [Description("Lists the working Bit.Bmotion source files this server can hand out: every page of the demo site - one per feature area, each a complete working example of the feature it covers - plus the layout, the shared components and the host wiring. Demo pages carry the route they are served at and the terms they cover. Use it to pick the path to pass to GetBmotionSourceFile.")]
    public BmotionSourceFileDto[] GetBmotionSourceFiles(
        [Description("Optional. 'Demo page' for only the feature pages, 'Host' for the server wiring, 'Demo' for the layout and shared components, or any substring of a path, a title or a page's keywords. Omitted, every file is returned.")] string? filter = null)
    {
        var files = BmotionSourceCatalog.SourceFiles;

        if (string.IsNullOrWhiteSpace(filter)) return files;

        var term = filter.Trim();

        return [.. files.Where(file => file.Kind.Equals(term, StringComparison.OrdinalIgnoreCase)
                                    || file.Path.Contains(term, StringComparison.OrdinalIgnoreCase)
                                    || (file.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                                    || (file.Keywords?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionSourceFile))]
    [Description("Gets one source file listed by GetBmotionSourceFiles, verbatim - e.g. 'Demo/Client/Pages/Springs.razor' for a complete, working page that exercises every form of spring. The longest demo pages run past what one answer carries; pass fromLine and toLine to read such a file a part at a time, using the line count GetBmotionSourceFiles reports for it.")]
    public string GetBmotionSourceFile(
        [Description("The path from GetBmotionSourceFiles, e.g. 'Demo/Client/Pages/Springs.razor'.")] string path,
        [Description("Optional 1-based first line to return. Omitted, the file is read from its start.")] int? fromLine = null,
        [Description("Optional 1-based last line to return, inclusive. Omitted, the file is read to its end.")] int? toLine = null)
    {
        var content = BmotionSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BmotionSourceCatalog.SourceFiles
                .Where(file => file.Path.Contains(path ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Select(file => file.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBmotionSourceFiles for the full list.";
        }

        if (fromLine is null && toLine is null) return Truncate(content, path!);

        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        // Both ends are clamped rather than rejected. An agent asking for lines 400-600 of a 380-line
        // file has made an off-by-something, not a request worth refusing; the header below says what
        // it actually got, which is what it needs to correct itself.
        var first = Math.Clamp(fromLine ?? 1, 1, lines.Length);
        var last = Math.Clamp(toLine ?? lines.Length, first, lines.Length);

        // The slice is bounded before the header goes on, so the line the truncation notice names is
        // a line of the file rather than a line of this answer.
        var slice = Truncate(string.Join('\n', lines[(first - 1)..last]), path!, first);

        return $"// {path}, lines {first}-{last} of {lines.Length}\n\n{slice}";
    }

    /// <summary>
    /// Everything this server exposes over MCP - its tools, prompts and resources - read off the
    /// attributes that declare them.
    /// <para>
    /// Deliberately not an MCP tool: an agent already gets this from the protocol's own
    /// <c>tools/list</c>. It exists for the /mcp demo page, which documents the server to a human
    /// reader. Reflecting over the registrations rather than restating them means the page cannot
    /// end up describing a tool that no longer exists, or missing one that was just added.
    /// </para>
    /// </summary>
    [HttpGet]
    public BmotionMcpCatalogDto GetMcpCatalog()
    {
        return new BmotionMcpCatalogDto
        {
            Tools = [.. typeof(McpController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method,
                                   Tool: method.GetCustomAttribute<McpServerToolAttribute>(),
                                   Description: method.GetCustomAttribute<DescriptionAttribute>()))
                .Where(entry => entry.Tool is not null)
                .Select(entry => new BmotionMcpMemberDto
                {
                    Name = entry.Tool!.Name ?? entry.Method.Name,
                    Description = entry.Description?.Description ?? string.Empty,
                    Parameters = [.. entry.Method.GetParameters().Select(FormatParameter)]
                })
                .OrderBy(tool => tool.Name, StringComparer.Ordinal)],

            Prompts = [.. typeof(McpPrompts)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method,
                                   Prompt: method.GetCustomAttribute<McpServerPromptAttribute>(),
                                   Description: method.GetCustomAttribute<DescriptionAttribute>()))
                .Where(entry => entry.Prompt is not null)
                .Select(entry => new BmotionMcpMemberDto
                {
                    Name = entry.Prompt!.Name ?? entry.Method.Name,
                    Description = entry.Description?.Description ?? string.Empty,
                    Parameters = [.. entry.Method.GetParameters().Select(FormatParameter)]
                })
                .OrderBy(prompt => prompt.Name, StringComparer.Ordinal)],

            Resources = [.. typeof(McpResources)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => (Resource: method.GetCustomAttribute<McpServerResourceAttribute>(),
                                   Description: method.GetCustomAttribute<DescriptionAttribute>()))
                .Where(entry => entry.Resource is not null)
                .Select(entry => new BmotionMcpMemberDto
                {
                    Name = entry.Resource!.UriTemplate ?? entry.Resource.Name ?? string.Empty,
                    Description = entry.Description?.Description ?? string.Empty,
                    Parameters = []
                })
                .OrderBy(resource => resource.Name, StringComparer.Ordinal)]
        };
    }

    /// <summary>A tool or prompt parameter, as "name: type" with "?" for the optional ones.</summary>
    private static string FormatParameter(ParameterInfo parameter)
    {
        var type = BmotionApiCatalog.FriendlyName(parameter.ParameterType);

        return parameter.HasDefaultValue ? $"{parameter.Name}?: {type}" : $"{parameter.Name}: {type}";
    }

    /// <summary>
    /// A renamed guide heading must not silently leave a blank gap in the overview - the agent is
    /// told where the text went instead.
    /// </summary>
    private static void AppendGuideSection(StringBuilder builder, string heading)
    {
        builder.AppendLine(BmotionSourceCatalog.GetGuideSection(heading)
                           ?? $"_The guide's \"{heading}\" section was not found in this build. " +
                              $"Call GetBmotionGuideSections for the sections it does have._")
               .AppendLine();
    }

    /// <summary>
    /// Bounds one document. A cut that says only that it happened leaves the caller to guess whether
    /// the rest matters and how to reach it, so when the text came from a named source file the
    /// notice carries the call that reads on from where this answer stopped.
    /// </summary>
    internal static string Truncate(string text, string? sourcePath = null, int firstLine = 1)
    {
        if (text.Length <= MaxDocumentLength) return text;

        var kept = text[..MaxDocumentLength];

        // Where the reader got to, in the units the follow-up call is written in.
        var line = firstLine + kept.Count(character => character == '\n');

        var howToContinue = sourcePath is null
            ? "Call GetBmotionGuideSection for one section at a time."
            : $"Call GetBmotionSourceFile(path: \"{sourcePath}\", fromLine: {line}) to read on from there.";

        return $"{kept}\n\n[truncated at line {line} - the full text is longer than " +
               $"{MaxDocumentLength} characters. {howToContinue}]";
    }
}
