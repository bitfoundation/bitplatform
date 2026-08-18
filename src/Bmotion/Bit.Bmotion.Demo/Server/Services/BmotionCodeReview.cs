using Bit.Bmotion.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Reviews a piece of Bmotion markup for the mistakes that compile cleanly and then do nothing.
/// <para>
/// Almost every way of getting Bmotion wrong is silent. An <c>Exit</c> without a presence component
/// around it is valid Razor that never plays, because Blazor removed the element before the
/// animation could start. <c>Bm.Spring(duration: 0.5)</c> is a valid call whose duration the engine
/// ignores. A <c>&lt;Bmotion&gt;</c> in a <c>@foreach</c> without a <c>@key</c> animates the wrong
/// rows when the list changes. None of these produce a warning from the compiler, a message in the
/// console, or an exception - the page simply does not move, or moves wrongly, and the usual next
/// step is to add more animation code on top of the part that was never running.
/// </para>
/// <para>
/// This is the pass that closes the loop after an agent writes code: generate, review, fix. Each
/// finding names the rule, the line, and the correction - so it can be acted on without another
/// round of searching.
/// </para>
/// </summary>
public static partial class BmotionCodeReview
{
    /// <summary>The rules this review applies, so an empty result is not mistaken for an unchecked one.</summary>
    public static readonly string[] Rules =
    [
        "exit-without-presence",
        "spring-duration-without-bounce",
        "animate-without-initial",
        "missing-key-in-loop",
        "nested-quotes-in-attribute",
        "component-as-animated-root",
        "empty-bmotion",
        "frame-loop-only-properties",
        "drag-without-capability-guard",
        "eased-infinite-rotation",
        "resting-state-in-gesture",
        "reduced-motion-not-configured",
    ];

    /// <summary>Reviews Razor or C# source and reports what will not work as written.</summary>
    public static BmotionReviewDto Review(string? code)
    {
        var text = code ?? string.Empty;
        var findings = new List<BmotionReviewFindingDto>();

        if (text.Trim().Length == 0)
        {
            return new BmotionReviewDto
            {
                Passed = true,
                Findings = [],
                RulesApplied = Rules
            };
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        var hasPresence = text.Contains("BmotionAnimatePresence", StringComparison.Ordinal)
                       || text.Contains("BmotionPresenceGroup", StringComparison.Ordinal)
                       || text.Contains("BmotionPresenceSwitch", StringComparison.Ordinal);

        CheckExit(lines, hasPresence, findings);
        CheckSpringDuration(lines, findings);
        CheckInitial(text, lines, findings);
        CheckLoopKeys(lines, findings);
        CheckNestedQuotes(lines, findings);
        CheckAnimatedRoot(lines, findings);
        CheckEmptyBmotion(lines, findings);
        CheckFrameLoopProperties(lines, findings);
        CheckDragGuard(text, lines, findings);
        CheckEasedRotation(lines, findings);
        CheckRestingStateInGesture(lines, findings);
        CheckReducedMotion(text, lines, findings);

        var ordered = findings
            .OrderBy(finding => SeverityOrder(finding.Severity))
            .ThenBy(finding => finding.Line ?? int.MaxValue)
            .ToArray();

        return new BmotionReviewDto
        {
            Passed = ordered.Any(finding => finding.Severity != "Suggestion") is false,
            Findings = ordered,
            RulesApplied = Rules
        };
    }

    /// <summary>
    /// An Exit target with nothing to hold the element in the DOM while it plays. This is the single
    /// most common Bmotion bug, and it looks exactly like an animation that "does not work".
    /// </summary>
    private static void CheckExit(string[] lines, bool hasPresence, List<BmotionReviewFindingDto> findings)
    {
        if (hasPresence) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (ExitAttributeRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmotionReviewFindingDto
            {
                Severity = "Error",
                Rule = "exit-without-presence",
                Line = i + 1,
                Message = "Exit is set, but nothing in this markup keeps the element alive while the exit animation " +
                          "plays. Blazor removes the element as soon as the condition changes, so Exit never runs.",
                Fix = "Wrap the element in <BmotionAnimatePresence IsPresent=\"...\">, or use BmotionPresenceGroup " +
                      "for a list and BmotionPresenceSwitch when one item replaces another."
            });
        }
    }

