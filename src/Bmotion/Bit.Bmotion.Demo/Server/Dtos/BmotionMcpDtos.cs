namespace Bit.Bmotion.Demo.Server.Dtos;

// The shapes every Bmotion MCP tool answers with. They are records with documented members rather
// than loose dictionaries because the MCP client turns them into the tool's output schema: an agent
// that can see "SettleSeconds" and "OvershootPercent" as named, described fields does not have to
// infer what a number means, and cannot mistake one for another.

/// <summary>One section of the Bit.Bmotion guide (the library README).</summary>
public record BmotionGuideSectionDto
{
    /// <summary>The heading text, e.g. "Drag". Pass it to GetBmotionGuideSection.</summary>
    public required string Heading { get; init; }

    /// <summary>Markdown heading level: 2 for a top-level section, 3 for a sub-section.</summary>
    public required int Level { get; init; }

    /// <summary>The owning level-2 section, or null when this entry is itself level 2.</summary>
    public string? Parent { get; init; }

    /// <summary>Number of markdown lines in the section, including its sub-sections.</summary>
    public required int Lines { get; init; }
}

/// <summary>A public type of the Bit.Bmotion assembly.</summary>
public record BmotionApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Component, Interface, Enum, Attribute, Delegate, Static class, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    public string? Summary { get; init; }
}

/// <summary>A member (parameter, property, method, event or enum value) of a public Bit.Bmotion type.</summary>
public record BmotionApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Parameter, Property, Field, EnumValue, Method or Event. "Parameter" means a Blazor [Parameter].</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or a method's return type.</summary>
    public string? Type { get; init; }

    /// <summary>A method's parameter list, e.g. "(double delay)".</summary>
    public string? Signature { get; init; }

    /// <summary>The value the member holds on a freshly created instance, when it could be determined.</summary>
    public string? Default { get; init; }

    /// <summary>True for [EditorRequired] Blazor parameters.</summary>
    public bool Required { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}

/// <summary>The full reference of one public Bit.Bmotion type.</summary>
public record BmotionApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    public string? BaseType { get; init; }

    public required string[] Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    public required BmotionApiMemberDto[] Members { get; init; }
}

/// <summary>The answer of GetBmotionApiDetails: the type, or a message naming the near misses.</summary>
public record BmotionApiDetailsResultDto
{
    public BmotionApiTypeDetailsDto? Details { get; init; }

    /// <summary>Set instead of Details when no public type goes by the requested name.</summary>
    public string? Message { get; init; }
}

/// <summary>One property that Bit.Bmotion can animate, and what animating it costs.</summary>
public record BmotionPropertyDto
{
    /// <summary>The <c>Bm.To(...)</c> argument name, e.g. "backgroundColor".</summary>
    public required string Name { get; init; }

    /// <summary>Transform, Visual, Layout, Typography, SVG, Motion path or Custom.</summary>
    public required string Category { get; init; }

    /// <summary>The CSS property (or transform component) the engine writes.</summary>
    public required string Css { get; init; }

    /// <summary>The C# type of the argument: BmKeyframes (numeric) or BmStringKeyframes (CSS value).</summary>
    public required string ValueType { get; init; }

    /// <summary>
    /// True when the browser compositor can own this property, so the animation plays through the
    /// Web Animations API with no per-frame interop - and therefore plays on Blazor Server too.
    /// </summary>
    public required bool CompositorEligible { get; init; }

    /// <summary>
    /// What happens on Blazor Server: "Animates" for a compositor-eligible property, or
    /// "Jumps to the target" for one that needs the per-frame loop.
    /// </summary>
    public required string OnBlazorServer { get; init; }

    /// <summary>How to write a value for it, e.g. "x: 100", "backgroundColor: \"#FD7F36\"".</summary>
    public required string Example { get; init; }

    public string? Notes { get; init; }
}

/// <summary>One <see cref="BmEase"/> preset, sampled from the library's own easing function.</summary>
public record BmotionEasingDto
{
    /// <summary>The enum member name, e.g. "InOut". Write it as <c>BmEase.InOut</c>.</summary>
    public required string Name { get; init; }

    /// <summary>In, Out, InOut or Linear - which end of the motion the easing slows.</summary>
    public required string Direction { get; init; }

