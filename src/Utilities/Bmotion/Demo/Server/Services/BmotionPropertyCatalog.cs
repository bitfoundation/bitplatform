using System.Reflection;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Bit.Bmotion.Demo.Server.Dtos;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Every property Bit.Bmotion can animate, and the one thing about each that an agent cannot read
/// off the API: how the engine plays it, and what that costs.
/// <para>
/// The list itself comes from reflecting over <see cref="BmProps"/>, so a property added to the
/// library appears here without anyone remembering to add it. The expensive claim - whether the
/// browser plays the animation, as a two-keyframe timeline or as a recording, and therefore whether
/// it survives on Blazor Server - is <b>measured</b>: each property is animated once through the real
/// engine at first use, as Blazor Server, and the catalog records what the engine handed the
/// browser. Writing that table by hand would mean maintaining a second copy of a rule set that lives
/// in the engine, and being wrong about it in exactly the cases that matter.
/// </para>
/// </summary>
public static partial class BmotionPropertyCatalog
{
    /// <summary>Descriptive metadata for a property: everything except the measured verdict.</summary>
    private sealed record Facts(string Category, string Css, string Example, string? Notes = null);

    private static readonly Lazy<Task<BmotionPropertyDto[]>> _properties = new(ProbeAsync);

    /// <summary>
    /// The longest stretch the engine records for the browser to play; an animation only a recording
    /// reproduces that runs longer stays on the frame loop. The engine's own limit, restated because it
    /// is internal to the library and the explanations have to name it.
    /// </summary>
    private const double RecordingLimitSeconds = 10;

    private static readonly Lazy<FrozenDictionary<string, PropertyInfo>> _bmPropsByName = new(() =>
        typeof(BmProps).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .Where(property => property.CanWrite && property.Name != nameof(BmProps.Transition))
                       .ToFrozenDictionary(property => CamelCase(property.Name), StringComparer.OrdinalIgnoreCase));

