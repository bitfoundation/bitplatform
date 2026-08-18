using System.Reflection;
using System.Collections.Frozen;
using Bit.Bmotion.Demo.Server.Dtos;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Every property Bit.Bmotion can animate, and the one thing about each that an agent cannot read
/// off the API: what animating it costs.
/// <para>
/// The list itself comes from reflecting over <see cref="BmProps"/>, so a property added to the
/// library appears here without anyone remembering to add it. The expensive claim - whether the
/// browser compositor can own the animation, and therefore whether it survives on Blazor Server -
/// is <b>measured</b>: each property is animated once through the real engine at first use and the
/// catalog records which playback path the engine chose. Writing that table by hand would mean
/// maintaining a second copy of a rule set that lives in the engine, and being wrong about it in
/// exactly the cases that matter.
/// </para>
/// </summary>
public static class BmotionPropertyCatalog
{
    /// <summary>Descriptive metadata for a property: everything except the measured verdict.</summary>
    private sealed record Facts(string Category, string Css, string Example, string? Notes = null);

    private static readonly Lazy<Task<BmotionPropertyDto[]>> _properties = new(ProbeAsync);

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
        ["originX"] = new("Transform", "transform-origin", "originX: 0", "0-1 across the element. Not animated - it is set, and it changes what every transform pivots around."),
        ["originY"] = new("Transform", "transform-origin", "originY: 1", "0-1 down the element. Set both to move the pivot, e.g. originY: 1 to grow upward."),

        ["opacity"] = new("Visual", "opacity", "opacity: 0", "The other compositor-cheap property. Prefer it to visibility or display, which cannot be animated at all."),
        ["backgroundColor"] = new("Visual", "background-color", "backgroundColor: \"#FD7F36\"", "Interpolated in C#; see BmColorSpace for Oklab/LCH mixing rather than sRGB."),
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

        ["d"] = new("SVG", "d", "d: \"M 0 0 L 100 0\"", "Shape morphing: both paths need the same command sequence."),
        ["pathLength"] = new("SVG", "stroke-dasharray", "pathLength: 1", "0-1 of the stroke drawn. The line-drawing effect."),
        ["pathOffset"] = new("SVG", "stroke-dashoffset", "pathOffset: 0.5"),
        ["pathSpacing"] = new("SVG", "stroke-dasharray", "pathSpacing: 0.2"),

        ["cssVars"] = new("Custom", "--custom-property", "cssVars: new() { [\"--glow\"] = \"18px\" }", "Written verbatim into inline style. See BmCssSafeMode before binding untrusted input."),
        ["css"] = new("Custom", "any CSS property", "css: new() { [\"backdropFilter\"] = \"blur(8px)\" }", "The escape hatch for a property with no dedicated argument."),
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every animatable property, with its measured playback verdict.</summary>
    public static Task<BmotionPropertyDto[]> GetAsync() => _properties.Value;

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
            var key = name.Trim().Trim('"');

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
    /// Explains the engine's playback decision in the engine's own terms, and - when the animation
    /// stayed on the frame loop - what would have to change for the compositor to take it.
    /// </summary>
    public static (string Reason, string[] Remedies) ExplainPlayback(
        IReadOnlyCollection<string> properties, BmTransition transition, bool offloaded)
    {
        if (offloaded)
        {
            return ("Every animated property is a transform component or opacity, and the transition has no " +
                    "feature the Web Animations API cannot express. The engine pre-samples the curve in C# and " +
                    "hands the whole animation to the browser, so it plays off the main thread with no per-frame " +
                    "interop - which is also why it works on Blazor Server.", []);
        }

        // The engine has already given its verdict; these are the reasons it could have had, named
        // so the caller learns which lever to pull rather than only that a lever exists.
        var blockers = new List<string>();
        var remedies = new List<string>();

        var ineligible = properties
            .Select(name => name.Trim().Trim('"'))
            .Where(name => IsCompositorProperty(name) is false)
            .ToArray();

        if (ineligible.Length > 0)
        {
            blockers.Add($"{string.Join(", ", ineligible)} - only transform components (x, y, z, scale, scaleX, " +
                         "scaleY, rotate, rotateX, rotateY, rotateZ, skewX, skewY, perspective) and opacity can be " +
                         "handed to the browser");

            foreach (var name in ineligible)
            {
                remedies.Add(name switch
                {
                    "width" or "height" => $"Replace {name} with a scale (plus Layout=\"BmLayout.Size\" when the content must not stretch).",
                    "top" or "left" or "right" or "bottom" => $"Replace {name} with x/y, which move the element without touching layout.",
                    "backgroundColor" or "color" or "borderColor" or "outlineColor" or "fill" or "stroke" =>
                        $"Cross-fade {name} with two stacked elements animating opacity, or accept that it snaps on Blazor Server.",
                    _ => $"There is no compositor equivalent for {name}; it needs the frame loop."
                });
            }
        }

        if (transition is BmInertia)
        {
            blockers.Add("the transition is inertia, whose target is not known until the motion decelerates");
            remedies.Add("Use a spring or a tween when the animation has to play on Blazor Server.");
        }

        if (transition is BmSpring { Velocity: not 0 })
        {
            blockers.Add("the spring starts with an initial velocity, which produces a curve that depends on the " +
                         "distance travelled and cannot be shipped as one shared easing");
            remedies.Add("Drop the initial velocity (velocity: 0) to let the spring be pre-sampled.");
        }

        if (transition is BmTween { Duration: <= 0 })
        {
            blockers.Add("the tween has no duration");
            remedies.Add("Give the tween a duration greater than zero.");
        }

        if (transition.Properties is { Count: > 0 })
        {
            blockers.Add("the transition carries per-property overrides, which the compositor cannot express as one timeline");
            remedies.Add("Split the animation into one call per property group instead of using Transition.Properties.");
        }

        if (transition.Repeat is { Delay: > 0 })
        {
            blockers.Add("the repeat has a delay between iterations");
            remedies.Add("Remove the repeat delay, or accept the frame loop.");
        }

        if (transition.Repeat is { Type: BmRepeatType.Reverse })
        {
            blockers.Add("BmRepeat.Reverse has no Web Animations API equivalent");
            remedies.Add("Use BmRepeat.Mirror, which maps to the browser's alternate direction.");
        }

        if (transition.Path is not null)
        {
            blockers.Add("an arc path couples x and y along a curve, which pre-sampling each property separately would flatten");
            remedies.Add("Drop the arc path to regain the compositor, or keep it and accept the frame loop.");
        }

        if (transition.OnUpdate is not null)
        {
            blockers.Add("OnUpdate needs a C# callback on every frame, which only the frame loop can give it");
        }

        var reason = blockers.Count > 0
            ? $"The engine kept this on the C# frame loop because {string.Join("; and ", blockers)}. On Blazor " +
              "Server there is no frame loop, so it becomes an instant state change."
            : "The engine kept this on the C# frame loop. On Blazor Server it becomes an instant state change. " +
              "None of the usual blockers apply, so this is a combination worth reporting.";

        if (remedies.Count == 0)
        {
            remedies.Add("Animate transform components (x, y, scale, rotate) and opacity only, with a plain spring " +
                         "or tween, to keep an animation on the compositor.");
        }

        return (reason, [.. remedies]);
    }