    /// <summary>The family: Cubic bezier, Circ, Back, Sine, Quad, Quart, Quint, Expo, Elastic, Bounce.</summary>
    public required string Family { get; init; }

    /// <summary>What it feels like, and when to reach for it.</summary>
    public required string Feel { get; init; }

    /// <summary>Eleven samples of the curve at t = 0, 0.1 ... 1, straight from BmEaseFunctions.</summary>
    public required double[] Curve { get; init; }

    /// <summary>The same curve drawn as text, so it can be read in a terminal.</summary>
    public required string Sparkline { get; init; }

    /// <summary>True when the curve leaves the 0-1 range, which makes the element overshoot.</summary>
    public required bool Overshoots { get; init; }
}

/// <summary>The result of running a transition through the real engine, off-screen.</summary>
public record BmotionSimulationDto
{
    /// <summary>The transition as it was understood, written as the C# call that produces it.</summary>
    public required string Transition { get; init; }

    /// <summary>Spring, Tween or Inertia.</summary>
    public required string Kind { get; init; }

    public required double From { get; init; }

    public required double To { get; init; }

    /// <summary>
    /// How long the motion actually takes, measured rather than configured: for a spring this is
    /// where the physics comes to rest, which no duration argument states.
    /// </summary>
    public required double SettleSeconds { get; init; }

    /// <summary>
    /// How far past the target the value travels, as a percentage of the distance covered. 0 for a
    /// motion that never crosses its target; around 20 for a lively spring.
    /// </summary>
    public required double OvershootPercent { get; init; }

    /// <summary>How many times the value crosses the target before resting - the wobble count.</summary>
    public required int TargetCrossings { get; init; }

    /// <summary>The greatest speed reached, in units per second.</summary>
    public required double PeakVelocity { get; init; }

    /// <summary>Seconds from the start to the moment 90% of the distance is covered.</summary>
    public required double TimeTo90Percent { get; init; }

    /// <summary>Evenly spaced samples of the motion: (seconds, value) pairs.</summary>
    public required BmotionSampleDto[] Samples { get; init; }

    /// <summary>The motion drawn as text, so the shape can be read without plotting it.</summary>
    public required string Sparkline { get; init; }

    /// <summary>What the numbers mean, in a sentence - including whether the feel matches the intent.</summary>
    public required string Reading { get; init; }

    /// <summary>Anything the spec said that was not applied, and assumptions that were made.</summary>
    public required string[] Warnings { get; init; }

    /// <summary>
    /// Set instead of a measurement when the transition could not be read, and says how to write it.
    /// An unreadable spec is a correctable mistake, so it comes back as data the caller can act on
    /// rather than as a failed call whose reason the protocol reduces to "an error occurred".
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>One sampled instant of a simulated motion.</summary>
public record BmotionSampleDto
{
    public required double Seconds { get; init; }

    public required double Value { get; init; }
}

/// <summary>How an animation will actually be played, as decided by the engine itself.</summary>
public record BmotionPlaybackDto
{
    /// <summary>The properties that were analysed.</summary>
    public required string[] Properties { get; init; }

    /// <summary>The transition, written as the C# call that produces it.</summary>
    public required string Transition { get; init; }

    /// <summary>"Compositor (Web Animations API)" or "C# frame loop (requestAnimationFrame)".</summary>
    public required string Path { get; init; }

    /// <summary>
    /// True when the animation plays on Blazor Server as it does on WebAssembly. False means it
    /// collapses to an instant state change there, because it needs the per-frame loop.
    /// </summary>
    public required bool WorksOnBlazorServer { get; init; }

    /// <summary>Why the engine chose that path, in the engine's own terms.</summary>
    public required string Reason { get; init; }

    /// <summary>The duration the compositor was given, in milliseconds, when the animation was offloaded.</summary>
    public double? CompositorDurationMs { get; init; }

    /// <summary>The CSS easing handed to the browser, when the animation was offloaded.</summary>
    public string? CompositorEasing { get; init; }

    /// <summary>What to change to make it play on Blazor Server, when it does not.</summary>
    public string[]? HowToOffload { get; init; }

