using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins that replacing the global <c>--bit-shd-focus-ring</c> re-shapes the focus ring of every component that has not
/// been given a focus color of its own.
/// </summary>
/// <remarks>
/// The composite substitutes its var()s where it is declared, on :root, so a color set further down the page cannot get
/// into it: a component whose ring has a color of its own has to compose that ring itself. <c>focus-ring($color)</c>
/// always does, and so always beats the global token; <c>focus-ring-own($color)</c> does only while something in the
/// color chain is set, and hands the ring back to the composite otherwise. A component focus color is therefore drawn
/// with the latter, and its chain carries no fallback to the primary focus color - that fallback is what kept the ring
/// from ever being the global one.
/// </remarks>
[TestClass]
public sealed class BitFocusRingStylesheetTests
{
    private static readonly Regex ColoredFocusRing = new(@"@include focus-ring\((?<args>[^;]+)\);", RegexOptions.Compiled);

    private static readonly Regex OwnFocusRing = new(@"@include focus-ring-own\((?<args>[^;]+)\);", RegexOptions.Compiled);

    // A ring a component keeps on purpose, whatever the global token says. The error and the disabled colors mean
    // something; a zero offset places the ring against its element (a grid cell), a shape the composite cannot take.
    // A state is a whole segment of a variable's name ($clr-err-focus, --bit-Toggle-error-color, --bit-mnb-dis-clr),
    // never a part of one: --bit-Display-focus-color or a "discrete" variant is a component color like any other.
    private static readonly Regex StateColor = new(@"(?<![A-Za-z0-9])(?:err|error|invalid|dis)(?![A-Za-z0-9])", RegexOptions.Compiled);

    private static readonly Regex ZeroOffset = new(@",\s*0$", RegexOptions.Compiled);

    // A default color that is not the primary focus color, so not one the global ring could stand in for: the variant's
    // own color of a ButtonGroup item, and the Info role an unset Color stands for on a Message.
    private static readonly string[] OwnDefaultColors = ["--bit-btg-itm-clr-fcs", "--bit-msg-focus"];

    // A custom property a var() chain reads - its own name, whatever its case, followed by the fallback or the end of
    // the var() - and a Sass variable it is written with.
    private static readonly Regex ChainVariable = new(@"var\(\s*(?<name>--bit-[A-Za-z0-9-]+)\s*[,)]", RegexOptions.Compiled);

    private static readonly Regex SassVariable = new(@"(?<name>\$[a-z][a-z0-9-]*)", RegexOptions.Compiled);

    private static readonly Regex FocusRingColorCall = new(@"focus-ring-color\((?:[^()]|\((?<depth>)|\)(?<-depth>))*\)", RegexOptions.Compiled);

    private static readonly string[] PrimaryFocusColor = ["$clr-pri-focus", "--bit-clr-pri-focus"];

    [TestMethod]
    public void TheStateColorExemptionShouldMatchWholeSegmentsOnly()
    {
        Assert.IsTrue(StateColor.IsMatch("#{$clr-err-focus}"));
        Assert.IsTrue(StateColor.IsMatch("var(--bit-Toggle-error-color, #{$clr-err-focus})"));
        Assert.IsTrue(StateColor.IsMatch("var(--bit-TextField-invalid-focus-color, #{$clr-err-focus})"));
        Assert.IsTrue(StateColor.IsMatch("var(--bit-mnb-dis-clr)"));

        Assert.IsFalse(StateColor.IsMatch("var(--bit-xyz-clr-focus, var(--bit-Display-focus-color))"));
        Assert.IsFalse(StateColor.IsMatch("var(--bit-xyz-discrete-focus)"));
        Assert.IsFalse(StateColor.IsMatch("var(--bit-xyz-overlay-border-focus)"));
    }