    /// <summary>
    /// The engine's own rule for what the browser compositor can own: transform components and
    /// opacity, nothing else. Public because the code review asks the same question of a property
    /// name it read out of a source file, where there is no engine run to observe.
    /// </summary>
    public static bool IsCompositorProperty(string name)
    {
        return name.ToLowerInvariant() is "x" or "y" or "z" or "scale" or "scalex" or "scaley"
            or "rotate" or "rotatex" or "rotatey" or "rotatez" or "skewx" or "skewy"
            or "perspective" or "opacity";
    }

    /// <summary>
    /// Animates each property once, on its own, through a real engine and records which path the
    /// engine took. Runs once per process, behind the Lazy above.
    /// </summary>
    private static async Task<BmotionPropertyDto[]> ProbeAsync()
    {
        var results = new List<BmotionPropertyDto>();

        foreach (var (name, property) in _bmPropsByName.Value.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            var facts = _facts.GetValueOrDefault(name)
                ?? new Facts("Other", name, $"{name}: ...");

            var eligible = await ProbeCompositorAsync(name);

            results.Add(new BmotionPropertyDto
            {
                Name = name,
                Category = facts.Category,
                Css = facts.Css,
                ValueType = FriendlyValueType(property.PropertyType),
                CompositorEligible = eligible,
                OnBlazorServer = eligible ? "Animates" : "Jumps to the target",
                Example = facts.Example,
                Notes = facts.Notes
            });
        }

        return [.. results
            .OrderBy(property => CategoryOrder(property.Category))
            .ThenBy(property => property.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Starts a plain tween on one property and reports whether the engine handed it to the browser.
    /// The animation is stopped immediately afterwards: the verdict is taken before the first frame,
    /// and nothing here is waiting for the motion to finish.
    /// </summary>
    private static async Task<bool> ProbeCompositorAsync(string name)
    {
        var props = BuildTarget([name], out var unknown);

        // A property with no representative value (the dictionary-valued escape hatches) cannot be
        // probed; it is a CSS write, which is never compositor-eligible.
        if (unknown.Length > 0) return false;

        var interop = new HeadlessBmotionInterop();
        await using var engine = new BmotionAnimationEngine(interop);
        var service = new BmotionAnimateService(engine, interop);

        var controls = await service.AnimateAsync(".bmotion-probe", props, new BmTween { Duration = 0.3 });

        // The hand-off is asynchronous; give its continuations a turn before reading the verdict.
        await Task.Yield();
        await Task.Yield();

        controls.Stop();

        return interop.WaapiCalls.Count > 0;
    }

    /// <summary>
    /// A value of the right shape for a property. Which value hardly matters - the engine decides on
    /// the value's <i>kind</i> - but it has to be one the drivers accept, or the probe would report
    /// "not offloaded" for a property that simply got a malformed target.
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
        // representative single value to probe with.
        return null;
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
}