    /// <summary>
    /// Set instead of a verdict when the transition could not be read, and says how to write it.
    /// When this is present the rest of the fields state nothing: in particular WorksOnBlazorServer
    /// is false because nothing was analysed, not because the animation would fail there.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>One finding from reviewing Bmotion markup.</summary>
public record BmotionReviewFindingDto
{
    /// <summary>Error, Warning or Suggestion. An Error is markup the library will not honour.</summary>
    public required string Severity { get; init; }

    /// <summary>The rule that fired, e.g. "animate-without-initial".</summary>
    public required string Rule { get; init; }

    /// <summary>The 1-based line of the reviewed code the finding sits on, when it maps to one.</summary>
    public int? Line { get; init; }

    /// <summary>What is wrong.</summary>
    public required string Message { get; init; }

    /// <summary>What to do about it.</summary>
    public required string Fix { get; init; }
}

/// <summary>The result of reviewing a piece of Bmotion markup.</summary>
public record BmotionReviewDto
{
    /// <summary>True when nothing above a suggestion was found.</summary>
    public required bool Passed { get; init; }

    public required BmotionReviewFindingDto[] Findings { get; init; }

    /// <summary>What was checked, so an empty result is not mistaken for an unchecked one.</summary>
    public required string[] RulesApplied { get; init; }
}

/// <summary>A ready-made, copy-pasteable Bmotion pattern.</summary>
public record BmotionRecipeDto
{
    /// <summary>The id to pass to GetBmotionRecipe, e.g. "fade-in-on-scroll".</summary>
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>What it is for, phrased the way the request usually arrives.</summary>
    public required string Intent { get; init; }

    /// <summary>Search terms the recipe covers.</summary>
    public required string Keywords { get; init; }

    /// <summary>The Razor markup. Present on GetBmotionRecipe, omitted from the listing.</summary>
    public string? Code { get; init; }

    /// <summary>
    /// What to know before using it - the caveat that is not visible in the code. Present on
    /// GetBmotionRecipe, omitted from the listing.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>The demo page that shows it running, e.g. "/scroll".</summary>
    public string? SeeAlso { get; init; }
}

/// <summary>The answer of GetBmotionRecipe: the recipe, or a message naming the ones that exist.</summary>
public record BmotionRecipeResultDto
{
    public BmotionRecipeDto? Recipe { get; init; }

    /// <summary>Set instead of Recipe when no recipe goes by the requested id.</summary>
    public string? Message { get; init; }
}

/// <summary>One source file the MCP server can hand out verbatim.</summary>
public record BmotionSourceFileDto
{
    /// <summary>The path to pass to GetBmotionSourceFile.</summary>
    public required string Path { get; init; }

    /// <summary>"Demo page", "Demo" or "Host".</summary>
    public required string Kind { get; init; }

    /// <summary>What the file is. For a demo page, what the page demonstrates.</summary>
    public string? Description { get; init; }

    public required int Lines { get; init; }

    /// <summary>The demo page's title, e.g. "Springs". Null for a file that is not a demo page.</summary>
    public string? Title { get; init; }

    /// <summary>
    /// The route the demo page is served at, e.g. "springs" - empty for the landing page, null for
    /// a file that is not a demo page.
    /// </summary>
    public string? Slug { get; init; }

    /// <summary>Space-separated search terms the demo page covers. Null for anything else.</summary>
    public string? Keywords { get; init; }
}

/// <summary>Everything this server exposes over MCP, for the demo page that documents it.</summary>
public record BmotionMcpCatalogDto
{
    public required BmotionMcpMemberDto[] Tools { get; init; }

    public required BmotionMcpMemberDto[] Prompts { get; init; }

    public required BmotionMcpMemberDto[] Resources { get; init; }
}

/// <summary>One tool, prompt or resource, read off the attribute that declares it.</summary>
public record BmotionMcpMemberDto
{
    /// <summary>The tool or prompt name, or the resource's URI template.</summary>
    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>The parameters, as "name: type" with a "?" on the optional ones.</summary>
    public required string[] Parameters { get; init; }
}

/// <summary>One hit from the unified search, carrying the call that returns its full text.</summary>
public record BmotionSearchHitDto
{
    /// <summary>What kind of thing was found: "Guide section", "API component", "Recipe", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where it sits - the parent section, the owning type, the category.</summary>
    public string? Context { get; init; }

    /// <summary>The exact tool call that returns this hit in full.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}