    // What each property is for, in the terms someone writing an animation thinks in. Only the
    // prose is here; the names come from BmProps and the playback verdict from the engine.
    private static readonly FrozenDictionary<string, Facts> _facts = new Dictionary<string, Facts>(StringComparer.OrdinalIgnoreCase)
    {
        ["x"] = new("Transform", "transform: translateX()", "x: 100", "Pixels by default. The cheapest thing to animate, along with y and opacity."),
        ["y"] = new("Transform", "transform: translateY()", "y: -20"),
        ["z"] = new("Transform", "transform: translateZ()", "z: 50", "Needs a perspective on this element or an ancestor to be visible."),
        ["scale"] = new("Transform", "transform: scale()", "scale: 1.2", "Scales text and borders with the element; animate width/height instead when that is unwanted."),
        ["scaleX"] = new("Transform", "transform: scaleX()", "scaleX: 0"),
        ["scaleY"] = new("Transform", "transform: scaleY()", "scaleY: 0"),
        ["rotate"] = new("Transform", "transform: rotate()", "rotate: 180", "Degrees. Values beyond 360 keep turning rather than wrapping, so rotate: 720 is two full turns."),
        ["rotateX"] = new("Transform", "transform: rotateX()", "rotateX: 45", "A 3D turn: pair it with perspective or it looks like a squash."),
        ["rotateY"] = new("Transform", "transform: rotateY()", "rotateY: 180", "The card-flip axis."),
        ["rotateZ"] = new("Transform", "transform: rotateZ()", "rotateZ: 90", "The same axis as rotate; use one or the other on an element."),
        ["skewX"] = new("Transform", "transform: skewX()", "skewX: 12"),
        ["skewY"] = new("Transform", "transform: skewY()", "skewY: 12"),
        ["perspective"] = new("Transform", "transform: perspective()", "perspective: 800", "Set it on the element being turned, or as a CSS perspective on its parent."),
        ["originX"] = new("Transform", "transform-origin", "originX: 0", "0-1 across the element: what every transform pivots around. Give it in Initial as well when it should animate - the origin the page reports is in pixels, which does not interpolate to a fraction, so without a starting value the change jumps."),
        ["originY"] = new("Transform", "transform-origin", "originY: 1", "0-1 down the element. Set both to move the pivot, e.g. originY: 1 to grow upward."),

        ["opacity"] = new("Visual", "opacity", "opacity: 0", "The other compositor-cheap property. Prefer it to visibility or display, which cannot be animated at all."),
        ["backgroundColor"] = new("Visual", "background-color", "backgroundColor: \"#FD7F36\"", "Mixed in sRGB, or in OKLab with BmColorSpace.Oklab. It repaints on every frame, where an opacity cross-fade of two stacked elements does not."),
        ["color"] = new("Visual", "color", "color: \"#1276C6\""),
        ["borderColor"] = new("Visual", "border-color", "borderColor: \"tomato\""),
        ["outlineColor"] = new("Visual", "outline-color", "outlineColor: \"#0B72E7\""),
        ["fill"] = new("Visual", "fill", "fill: \"#FD7F36\"", "SVG only."),
        ["stroke"] = new("Visual", "stroke", "stroke: \"#FD7F36\"", "SVG only."),
        ["width"] = new("Visual", "width", "width: \"320px\"", "Triggers layout on every frame. A scaleX with a layout animation is usually cheaper and smoother."),
        ["height"] = new("Visual", "height", "height: \"0px\"", "The usual accordion property. Layout-bound, so it cannot go to the compositor."),
        ["borderRadius"] = new("Visual", "border-radius", "borderRadius: \"50%\"", "Percentages and pixels interpolate; mixing the two units in one animation does not."),
        ["boxShadow"] = new("Visual", "box-shadow", "boxShadow: \"0 8px 30px rgba(0,0,0,.2)\"", "Interpolated piece by piece, so both ends need the same shadow count and shape."),
        ["filter"] = new("Visual", "filter", "filter: \"blur(4px)\"", "Both ends need the same filter functions in the same order."),

        ["top"] = new("Layout", "top", "top: \"0px\"", "Prefer y: it does the same thing without touching layout."),
        ["left"] = new("Layout", "left", "left: \"0px\"", "Prefer x."),
        ["right"] = new("Layout", "right", "right: \"0px\""),
        ["bottom"] = new("Layout", "bottom", "bottom: \"0px\""),
        ["margin"] = new("Layout", "margin", "margin: \"16px\""),
        ["padding"] = new("Layout", "padding", "padding: \"24px\""),
        ["gap"] = new("Layout", "gap", "gap: \"12px\""),

        ["letterSpacing"] = new("Typography", "letter-spacing", "letterSpacing: \"0.2em\""),
        ["lineHeight"] = new("Typography", "line-height", "lineHeight: \"1.8\""),
        ["fontSize"] = new("Typography", "font-size", "fontSize: \"2rem\""),

        ["clipPath"] = new("Visual", "clip-path", "clipPath: \"circle(70%)\"", "Both ends need the same shape function."),
        ["backgroundPosition"] = new("Visual", "background-position", "backgroundPosition: \"100% 50%\""),
        ["backgroundSize"] = new("Visual", "background-size", "backgroundSize: \"120%\""),

        ["offsetPath"] = new("Motion path", "offset-path", "offsetPath: \"path('M 0 0 C 80 -60 160 60 240 0')\"", "Set the path once, then animate offsetDistance along it."),
        ["offsetDistance"] = new("Motion path", "offset-distance", "offsetDistance: \"100%\"", "The value that actually moves the element along an offsetPath."),

        ["d"] = new("SVG", "d", "d: \"M 0 0 L 100 0\"", "Shape morphing: both paths need the same command sequence. The one property no Web Animation can play - it is an SVG attribute - so it needs the frame loop, and jumps on Blazor Server."),
        ["pathLength"] = new("SVG", "stroke-dasharray", "pathLength: 1", "0-1 of the stroke drawn. The line-drawing effect."),
        ["pathOffset"] = new("SVG", "stroke-dashoffset", "pathOffset: 0.5"),
        ["pathSpacing"] = new("SVG", "stroke-dasharray", "pathSpacing: 0.2"),

        ["cssVars"] = new("Custom", "--custom-property", "cssVars: new() { [\"--glow\"] = \"18px\" }", "Written verbatim into inline style. See BmCssSafeMode before binding untrusted input."),
        ["css"] = new("Custom", "any CSS property", "css: new() { [\"backdropFilter\"] = \"blur(8px)\" }", "The escape hatch for a property with no dedicated argument."),
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every animatable property, with its measured playback verdict.</summary>
    public static Task<BmotionPropertyDto[]> GetAsync() => _properties.Value;

    /// <summary>A property name the way an agent may have written it - padded or quoted - reduced to the name.</summary>
    public static string Normalize(string name) => name.Trim().Trim('"');

    /// <summary>
    /// Builds a <see cref="BmProps"/> that animates exactly <paramref name="names"/>, using a
    /// representative value for each. The values are placeholders - what the engine decides depends
    /// on which properties are animated and their value shape, not on the numbers themselves.
    /// </summary>
    public static BmProps BuildTarget(IReadOnlyCollection<string> names, out string[] unknown)
    {
        var props = new BmProps();
        var missing = new List<string>();

        foreach (var name in names)
        {
            var key = Normalize(name);

            if (_bmPropsByName.Value.TryGetValue(key, out var property) is false)
            {
                missing.Add(key);
                continue;
            }

            var value = SampleValueFor(property, key);

            if (value is null) missing.Add(key);
            else property.SetValue(props, value);
        }

        unknown = [.. missing];

        return props;
    }

    /// <summary>
    /// The engine key a <see cref="BmProps"/> argument animates: its own name, except for the two
    /// origin fractions, which the engine animates together as one <c>transformOrigin</c>.
    /// </summary>
    public static string EngineKeyOf(string name)
    {
        var key = Normalize(name);

        if (key.Equals("originX", StringComparison.OrdinalIgnoreCase) || key.Equals("originY", StringComparison.OrdinalIgnoreCase))
        {
            return "transformOrigin";
        }

        return _bmPropsByName.Value.TryGetValue(key, out var property) ? CamelCase(property.Name) : key;
    }

    /// <summary>
    /// What the page shows for each of <paramref name="names"/> before the animation starts - what the
    /// engine reads off the computed style for a property it has never seen. Each is a value of the
    /// same kind as the target <see cref="BuildTarget"/> animates to: the same functions and units with
    /// every number at zero, or a transparent colour, as an element shows whose stylesheet or
    /// <c>Initial</c> gives it one. Keyed by the engine's names.
    /// </summary>
    public static Dictionary<string, string> StartingStyles(IEnumerable<string> names)
    {
        // Opacity is read like any other unseen value, and an element nobody has styled is opaque.
        var styles = new Dictionary<string, string>(StringComparer.Ordinal) { ["opacity"] = "1" };

        foreach (var name in names)
        {
            var key = Normalize(name);

            if (_bmPropsByName.Value.TryGetValue(key, out var property) is false) continue;

            switch (SampleValueFor(property, key))
            {
                case BmStringKeyframes target:
                    styles[EngineKeyOf(key)] = SameShapeAtZero(target.First);
                    break;

                // The two origin fractions are written as one pair of percentages.
                case double when EngineKeyOf(key) == "transformOrigin":
                    styles["transformOrigin"] = "0% 0%";
                    break;
            }
        }

        return styles;
    }

    /// <summary>
    /// Explains a measured playback verdict in the engine's own terms: why the engine played the
    /// animation the way it did, and - when that is not the cheapest way it could have - what would
    /// have to change. The remedies for <see cref="BmotionPlaybackPath.FrameLoop"/> say how to get it
    /// to the browser; those for <see cref="BmotionPlaybackPath.Recorded"/>, how to make it a timeline.
    /// </summary>
    /// <param name="properties">The properties the verdict is about, by their <c>Bm.To(...)</c> names.</param>
    /// <param name="transition">The transition they were started with.</param>
    /// <param name="measured">What the engine did with them.</param>
    /// <param name="recordedOnTheirOwn">
    /// Properties the catalog measured as recorded even under a plain tween - values the browser does
    /// not interpolate the way the engine does - so a recording of them is put down to the value
    /// rather than to the transition.
    /// </param>
    public static (string Reason, string[] Remedies) ExplainPlayback(
        IReadOnlyCollection<string> properties, BmTransition transition, BmotionPlaybackPath measured,
        IReadOnlyCollection<string>? recordedOnTheirOwn = null)
    {
        var names = properties.Select(Normalize).ToArray();

        return measured switch
        {
            BmotionPlaybackPath.Timeline => ExplainTimeline(names, transition),
            BmotionPlaybackPath.Recorded => ExplainRecording(names, transition, recordedOnTheirOwn ?? []),
            BmotionPlaybackPath.FrameLoop => ExplainFrameLoop(names, transition),
            _ => ExplainInstant(transition)
        };
    }

    private static (string Reason, string[] Remedies) ExplainTimeline(string[] names, BmTransition transition)
    {
        var curve = transition is BmSpring ? "spring" : "tween";

        var reason = $"Every animated property shares one {curve} curve, and every value is one the browser interpolates " +
                     "exactly as the engine does, so the engine hands the browser a timeline: a Web Animation of two " +
                     "keyframes with the curve as its easing (one keyframe on Blazor Server, where the browser starts from " +
                     "what is on screen). It costs those two keyframes whatever its length, needs no per-frame interop, and " +
                     "plays the same on every runtime - Blazor Server included.";

        return (reason + CostOf(names), []);
    }

    private static (string Reason, string[] Remedies) ExplainRecording(
        string[] names, BmTransition transition, IReadOnlyCollection<string> recordedOnTheirOwn)
    {
        // The engine has already given its verdict; these are the reasons it could have had, named so
        // the caller learns which lever to pull rather than only that a lever exists.
        var why = new List<string>();
        var remedies = new List<string>();

        var ownValues = names.Where(name => recordedOnTheirOwn.Contains(name, StringComparer.OrdinalIgnoreCase)).ToArray();

        if (ownValues.Length > 0)
        {
            why.Add($"{string.Join(", ", ownValues)} {(ownValues.Length == 1 ? "is a value" : "are values")} the browser does " +
                    "not interpolate the way the engine does, whatever the transition");
            remedies.Add($"Nothing in the transition makes {string.Join(", ", ownValues)} a recording - the value does. Where a " +
                         "property the browser interpolates exactly does the same job, animate that one instead.");
        }

        if (transition.Properties is { Count: > 0 } perKey && names.Length > 1 && names.Any(perKey.ContainsKey))
        {
            why.Add("its per-property transitions give the properties different curves, and a timeline has only one");
            remedies.Add("Give every property the same transition - drop Transition.Properties - or split the animation into " +
                         "one call per transition.");
        }

        if (transition is BmInertia)
        {
            why.Add("inertia has no target until it has decelerated, so its glide is recorded rather than described by a curve");
            remedies.Add("A spring or a tween to a known target plays as a timeline.");
        }

        if (transition is BmSpring { Velocity: not 0 })
        {
            why.Add("the spring starts with an initial velocity, which bends its curve by the distance travelled - no one " +
                    "easing describes it");
            remedies.Add("Drop the initial velocity (velocity: 0) for a timeline. A spring that interrupts a moving element " +
                         "inherits its velocity anyway, and is recorded then.");
        }

        if (transition is BmTween { Eases: not null })
        {
            why.Add("the tween eases each keyframe segment separately");
            remedies.Add("One ease for the whole tween lets it play as a timeline.");
        }

        if (transition is BmTween { Duration: <= 0 })
        {
            why.Add("the tween has no duration, so what plays is a delayed jump");
            remedies.Add("Give the tween a duration greater than zero.");
        }

        if (transition.Path is not null
            && names.Contains("x", StringComparer.OrdinalIgnoreCase)
            && names.Contains("y", StringComparer.OrdinalIgnoreCase))
        {
            why.Add("an arc couples x and y along a curve, which no pair of straight keyframes describes");
            remedies.Add("Drop the arc path for a timeline - or keep it: the recording reproduces the curve exactly.");
        }

        if (transition.Repeat is { Count: not 0 } repeat)
        {
            if (repeat.Delay > 0)
            {
                why.Add("the repeat waits between iterations, and a Web Animation's iterations have no gap between them");
                remedies.Add("Remove the repeat delay.");
            }

            if (repeat.Type == BmRepeatType.Mirror)
            {
                why.Add("a mirrored repeat plays its way back with the curve the way out has, which the browser's " +
                        "alternate direction would run backwards instead");
                remedies.Add("Use BmRepeat.Loop when the way back does not have to retrace the way out.");
            }

            if (repeat.Type == BmRepeatType.Reverse)
            {
                why.Add("BmRepeat.Reverse plays forwards once and backwards after that - two different animations");
                remedies.Add("Use BmRepeat.Loop for a timeline.");
            }
        }

        if (why.Count == 0)
        {
            why.Add("its values are keyframe sequences, or ones the browser does not interpolate exactly as the engine does " +
                    "- a gradient, a custom property, a keyword, two different units");
            remedies.Add("Animate between two values of the same shape, with one tween or spring for every property, to " +
                         "get a timeline.");
        }

        var reason = "The browser plays this as a recording: the engine runs its own drivers on a copy of the element's " +
                     "state at 60 Hz, keeps only the frames a straight line between their neighbours cannot reproduce, and " +
                     "hands those to the browser to play with a linear easing - so it plays exactly as the frame loop would " +
                     "have, on every runtime, Blazor Server included. " +
                     $"It is not a timeline because {string.Join("; and ", why)}. " +
                     "A recording sends more keyframes than a timeline's two (a few KB for a long spring), and recording it " +
                     "costs some main-thread time on WebAssembly - server time on Blazor Server - at the moment the animation " +
                     "starts. Prefer a timeline where the motion allows one.";

        return (reason + CostOf(names), [.. remedies]);
    }

    private static (string Reason, string[] Remedies) ExplainFrameLoop(string[] names, BmTransition transition)
    {
        var why = new List<string>();
        var remedies = new List<string>();

        var attributes = names.Where(IsFrameLoopOnlyProperty).ToArray();

        if (attributes.Length > 0)
        {
            why.Add($"{string.Join(", ", attributes)} {(attributes.Length == 1 ? "is an SVG geometry attribute" : "are SVG geometry attributes")}, " +
                    "and Web Animations cannot animate attributes");
            remedies.Add("There is no browser equivalent of a shape morph. Cross-fade two stacked shapes with opacity, or draw " +
                         "the stroke with pathLength - both play in the browser on every runtime - or gate the morph on " +
                         "BmotionCapabilities.SupportsFrameLoop.");
        }

        if (transition.OnUpdate is not null || transition.Properties?.Values.Any(perKey => perKey.OnUpdate is not null) is true)
        {
            why.Add("OnUpdate needs a C# callback on every frame, which only the frame loop can give it");
            remedies.Add("Remove Transition.OnUpdate: without a per-frame callback the browser plays the animation, on every " +
                         "runtime. Read the end state once the animation completes instead.");
        }

        if (transition.Repeat is { IsForever: true, Type: BmRepeatType.Reverse })
        {
            why.Add("an endless BmRepeat.Reverse plays its first cycle forwards and every one after it backwards, which no " +
                    "single repeating recording reproduces");
            remedies.Add("Use BmRepeat.Mirror, which the browser plays as one recorded cycle there and back on a loop, or give " +
                         "the Reverse a count.");
        }

        if (transition.Properties is { Count: > 0 } perProperty
            && (transition.Repeat is { IsForever: true } || perProperty.Values.Any(perKey => perKey.Repeat is { IsForever: true })))
        {
            why.Add("per-property transitions together with an endless repeat cannot be recorded as one shared cycle");
            remedies.Add("Give the endlessly repeating properties one shared transition - drop Transition.Properties - or move " +
                         "them into an animation of their own.");
        }

        if (RecordedSeconds(transition) is { } seconds && seconds > RecordingLimitSeconds)
        {
            why.Add($"only a recording reproduces it, and it would take {seconds:0.#} seconds to record - past the " +
                    $"{RecordingLimitSeconds:0} seconds the engine records");
            remedies.Add("Shorten it, or repeat it forever rather than a fixed number of times: an endless BmRepeat.Loop or " +
                         "BmRepeat.Mirror records one cycle, and the browser repeats it.");
        }
        else if (why.Count == 0 && transition is BmSpring or BmInertia)
        {
            // Nothing else keeps a spring or an inertia off the browser, so what is left is its length,
            // which falls out of the physics rather than being configured anywhere it could be read.
            why.Add($"it settles too slowly to record - the engine records at most {RecordingLimitSeconds:0} seconds " +
                    "of an animation the browser cannot play as a timeline");
            remedies.Add("Settle it sooner - more damping, or Bm.Spring(bounce:, duration:) - and check how long it takes " +
                         "with SimulateBmotionTransition.");
        }

        var reason = why.Count > 0
            ? $"The engine kept this on the C# frame loop because {string.Join("; and ", why)}. On Blazor WebAssembly it " +
              "animates frame by frame; on Blazor Server there is no frame loop, so it becomes an instant state change."
            : "The engine kept this on the C# frame loop, so on Blazor Server it becomes an instant state change. None of " +
              "the reasons the engine has for that apply - a per-frame OnUpdate, an SVG d or points morph, an endless " +
              "BmRepeat.Reverse, per-property transitions with an endless repeat, a recording past " +
              $"{RecordingLimitSeconds:0} seconds - so this combination is worth reporting.";

        if (remedies.Count == 0)
        {
            remedies.Add("Leave out what only the frame loop plays - a per-frame OnUpdate, a d or points morph, an endless " +
                         "BmRepeat.Reverse - and keep a recording under " +
                         $"{RecordingLimitSeconds:0} seconds; the browser then plays the animation on every runtime.");
        }

        return (reason, [.. remedies]);
    }

    private static (string Reason, string[] Remedies) ExplainInstant(BmTransition transition)
    {
        var reason = "The transition has no length - a tween with no duration, delay or repeat - so the engine sets the " +
                     "values at once, on every runtime alike. Nothing is animated, on Blazor Server or anywhere else.";

        string[] remedies = transition is BmTween { Duration: <= 0 }
            ? ["Give the tween a duration greater than zero for it to animate."]
            : [];

        return (reason, remedies);
    }

    /// <summary>
    /// How long a tween takes to record, which is how long it plays: every repeat a fixed count
    /// unrolls, with the delays between them, or one cycle of an endless one (the way out and back for
    /// a mirror). Null when the transition is not a tween, whose length falls out of its physics.
    /// </summary>
    private static double? RecordedSeconds(BmTransition transition)
    {
        if (transition is not BmTween { Duration: > 0 } tween) return null;

        if (tween.Repeat is not { Count: not 0 } repeat) return tween.Duration;

        if (repeat.IsForever)
        {
            return (tween.Duration * (repeat.Type == BmRepeatType.Mirror ? 2 : 1)) + repeat.Delay;
        }

        return (tween.Duration * (repeat.Count + 1)) + (repeat.Delay * repeat.Count);
    }

    /// <summary>
    /// What the browser still pays on every frame for the properties off its compositor, with the
    /// compositor equivalent of each - the advice that outlived "width does not work on Server": it
    /// works everywhere now, and it is still the slower way to move an element.
    /// </summary>
    private static string CostOf(string[] names)
    {
        var hints = new List<string>();

        foreach (var name in names)
        {
            var hint = name switch
            {
                "width" or "height" => $"{name} runs layout on every frame, where a scale (with Layout=\"BmLayout.Size\" " +
                                       "when the content must not stretch) moves on the compositor",
                "top" or "left" or "right" or "bottom" => $"{name} runs layout on every frame, where x/y move the element on " +
                                                          "the compositor",
                "backgroundColor" or "color" or "borderColor" or "outlineColor" or "fill" or "stroke" =>
                    $"{name} repaints on every frame, where an opacity cross-fade of two stacked elements stays on the compositor",
                _ => null
            };

            if (hint is not null) hints.Add(hint);
        }

        return hints.Count > 0 ? $" Off the compositor it still costs the page something: {string.Join("; ", hints)}." : string.Empty;
    }

    /// <summary>
    /// The properties the browser animates on its compositor - the transform components and opacity -
    /// which run off the main thread with no layout or paint per frame. A browser fact about cost, not
    /// the engine's rule for where an animation plays: the browser plays nearly everything now (see
    /// <see cref="IsFrameLoopOnlyProperty"/>). Public because the explanations name the compositor
    /// equivalents of the rest.
    /// </summary>
    public static bool IsCompositorProperty(string name)
    {
        return name.ToLowerInvariant() is "x" or "y" or "z" or "scale" or "scalex" or "scaley"
            or "rotate" or "rotatex" or "rotatey" or "rotatez" or "skewx" or "skewy"
            or "perspective" or "opacity";
    }

    /// <summary>
    /// The engine's own rule for the properties no Web Animation can play whatever the transition: the
    /// SVG geometry attributes <c>d</c> and <c>points</c>, since Web Animations animate CSS, not
    /// attributes. Public because the code review asks the same question of a property name it read out
    /// of a source file, where there is no engine run to observe.
    /// </summary>
    public static bool IsFrameLoopOnlyProperty(string name)
    {
        return Normalize(name).ToLowerInvariant() is "d" or "points";
    }

    /// <summary>
    /// Animates each property once, on its own, through a real engine and records what the engine did
    /// with it. Runs once per process, behind the Lazy above.
    /// </summary>
    private static async Task<BmotionPropertyDto[]> ProbeAsync()
    {
        var results = new List<BmotionPropertyDto>();

        foreach (var (name, property) in _bmPropsByName.Value.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            var facts = _facts.GetValueOrDefault(name)
                ?? new Facts("Other", name, $"{name}: ...");

            var playback = await ProbePlaybackAsync(name);
            var played = playback is BmotionPlaybackPath.Timeline or BmotionPlaybackPath.Recorded;

            results.Add(new BmotionPropertyDto
            {
                Name = name,
                Category = facts.Category,
                Css = facts.Css,
                ValueType = FriendlyValueType(property.PropertyType),
                PlayedByBrowser = played,
                BrowserPlayback = played ? playback.ToString() : null,
                CompositorEligible = played && IsCompositorProperty(name),
                OnBlazorServer = played ? "Animates" : "Jumps to the target",
                Example = facts.Example,
                Notes = facts.Notes
            });
        }

        return [.. results
            .OrderBy(property => CategoryOrder(property.Category))
            .ThenBy(property => property.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Starts a plain tween on one property, as Blazor Server, and reports how the engine handed it to
    /// the browser. A tween with a length that the browser was not handed is one only the frame loop can
    /// play - which on Server means it landed at once.
    /// </summary>
    private static async Task<BmotionPlaybackPath> ProbePlaybackAsync(string name)
    {
        try
        {
            var (target, starts) = ProbeTargetFor(name);

            var observed = await BmotionMotionLab.ObserveAsync(target, new BmTween { Duration = 0.3 }, starts, onWebAssembly: false);

            return observed.Browser switch
            {
                null => BmotionPlaybackPath.FrameLoop,
                { Recorded: true } => BmotionPlaybackPath.Recorded,
                _ => BmotionPlaybackPath.Timeline
            };
        }
        catch (Exception)
        {
            // The catalog is built once and cached for the life of the process, so a throw here would
            // not cost one property its verdict - it would leave the whole tool answering with the
            // same exception forever. A property that could not be probed is reported the way the
            // engine reports what it cannot hand over: not played by the browser.
            return BmotionPlaybackPath.FrameLoop;
        }
    }

    /// <summary>
    /// What the probe animates a property to, and what the page shows for it beforehand. The two
    /// dictionary-valued escape hatches have no single representative value of their own, so each is
    /// probed with the entry its example shows: a custom property, and a CSS property with no
    /// dedicated argument.
    /// </summary>
    private static (BmProps Target, Dictionary<string, string> Starts) ProbeTargetFor(string name)
    {
        if (name.Equals("cssVars", StringComparison.OrdinalIgnoreCase))
        {
            return (new BmProps { CssVars = new() { ["--glow"] = "18px" } },
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["--glow"] = "0px" });
        }

        if (name.Equals("css", StringComparison.OrdinalIgnoreCase))
        {
            return (new BmProps { Css = new() { ["backdropFilter"] = (BmStringKeyframes)"blur(8px)" } },
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["backdropFilter"] = "blur(0px)" });
        }

        return (BuildTarget([name], out _), StartingStyles([name]));
    }

    /// <summary>
    /// A value of the right shape for a property. Which value hardly matters - the engine decides on
    /// the value's <i>kind</i> - but it has to be one the drivers accept, or the probe would report
    /// a verdict about a malformed target rather than about the property.
    /// </summary>
    private static object? SampleValueFor(PropertyInfo property, string name)
    {
        if (property.PropertyType == typeof(BmKeyframes))
        {
            // Scale and opacity rest at 1, so animating them to 0 is a real change of state; for
            // everything else any non-zero target moves the element.
            return (BmKeyframes)(name.StartsWith("scale", StringComparison.OrdinalIgnoreCase) || name is "opacity" ? 0d : 40d);
        }

        if (property.PropertyType == typeof(BmStringKeyframes))
        {
            return (BmStringKeyframes)(_facts.GetValueOrDefault(name)?.Category switch
            {
                "SVG" when name is "d" => "M 0 0 L 100 0",
                "Motion path" when name is "offsetPath" => "path('M 0 0 L 100 0')",
                "Motion path" => "100%",
                _ when name.EndsWith("Color", StringComparison.OrdinalIgnoreCase) || name is "fill" or "stroke" => "#FD7F36",
                _ when name is "clipPath" => "circle(70%)",
                _ when name is "filter" => "blur(4px)",
                _ when name is "boxShadow" => "0 8px 30px rgba(0,0,0,0.2)",
                _ when name is "backgroundPosition" => "100% 50%",
                _ => "24px"
            });
        }

        if (property.PropertyType == typeof(double?)) return 0.5;

        // CssVars and Css are dictionaries: real targets, but not single properties, so they have no
        // representative single value to build an animation of one property with.
        return null;
    }

    /// <summary>
    /// The value an animation to <paramref name="target"/> starts from on a page that styles the
    /// element: a transparent colour for a colour, and otherwise the target's own shape - the same
    /// functions, the same units - with every number at zero, so "blur(4px)" starts from "blur(0px)"
    /// and "100% 50%" from "0% 0%".
    /// </summary>
    private static string SameShapeAtZero(string target)
    {
        if (target.StartsWith('#') || ColourFunctionRegex().IsMatch(target)) return "rgba(0, 0, 0, 0)";

        return NumberRegex().Replace(target, "0");
    }

    private static string FriendlyValueType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(BmKeyframes)) return "BmKeyframes (number or number[])";
        if (underlying == typeof(BmStringKeyframes)) return "BmStringKeyframes (CSS value or CSS value[])";
        if (underlying == typeof(double)) return "double";
        if (underlying == typeof(Dictionary<string, string>)) return "Dictionary<string, string>";
        if (underlying == typeof(Dictionary<string, BmStringKeyframes>)) return "Dictionary<string, BmStringKeyframes>";

        return underlying.Name;
    }

    private static int CategoryOrder(string category) => category switch
    {
        "Transform" => 0,
        "Visual" => 1,
        "Layout" => 2,
        "Typography" => 3,
        "Motion path" => 4,
        "SVG" => 5,
        "Custom" => 6,
        _ => 7
    };

    private static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    [GeneratedRegex(@"^\s*(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\(", RegexOptions.IgnoreCase)]
    private static partial Regex ColourFunctionRegex();

    [GeneratedRegex(@"-?\d*\.?\d+")]
    private static partial Regex NumberRegex();
}