    [TestMethod]
    public void EveryComponentFocusColorShouldGiveTheGlobalRingBackWhileItIsUnset()
    {
        var offenders = new List<string>();

        foreach (var (file, stylesheet) in ReadComponentStylesheets())
        {
            foreach (Match call in ColoredFocusRing.Matches(stylesheet))
            {
                var args = call.Groups["args"].Value;

                if (StateColor.IsMatch(args) || ZeroOffset.IsMatch(args) || OwnDefaultColors.Any(args.Contains)) continue;

                offenders.Add($"{Path.GetFileName(file)}: {call.Value}");
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A component draws its focus color with focus-ring(), which never reads --bit-shd-focus-ring - use focus-ring-own(): {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void NoFocusRingOwnColorShouldFallBackToThePrimaryFocusColor()
    {
        var offenders = new List<string>();

        foreach (var (file, stylesheet) in ReadComponentStylesheets())
        {
            foreach (Match call in OwnFocusRing.Matches(stylesheet))
            {
                var color = call.Groups["args"].Value;
                var callAncestors = EnclosingHeaders(stylesheet, call.Index);

                if (UnconditionalValues(stylesheet, color, callAncestors).Prepend(color).Any(ReadsThePrimaryFocusColor))
                {
                    offenders.Add($"{Path.GetFileName(file)}: {call.Value}");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A focus-ring-own() color always resolves, so the ring is never the global one: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void TheChainGuardShouldFollowEveryAliasThatAlwaysApplies()
    {
        // The head of a chain that has a fallback, a public name in mixed case, and an alias declared in the root rule or
        // a mixin are all followed - and a role class that publishes the primary focus color for an explicit Color is not,
        // since it applies only to the instance that asked for that color.
        const string stylesheet = """
            @mixin abc-colors {
                --bit-abc-mix: #{$clr-pri-focus};
            }

            .bit-abc {
                --bit-abc-clr-focus: #{$clr-pri-focus};
                --bit-abc-alias: var(--bit-abc-inner);
                --bit-abc-inner: var(--bit-Abc-Focus-color, #{$clr-pri-focus});
                --bit-abc-stripped: #{focus-ring-color(var(--bit-Abc-focus-color, #{$clr-pri-focus}))};

                &:focus-visible {
                    @include focus-ring-own(var(--bit-abc-clr-focus, var(--bit-Abc-focus-color)));
                    @include focus-ring-own(var(--bit-abc-alias));
                    @include focus-ring-own(var(--bit-abc-mix));
                    @include focus-ring-own(var(--bit-abc-role));
                    @include focus-ring-own(var(--bit-abc-stripped));
                }
            }

            .bit-abc-pri {
                --bit-abc-role: #{$clr-pri-focus};
            }
            """;

        var resolved = OwnFocusRing.Matches(stylesheet)
                                   .Select(call => (call.Groups["args"].Value, UnconditionalValues(stylesheet, call.Groups["args"].Value, EnclosingHeaders(stylesheet, call.Index)).Any(ReadsThePrimaryFocusColor)))
                                   .ToArray();

        CollectionAssert.AreEqual(new[] { true, true, true, false, false }, resolved.Select(r => r.Item2).ToArray(),
                                  string.Join(", ", resolved.Select(r => $"{r.Item1}: {r.Item2}")));
    }

    [TestMethod]
    public void TheFocusRingOwnMixinShouldDrawTheGlobalRingWhileItsColorIsUnset()
    {
        var functions = SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss").Replace("\r\n", "\n");
        var mixin = SourceFiles.GetScssBlock(functions, "@mixin focus-ring-own($color, $also: null) {");

        StringAssert.Contains(mixin, "--bit-focus-ring-own: #{focus-ring-layers($color)};");
        StringAssert.Contains(mixin, "box-shadow: var(--bit-focus-ring-own, #{$box-shadow-focus-ring})#{$kept};");

        // The two layers are the recipe of the global token, composed against the element that draws them.
        var layers = SourceFiles.GetScssBlock(functions, "@function focus-ring-layers($color, $offset: $shp-focus-ring-offset) {");
        StringAssert.Contains(layers, "0 0 0 #{$offset} #{$clr-bg-pri}, 0 0 0 calc(#{$offset} + #{$shp-focus-ring-width}) #{$color}");

        // Box-shadows are stripped in forced-colors mode, so the Highlight outline stays on every ring.
        StringAssert.Contains(mixin, "@include focus-ring-forced-colors($shp-focus-ring-offset);");
    }

    [TestMethod]
    public void TheOwnRingShouldNotInheritIntoANestedFocusable()
    {
        // A recolored ring declared on an element that holds another focusable (a root drawn through :has() or
        // :focus-within) must not reach the nested one, under either rule an engine follows for a custom property that is
        // invalid at computed-value time - so the property is registered as one that does not inherit.
        var general = SourceFiles.Read("Bit.BlazorUI", "Styles", "general.scss").Replace("\r\n", "\n");

        StringAssert.Contains(general, "@property --bit-focus-ring-own {\n    syntax: \"*\";\n    inherits: false;\n}");
    }

    private static bool ReadsThePrimaryFocusColor(string value) => PrimaryFocusColor.Any(color => value.Contains(color, StringComparison.Ordinal));

    // Every value the chain resolves through whenever the ring is drawn: the declarations of the custom properties and
    // the Sass variables it reads, and of the ones those read in turn, wherever they always apply - at the top level, in
    // :root or a mixin, or in a rule the call itself is written in. A declaration in any other rule (a role class an
    // explicit Color adds) applies to the instance that asked for it alone, so the ring can still be the global one.
    private static IEnumerable<string> UnconditionalValues(string stylesheet, string chain, IReadOnlyList<string> callAncestors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>(Names(chain));

        while (pending.TryDequeue(out var name))
        {
            if (seen.Add(name) is false) continue;

            var declaration = new Regex($@"(?<![A-Za-z0-9-]){Regex.Escape(name)}\s*:\s*(?<value>[^;]+);");

            foreach (Match match in declaration.Matches(stylesheet))
            {
                var ancestors = EnclosingHeaders(stylesheet, match.Index);

                if (AlwaysApplies(ancestors, callAncestors) is false) continue;

                // focus-ring-color() is what takes the primary focus color back out of the color it is handed.
                var value = FocusRingColorCall.Replace(match.Groups["value"].Value, string.Empty);

                yield return value;

                foreach (var next in Names(value)) pending.Enqueue(next);
            }
        }
    }

    private static IEnumerable<string> Names(string value)
        => ChainVariable.Matches(value).Select(m => m.Groups["name"].Value)
                        .Concat(SassVariable.Matches(value).Select(m => m.Groups["name"].Value));

    private static bool AlwaysApplies(IReadOnlyList<string> ancestors, IReadOnlyList<string> callAncestors)
    {
        if (ancestors.Count == 0) return true;

        if (ancestors.Any(header => header.StartsWith("@mixin", StringComparison.Ordinal) || header == ":root")) return true;

        return ancestors.Count <= callAncestors.Count && ancestors.Select((header, i) => callAncestors[i] == header).All(same => same);
    }

    // The headers of the rules the position is nested in, the outermost first.
    private static IReadOnlyList<string> EnclosingHeaders(string stylesheet, int position)
    {
        var headers = new List<string>();
        var statementStart = 0;
        var interpolation = 0;

        for (var i = 0; i < position; i++)
        {
            var c = stylesheet[i];

            if (c == '{' && (interpolation > 0 || (i > 0 && stylesheet[i - 1] == '#')))
            {
                interpolation++;
            }
            else if (interpolation > 0)
            {
                if (c == '}') interpolation--;
            }
            else if (c == '{')
            {
                headers.Add(stylesheet[statementStart..i].Trim());
                statementStart = i + 1;
            }
            else if (c is '}' or ';')
            {
                if (c == '}' && headers.Count > 0) headers.RemoveAt(headers.Count - 1);
                statementStart = i + 1;
            }
        }

        return headers;
    }

    private static IEnumerable<(string File, string Stylesheet)> ReadComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories))
            .Select(file => (file, SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n")));
}
