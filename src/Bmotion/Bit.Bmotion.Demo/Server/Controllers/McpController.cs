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
    // crowd out a client's context window.
    private const int MaxDocumentLength = 40_000;

    private static readonly string BmotionVersion =
        typeof(Bm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Bm).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionOverview))]
    [Description("Start here. Explains what Bit.Bmotion is, how to install and register it, the one thing that decides whether an animation works on Blazor Server, and which of the other Bmotion tools to call for what.")]
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

        builder.AppendLine("""
            ---

            ## Which tool to call

            - `SearchBmotion` - **the default entry point.** One query across the guide, every public type and
              member, the animatable properties, the easing presets, the recipes and the demo's sources; each hit
              carries the exact follow-up call. Reach for it whenever you do not already know the name of what you
              want.
            - `GetBmotionRecipes` / `GetBmotionRecipe` - complete, copy-pasteable patterns for the things people
              actually ask for: a staggered list, a modal, an exit animation, a scroll reveal, a shared-element
              transition. **Start here when writing an animation** - each recipe carries the caveat that is not
              visible in the code.
            - `SimulateBmotionTransition` - runs a transition on the real engine and reports how long it takes to
              settle, how far it overshoots and what the curve looks like. A spring has no duration argument, so
              this is the only way to know what one does before shipping it. `CompareBmotionTransitions` does
              several at once, which is how to choose between them.
            - `AnalyzeBmotionAnimation` - **call this before finishing any animation for an app that is not
              WebAssembly-only.** It starts the animation on the real engine and reports which playback path the
              engine chose, which is exactly what decides whether the animation plays or silently snaps on Blazor
              Server.
            - `ReviewBmotionCode` - checks written markup for the Bmotion mistakes that compile cleanly and then do
              nothing: an `Exit` with no presence component, a `@foreach` with no `@key`, a spring whose duration
              is ignored. Run it on what you wrote before you call the work done.
            - `GetBmotionSetupGuide` - the complete wiring for one Blazor render mode ('wasm', 'server', 'auto',
              'standalone-wasm'), as the real files of a working project.
            - `GetBmotionApiList` / `GetBmotionApiDetails` - the exact public API: every component parameter with
              its type and default value, straight out of the shipped assembly. Call this before using a member you
              are not sure about - in an animation library the defaults *are* the behaviour.
            - `GetBmotionAnimatableProperties` - every property that can be animated, and for each one whether the
              browser compositor can own it.
            - `GetBmotionEasings` - every `BmEase` preset with its curve sampled from the library, so a name like
              "BackOut" becomes numbers instead of a guess.
            - `GetBmotionGuideSections` / `GetBmotionGuideSection` - the library's own reference guide, one topic
              at a time, with copy-pasteable code.
            - `GetBmotionDemoPages` / `GetBmotionSourceFiles` / `GetBmotionSourceFile` - the demo site's pages as
              real, working source. Every feature has one.

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
            - `Bm.Spring(duration: ...)` does nothing on its own. Duration only shapes the spring when `bounce` is
              given too.
            - Register the services in every DI container the components run in - in a Blazor Web App that means
              both the server and the client project - and add `@using Bit.Bmotion` to `_Imports.razor`.
            """);

        return builder.ToString();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBmotion))]
    [Description("Searches everything known about Bit.Bmotion at once - the guide, every public type and member, the animatable properties, the easing presets, the ready-made recipes and the demo's source files - and returns the best matches, each with the exact follow-up tool call that returns its full text. Use this first whenever you do not already know which section, type or recipe holds the answer. Example queries: 'make a list appear one item at a time', 'animate something out before it is removed', 'drag within bounds', 'why does my animation not run on the server'.")]
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
    [Description("Lists the ready-made Bit.Bmotion patterns this server can hand out complete - a staggered list, an exit animation, a modal, hover feedback, a scroll reveal, a layout animation, a shared-element transition, drag, a spinner, split text, programmatic animation, reduced motion. Use it to pick the id to pass to GetBmotionRecipe. This is usually the fastest route from a request to correct code.")]
    public BmotionRecipeDto[] GetBmotionRecipes()
    {
        return BmotionRecipeCatalog.Summaries;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionRecipe))]
    [Description("Gets one Bit.Bmotion recipe in full: the Razor markup, and the caveat that is not visible in the code - the missing @key, the presence component it needs, the render mode it will not survive. Pass an id from GetBmotionRecipes, e.g. 'staggered-list', 'exit-animation', 'modal-dialog', 'reveal-on-scroll', 'shared-element'.")]
    public object GetBmotionRecipe(string id)
    {
        var recipe = BmotionRecipeCatalog.Find(id);

        if (recipe is not null) return recipe;

        var ids = string.Join(", ", BmotionRecipeCatalog.All.Select(entry => entry.Id));

        return $"There is no recipe called '{id}'. Available recipes: {ids}. " +
               "Call SearchBmotion with what you are trying to build if none of them fit.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SimulateBmotionTransition))]
    [Description("Runs a transition on the real Bit.Bmotion engine, off-screen, and reports what the motion actually does: how long it takes to come to rest, how far it overshoots its target, how many times it wobbles, and the shape of the curve. Springs have no duration argument - their length falls out of the physics - so this is the only way to know what one feels like without opening a browser. Accepts 'spring(stiffness: 260, damping: 12)', 'spring(bounce: 0.4, duration: 0.6)', 'tween(0.4, InOut)' or 'inertia(velocity: 500)'.")]
    public Task<BmotionSimulationDto> SimulateBmotionTransition(
        [Description("The transition, e.g. 'spring(stiffness: 260, damping: 12)'. A Bm.Spring(...) call copied out of Razor works verbatim.")] string transition,
        [Description("The value the animation starts at. The default of 0 to 100 reads as a percentage of the distance travelled.")] double from = 0,
        [Description("The value the animation targets.")] double to = 100)
    {
        return BmotionMotionLab.SimulateAsync(transition, from, to);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(CompareBmotionTransitions))]
    [Description("Runs several transitions through the real engine and returns their measurements side by side, so the one that matches the intended feel can be chosen on evidence rather than on the names of the arguments. Pass the transitions separated by semicolons or newlines, e.g. 'spring(stiffness: 260, damping: 12); spring(bounce: 0.2, duration: 0.4); tween(0.3, BackOut)'.")]
    public async Task<BmotionSimulationDto[]> CompareBmotionTransitions(
        [Description("The transitions to compare, separated by semicolons or newlines.")] string transitions,
        double from = 0,
        double to = 100)
    {
        var specs = (transitions ?? string.Empty)
            .Split([';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // Simulating dozens at once would spend more of the client's context than any comparison
            // is read with; the interesting comparisons are between two and four candidates.
            .Take(8)
            .ToArray();

        var results = new List<BmotionSimulationDto>(specs.Length);

        foreach (var spec in specs)
        {
            results.Add(await BmotionMotionLab.SimulateAsync(spec, from, to));
        }

        return [.. results];
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
    [Description("Reviews Razor or C# that uses Bit.Bmotion and reports the mistakes that compile cleanly and then do nothing: an Exit with no presence component around it, an animated element in a loop with no @key, a spring whose duration the engine ignores, a nested-quote attribute that does not parse as intended, properties that will snap rather than animate on Blazor Server. Every finding names the line and the correction. Run this on animation code before calling it done - none of these produce a compiler warning, an exception or a console message.")]
    public BmotionReviewDto ReviewBmotionCode(
        [Description("The Razor markup or C# to review. A component, a fragment, or just the <Bmotion> element in question.")] string code)
    {
        return BmotionCodeReview.Review(code);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionAnimatableProperties))]
    [Description("Lists every property Bit.Bmotion can animate, with the CSS it writes, an example value, and - measured by running each one through the real engine - whether the browser compositor can own it and therefore whether it animates or jumps on Blazor Server. Use it when choosing what to animate, or to find the compositor-friendly equivalent of a property that is not.")]
    public Task<BmotionPropertyDto[]> GetBmotionAnimatableProperties()
    {
        return BmotionPropertyCatalog.GetAsync();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionEasings))]
    [Description("Lists every BmEase preset with its curve sampled from the library's own easing implementation - eleven points, a text sparkline, and whether the curve leaves the 0-1 range and so makes the element overshoot. Call it when choosing an easing: the names alone do not say how BackOut differs from ExpoOut, or which presets are unusable on an element with a hard edge.")]
    public Task<BmotionEasingDto[]> GetBmotionEasings()
    {
        return BmotionEasingCatalog.GetAsync();
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
    [Description("Lists every public type of the Bit.Bmotion library - components, services, transitions, targets, options and enums - with its kind and summary. Use it to pick the type to pass to GetBmotionApiDetails.")]
    public BmotionApiTypeDto[] GetBmotionApiList()
    {
        return BmotionApiCatalog.Types;
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

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionDemoPages))]
    [Description("Lists the pages of the Bit.Bmotion demo site - one per feature area - with what each demonstrates and the source file behind it. Every page is a working example of the feature it covers, so this is the way to find real code for a feature rather than a fragment.")]
    public BmotionDemoPageDto[] GetBmotionDemoPages()
    {
        return [.. NavItem.All.Select(page => new BmotionDemoPageDto
        {
            Slug = page.Href,
            Title = page.Title,
            Description = page.Description,
            Keywords = page.Keywords,
            SourcePath = page.SourcePath
        })];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionSourceFiles))]
    [Description("Lists the working Bit.Bmotion source files this server can hand out: every page of the demo site, its layout and shared components, and the host wiring. Use it to pick the path to pass to GetBmotionSourceFile.")]
    public BmotionSourceFileDto[] GetBmotionSourceFiles()
    {
        return BmotionSourceCatalog.SourceFiles;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmotionSourceFile))]
    [Description("Gets one source file listed by GetBmotionSourceFiles or GetBmotionDemoPages, verbatim - e.g. 'Demo/Client/Pages/Springs.razor' for a complete, working page that exercises every form of spring.")]
    public string GetBmotionSourceFile(string path)
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

        return Truncate(content);
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

    private static string Truncate(string text)
    {
        return text.Length <= MaxDocumentLength
            ? text
            : $"{text[..MaxDocumentLength]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