    /// <summary>
    /// A spring given a duration but no bounce. The duration is only used to derive stiffness and
    /// damping when bounce is present, so on its own it is silently ignored.
    /// </summary>
    private static void CheckSpringDuration(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match match in SpringCallRegex().Matches(lines[i]))
            {
                var arguments = match.Groups["args"].Value;

                if (arguments.Contains("duration", StringComparison.OrdinalIgnoreCase) is false) continue;
                if (arguments.Contains("bounce", StringComparison.OrdinalIgnoreCase)) continue;

                findings.Add(new BmotionReviewFindingDto
                {
                    Severity = "Warning",
                    Rule = "spring-duration-without-bounce",
                    Line = i + 1,
                    Message = "Bm.Spring(duration: ...) without a bounce does not set the spring's length. Duration " +
                              "is only used to derive stiffness and damping when bounce is given as well; on its " +
                              "own it is ignored, and the spring runs at its default stiffness of 100 and damping " +
                              "of 10.",
                    Fix = "Pass both - Bm.Spring(bounce: 0.2, duration: 0.5) - or configure the physics directly " +
                          "with Bm.Spring(stiffness: ..., damping: ...). Use SimulateBmotionTransition to see how " +
                          "long the result actually takes."
                });
            }
        }
    }

    /// <summary>
    /// An Animate target with no Initial. The element is already at its resting state when it
    /// mounts, so there is nothing to animate from and the entrance never happens.
    /// </summary>
    private static void CheckInitial(string text, string[] lines, List<BmotionReviewFindingDto> findings)
    {
        // Only for markup that is clearly an entrance: a variant-driven or state-driven element
        // animates on change instead, and gesture overlays have their own resting state.
        if (text.Contains("Variants", StringComparison.Ordinal)) return;
        if (text.Contains("Timeline", StringComparison.Ordinal)) return;

        for (int i = 0; i < lines.Length; i++)
        {
            var match = BmotionOpenTagRegex().Match(string.Join('\n', lines[i..Math.Min(lines.Length, i + 12)]));

            if (match.Success is false || match.Index > lines[i].Length) continue;

            var tag = match.Value;

            if (tag.Contains("Animate=", StringComparison.Ordinal) is false) continue;
            if (tag.Contains("Initial=", StringComparison.Ordinal)) continue;
            // An element whose Animate is bound to state animates when that state changes, which is
            // a different (and correct) pattern from an entrance. Only the bound Animate value says
            // so: an underscore in a CSS class or in an unrelated attribute is not state.
            if (tag.Contains("@(", StringComparison.Ordinal)) continue;

            var animate = AnimateValueRegex().Match(tag);

            if (animate.Success && PrivateFieldRegex().IsMatch(animate.Groups["value"].Value)) continue;

            findings.Add(new BmotionReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "animate-without-initial",
                Line = i + 1,
                Message = "Animate is set to a constant target with no Initial. The element mounts already at that " +
                          "target, so nothing animates on the first render.",
                Fix = "Add an Initial for the state to animate from, e.g. Initial=\"Bm.To(opacity: 0, y: 20)\". " +
                      "If the animation is meant to run on a state change rather than on mount, bind Animate to " +
                      "that state instead."
            });
        }
    }

    /// <summary>A &lt;Bmotion&gt; inside a loop with no @key: Blazor reuses elements across items.</summary>
    private static void CheckLoopKeys(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        var loopStart = -1;
        var braces = 0;
        var bodyOpened = false;

        for (int i = 0; i < lines.Length; i++)
        {
            if (loopStart < 0)
            {
                if (LoopRegex().IsMatch(lines[i]) is false) continue;

                loopStart = i;
                braces = 0;
                bodyOpened = false;
            }

            if (lines[i].Contains("<Bmotion", StringComparison.Ordinal))
            {
                var window = string.Join('\n', lines[i..Math.Min(lines.Length, i + 4)]);

                if (window.Contains("@key", StringComparison.Ordinal) is false)
                {
                    findings.Add(new BmotionReviewFindingDto
                    {
                        Severity = "Warning",
                        Rule = "missing-key-in-loop",
                        Line = i + 1,
                        Message = "An animated element inside a loop has no @key. Blazor reuses DOM elements between " +
                                  "renders, so when the collection changes an item's animation state carries over to " +
                                  "whatever item takes its place - entrances are skipped and exits play on the wrong row.",
                        Fix = "Add @key=\"item.Id\" (any stable identity) to the <Bmotion> element."
                    });

                    loopStart = -1;

                    continue;
                }
            }

            // Where the loop body ends. Tracking its braces rather than counting lines is what keeps
            // the rule off the markup *after* a correctly keyed loop - the else branch of the @if
            // around it is not a second iteration of anything, and reporting it there is the kind of
            // false positive that teaches an agent to stop reading the review.
            foreach (var c in lines[i])
            {
                if (c == '{') { braces++; bodyOpened = true; }
                else if (c == '}') braces--;
            }

            // The line cap stays as the backstop for a body this cannot read - one whose braces sit
            // inside a Razor expression, or a single-line body written without any.
            if ((bodyOpened && braces <= 0) || i - loopStart > 12) loopStart = -1;
        }
    }

    /// <summary>
    /// A Razor attribute delimited by double quotes whose value contains a string literal. The
    /// attribute ends at the inner quote, so the markup means something other than it looks like.
    /// </summary>
    private static void CheckNestedQuotes(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match match in DoubleQuotedAttributeRegex().Matches(lines[i]))
            {
                var value = match.Groups["value"].Value;

                if (value.Contains("Bm.", StringComparison.Ordinal) is false) continue;

                // The value as Razor sees it ends at the next double quote. If the Bm call inside it
                // still has an unclosed parenthesis at that point, the attribute was cut short by a
                // quote that was meant to be part of the value - a colour, a CSS length, a path.
                var depth = 0;

                foreach (var c in value)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth--;
                }

                if (depth <= 0) continue;

                var name = match.Groups["name"].Value;

                findings.Add(new BmotionReviewFindingDto
                {
                    Severity = "Error",
                    Rule = "nested-quotes-in-attribute",
                    Line = i + 1,
                    Message = $"The {name} attribute is delimited by double quotes and its value contains a " +
                              "double-quoted string. Razor ends the attribute at that inner quote, so the call is " +
                              "left unclosed and the markup does not mean what it looks like.",
                    Fix = $"Single-quote the attribute instead: {name}='Bm.To(...)', keeping the double-quoted " +
                          "literal inside it."
                });
            }
        }
    }

    /// <summary>
    /// A component as the first child of &lt;Bmotion&gt;. The engine injects its id and initial style
    /// into the first root HTML element, and a component is not one.
    /// </summary>
    private static void CheckAnimatedRoot(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("<Bmotion", StringComparison.Ordinal) is false) continue;

            // The child is the next non-blank line that opens a tag, once the opening tag has closed.
            for (int j = i; j < Math.Min(lines.Length, i + 8); j++)
            {
                if (lines[j].Contains('>', StringComparison.Ordinal) is false) continue;

                var child = FirstChildTagRegex().Match(string.Join('\n', lines[(j + 1)..Math.Min(lines.Length, j + 3)]));

                if (child.Success is false) break;

                var name = child.Groups["tag"].Value;

                // A component is PascalCase; an HTML element is not. Bmotion's own components are
                // excluded: nesting them is normal and they resolve to elements of their own.
                if (char.IsUpper(name[0]) is false || name.StartsWith("Bmotion", StringComparison.Ordinal)) break;

                findings.Add(new BmotionReviewFindingDto
                {
                    Severity = "Warning",
                    Rule = "component-as-animated-root",
                    Line = j + 2,
                    Message = $"The first child of <Bmotion> is the component <{name}>, not an HTML element. " +
                              "Bmotion injects the engine id and the initial inline style into the first root HTML " +
                              "element of its content, so there is nothing here for it to animate.",
                    Fix = $"Wrap it in a plain element - <div><{name} /></div> - or move the <Bmotion> inside " +
                          $"{name} around the element that should actually move."
                });

                break;
            }
        }
    }

    /// <summary>A self-closed &lt;Bmotion /&gt;: it wraps the element it animates, so it needs one.</summary>
    private static void CheckEmptyBmotion(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (SelfClosedBmotionRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmotionReviewFindingDto
            {
                Severity = "Error",
                Rule = "empty-bmotion",
                Line = i + 1,
                Message = "<Bmotion /> is self-closed, so it has no content. Bmotion animates the element written " +
                          "inside it; with no child there is nothing on the page for it to touch.",
                Fix = "Put the element inside it: <Bmotion ...><div class=\"box\" /></Bmotion>."
            });
        }
    }

    /// <summary>
    /// Properties the browser compositor cannot own. They animate on WebAssembly and jump on Blazor
    /// Server, which is a difference no build output will point out.
    /// </summary>
    private static void CheckFrameLoopProperties(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match match in TargetCallRegex().Matches(lines[i]))
            {
                foreach (Match argument in ArgumentNameRegex().Matches(match.Groups["args"].Value))
                {
                    var name = argument.Groups["name"].Value;

                    if (name is "transition") continue;
                    if (BmotionPropertyCatalog.IsCompositorProperty(name)) continue;
                    if (reported.Add(name) is false) continue;

                    findings.Add(new BmotionReviewFindingDto
                    {
                        Severity = "Suggestion",
                        Rule = "frame-loop-only-properties",
                        Line = i + 1,
                        Message = $"'{name}' cannot be handed to the browser compositor, so this animation runs on " +
                                  "the C# per-frame loop. That is fine on Blazor WebAssembly, and on Blazor Server " +
                                  "it becomes an instant jump to the target.",
                        Fix = "Ignore this if the app is WebAssembly-only. Otherwise animate x, y, scale, rotate " +
                              "and opacity instead, and confirm with AnalyzeBmotionAnimation."
                    });
                }
            }
        }
    }

    /// <summary>Drag on a page that may be served over Blazor Server, with nothing to say so.</summary>
    private static void CheckDragGuard(string text, string[] lines, List<BmotionReviewFindingDto> findings)
    {
        if (text.Contains("SupportsFrameLoop", StringComparison.Ordinal)) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (DragAttributeRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmotionReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "drag-without-capability-guard",
                Line = i + 1,
                Message = "Drag needs the synchronous per-frame loop, so it only works on Blazor WebAssembly. On " +
                          "Blazor Server the element simply does not move when the user tries to drag it, which " +
                          "reads as a broken page rather than as an unavailable feature.",
                Fix = "If this page can ever render on Server, gate it: inject BmotionCapabilities and check " +
                      "Caps.SupportsFrameLoop, giving the Server path a non-dragging equivalent.",
            });

            break;
        }
    }

    /// <summary>An endlessly repeating rotation with an easing - the stutter every spinner has.</summary>
    private static void CheckEasedRotation(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("rotate:", StringComparison.Ordinal) is false) continue;

            var window = string.Join('\n', lines[i..Math.Min(lines.Length, i + 4)]);

            if (window.Contains("BmRepeat.Forever", StringComparison.Ordinal) is false &&
                window.Contains("BmRepeat.Loop()", StringComparison.Ordinal) is false) continue;

            if (window.Contains("BmEase.Linear", StringComparison.Ordinal)) continue;

            findings.Add(new BmotionReviewFindingDto
            {
                Severity = "Warning",
                Rule = "eased-infinite-rotation",
                Line = i + 1,
                Message = "A continuously repeating rotation without BmEase.Linear speeds up and slows down once " +
                          "per revolution, so the spinner visibly stutters at the seam. Bm.Tween defaults to " +
                          "BmEase.Out, which is the wrong curve for looping motion.",
                Fix = "Pass BmEase.Linear: Bm.Tween(1, BmEase.Linear, repeat: BmRepeat.Forever)."
            });

            break;
        }
    }

    /// <summary>A gesture overlay that also writes the resting state, which the overlay restores anyway.</summary>
    private static void CheckRestingStateInGesture(string[] lines, List<BmotionReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("WhileHover", StringComparison.Ordinal) is false &&
                lines[i].Contains("WhileTap", StringComparison.Ordinal) is false) continue;

            if (RestingScaleRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmotionReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "resting-state-in-gesture",
                Line = i + 1,
                Message = "A gesture overlay is set to the element's resting value (scale: 1). Gesture overlays " +
                          "revert on their own when the gesture ends, so this has no effect beyond what already " +
                          "happens.",
                Fix = "Give the gesture the state it should move TO, and let it revert by itself."
            });
        }
    }

    /// <summary>Registration that never states a reduced-motion policy.</summary>
    private static void CheckReducedMotion(string text, string[] lines, List<BmotionReviewFindingDto> findings)
    {
        if (text.Contains("AddBitBmotionServices", StringComparison.Ordinal) is false) return;
        if (text.Contains("ReducedMotion", StringComparison.Ordinal)) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("AddBitBmotionServices", StringComparison.Ordinal) is false) continue;

            findings.Add(new BmotionReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "reduced-motion-not-configured",
                Line = i + 1,
                Message = "No reduced-motion policy is set, so the library keeps its back-compatible default " +
                          "(IgnoreUnlessConfigured), which consults the operating system preference only inside a " +
                          "<BmotionConfig>. Users who have asked their system for less motion will get the full " +
                          "animations everywhere else.",
                Fix = "AddBitBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User) - the web-platform " +
                      "default. Reduced motion still animates opacity and colour, so the interface stays legible."
            });

            break;
        }
    }

    private static int SeverityOrder(string severity) => severity switch
    {
        "Error" => 0,
        "Warning" => 1,
        _ => 2
    };

    [GeneratedRegex(@"\bExit\s*=")]
    private static partial Regex ExitAttributeRegex();

    [GeneratedRegex(@"\bBm\.Spring\s*\((?<args>[^()]*)\)")]
    private static partial Regex SpringCallRegex();

    [GeneratedRegex(@"<Bmotion\b[^>]*>", RegexOptions.Singleline)]
    private static partial Regex BmotionOpenTagRegex();

    [GeneratedRegex(@"<Bmotion\b[^>]*/>", RegexOptions.Singleline)]
    private static partial Regex SelfClosedBmotionRegex();

    [GeneratedRegex(@"@(foreach|for)\s*\(")]
    private static partial Regex LoopRegex();

    // A double-quoted attribute and the value Razor would read out of it - everything up to the next
    // double quote, which is exactly where a nested literal cuts the value short.
    [GeneratedRegex(@"\b(?<name>Initial|Animate|Exit|While\w+|Transition|Variants|DragConstraints|Viewport)\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex DoubleQuotedAttributeRegex();

    [GeneratedRegex(@"^\s*<(?<tag>[A-Za-z][\w.]*)")]
    private static partial Regex FirstChildTagRegex();

    [GeneratedRegex(@"\bAnimate\s*=\s*""(?<value>[^""]*)""")]
    private static partial Regex AnimateValueRegex();

    /// <summary>A reference to a private field - the convention Blazor component state is written in.</summary>
    [GeneratedRegex(@"(?<!\w)_\w")]
    private static partial Regex PrivateFieldRegex();

    // The two runs of non-parenthesis text around the optional nested call are ambiguous, so a
    // Bm.To( that is never closed - which is exactly what half-written code under review looks like -
    // makes the backtracking engine walk the line quadratically. NonBacktracking matches the same
    // strings in one pass.
    [GeneratedRegex(@"\bBm\.To\s*\((?<args>[^()]*(\([^()]*\))?[^()]*)\)", RegexOptions.NonBacktracking)]
    private static partial Regex TargetCallRegex();

    [GeneratedRegex(@"(?<name>[a-zA-Z][a-zA-Z0-9]*)\s*:")]
    private static partial Regex ArgumentNameRegex();

    [GeneratedRegex(@"\bDrag\s*=\s*""(?!false)")]
    private static partial Regex DragAttributeRegex();

    [GeneratedRegex(@"While(Hover|Tap)\s*=\s*['""]Bm\.To\([^)]*\bscale:\s*1\s*[,)]")]
    private static partial Regex RestingScaleRegex();
}
