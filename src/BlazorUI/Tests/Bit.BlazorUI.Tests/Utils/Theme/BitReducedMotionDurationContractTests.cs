using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the one way a duration follows the reduced-motion preference (#13552): the <c>--bit-mot-duration*</c> tokens
/// collapse under <c>prefers-reduced-motion</c>, and a duration the PAGE sets on a component - a public
/// <c>--bit-&lt;Component&gt;-*-duration</c> variable, a duration parameter written inline as a private
/// token - is read through <c>mot-scaled-duration</c> / <c>mot-scaled-delay</c>, which scale it by the shared
/// <c>--bit-mot-duration-factor</c>.
/// </summary>
/// <remarks>
/// Before, some components collapsed a page-set duration with a reduced-motion query of their own and restored it in a
/// <c>.bit-fam</c> block of their own, while others left it alone: the same page setting behaved differently depending
/// on the component, an app re-enabling motion by restoring the tokens at <c>:root</c> got every component back but
/// the ones with a query, and every restore block had to be kept in step with motion.fluent.scss by hand. These tests
/// fail on a component that brings either pattern back.
/// </remarks>
[TestClass]
public sealed class BitReducedMotionDurationContractTests
{
    // A read of a public duration or delay variable of a component (--bit-Collapse-duration,
    // --bit-Slider-transition-duration, --bit-Shimmer-animation-delay, ...).
    private static readonly Regex PublicDurationRead = new(
        @"var\(--bit-[A-Z][A-Za-z]+-(?:[a-z]+-)*(?:duration|delay)(?![a-z-])",
        RegexOptions.Compiled);

    // A private property the C# or the markup beside a stylesheet gives a value inline, with the value written after it:
    // what a duration or delay PARAMETER arrives as (--bit-col-dur-full:{...}ms, --bit-cht-dur-full:{...}ms, ...).
    private static readonly Regex InlineDeclaration = new(
        @"(?<![a-zA-Z0-9-])(--bit-[a-z]+-[a-z0-9-]+)\s*:(?:\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}|[^;""{}]*?)(?<unit>m?s(?![a-z]))?(?=[;""]|$)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // A name that says it holds a time, whatever unit the C# writes the value in (--bit-ico-anm-dur, --bit-col-del-full).
    private static readonly Regex TimeName = new(@"-(?:dur|del|dly|duration|delay)(?:-|$)", RegexOptions.Compiled);

    // The scaled reads, by the name of the function that opens them.
    private static readonly Regex ScaledCall = new(@"mot-scaled-(?:duration|delay)$", RegexOptions.Compiled);

    // The private tokens of a looping animation's delay: a wait before a loop that reduced motion slows rather than
    // collapses, which is not motion (see BitIcon.AnimationDelay). The loop's own duration is read through
    // $mot-loop-factor, which the declaration says by itself.
    private static readonly string[] LoopDelays = ["--bit-ico-anm-dly"];

    private static readonly ConcurrentDictionary<string, string> ComponentSourceCache = new(StringComparer.Ordinal);

    // The looping placeholders, whose page-set pace is not one the factor can scale: reduced motion STOPS them rather
    // than shortening them, which a query of their own does (see theme-variables.scss).
    private static readonly string[] StoppedLoops = ["BitShimmer.scss", "BitTextShimmer.scss"];

    [TestMethod]
    public void EveryPageSetDurationIsReadThroughTheSharedScale()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            if (StoppedLoops.Contains(Path.GetFileName(file), StringComparer.Ordinal)) continue;

            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n");

            // The tokens a duration or delay parameter arrives inline as are found where they are written - the C#
            // and the markup beside the stylesheet - rather than by a naming convention, so a parameter written under
            // any name is held to the same scale.
            var inline = InlineTimeTokens(file).Except(LoopDelays, StringComparer.Ordinal).ToArray();

            var reads = PublicDurationRead.Matches(stylesheet)
                                          .Concat(inline.SelectMany(token => Regex.Matches(stylesheet, $@"var\({Regex.Escape(token)}(?![a-z0-9-])")));

            foreach (var match in reads)
            {
                if (IsScaled(stylesheet, match.Index) || IsLoop(stylesheet, match.Index)) continue;

                var line = stylesheet[..match.Index].Count(c => c == '\n');

                offenders.Add($"{Path.GetFileName(file)}:{line + 1}: {stylesheet.Split('\n')[line].Trim()}");
            }
        }

        Assert.AreEqual(0, offenders.Count,
            "A duration the page sets is read without mot-scaled-duration / mot-scaled-delay, so the reduced motion " +
            "preference never collapses it:\n" + string.Join("\n", offenders));
    }

    [TestMethod]
    public void TheInlineTimeTokensAreFoundWhereTheyAreWritten()
    {
        // The discovery the test above relies on, pinned on the tokens it has to find: a -full token, one under a name
        // of its own, a delay, a value written without a unit, and the element tokens the chart writes.
        var collapse = InlineTimeTokens(SourceFiles.GetPath("Bit.BlazorUI", "Components", "Surfaces", "Collapse", "BitCollapse.scss")).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "--bit-col-dur-full", "--bit-col-del-full" }, collapse);

        var chart = InlineTimeTokens(SourceFiles.GetPath("Bit.BlazorUI.Extras", "Components", "Chart", "BitChart.scss")).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "--bit-cht-dur-full", "--bit-cht-el-dur-full", "--bit-cht-el-del-full" }, chart);
        CollectionAssert.DoesNotContain(chart, "--bit-cht-ease");

        var icon = InlineTimeTokens(SourceFiles.GetPath("Bit.BlazorUI", "Components", "Utilities", "Icon", "BitIcon.scss")).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "--bit-ico-anm-dur", "--bit-ico-anm-dly" }, icon);
        CollectionAssert.DoesNotContain(icon, "--bit-ico-anm-itr");
    }

    [TestMethod]
    public void EveryReducedMotionQueryStepsAsideForForceAnimation()
    {
        // What stays a query of its own (a loop that stops, a scroll-behavior, a cue standing in for a motion) still
        // gives way to ForceAnimation, on the component that carries it as well as inside a subtree that does: every
        // selector it styles carries both halves of the guard, itself or through the rule it is nested in.
        var offenders = new List<string>();

        foreach (var (file, rule, declarations) in EnumerateDeclarations())
        {
            var chain = rule.Ancestors.Append(rule.Header).ToArray();

            if (chain.Any(h => h.StartsWith("@media", StringComparison.Ordinal) && h.Contains("prefers-reduced-motion")) is false) continue;

            // Only a rule that declares something applies anything.
            var body = declarations[(declarations.IndexOf('{') + 1)..].TrimEnd('}');
            if (string.IsNullOrWhiteSpace(body)) continue;

            var selectors = chain.Where(h => h.StartsWith('@') is false).ToArray();

            if (selectors.Any(h => h.Split(',').All(part => part.Contains(":not(.bit-fam):not(.bit-fam *)", StringComparison.Ordinal)))) continue;

            offenders.Add($"{Path.GetFileName(file)}: {string.Join(" ", chain)}");
        }

        Assert.AreEqual(0, offenders.Count,
            "A rule under prefers-reduced-motion applies inside a ForceAnimation subtree, or on the component that " +
            "sets it; guard every selector with :not(.bit-fam):not(.bit-fam *):\n" + string.Join("\n", offenders));
    }

    [TestMethod]
    public void TheStoppedLoopsStillStopUnderReducedMotion()
    {
        // The exception above holds only while those stylesheets keep stopping the loop outright.
        foreach (var name in StoppedLoops)
        {
            var file = EnumerateComponentStylesheets().Single(f => Path.GetFileName(f) == name);
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file));

            StringAssert.Contains(stylesheet, "@media (prefers-reduced-motion: reduce)", $"{name} no longer stops its loop.");
            StringAssert.Contains(stylesheet, ":not(.bit-fam):not(.bit-fam *)", $"{name} stops its loop inside a ForceAnimation subtree as well.");
        }
    }

    [TestMethod]
    public void NoComponentCollapsesADurationWithAQueryOfItsOwn()
    {
        // A custom property set under the query is the old per-component collapse (--bit-col-duration: 0.01ms).
        var offenders = new List<string>();

        foreach (var (file, rule, declarations) in EnumerateDeclarations())
        {
            if (rule.Ancestors.Append(rule.Header).Any(h => h.StartsWith("@media", StringComparison.Ordinal) && h.Contains("prefers-reduced-motion")) is false) continue;

            foreach (var name in CustomPropertyDeclarations(declarations))
            {
                offenders.Add($"{Path.GetFileName(file)}: {rule.Header} {{ {name} }}");
            }
        }

        Assert.AreEqual(0, offenders.Count,
            "A component sets a custom property under prefers-reduced-motion; scale the duration with " +
            "mot-scaled-duration instead:\n" + string.Join("\n", offenders));
    }

    [TestMethod]
    public void NoComponentRestoresADurationForForceAnimation()
    {
        // A rule matching .bit-fam (rather than excluding it through :not()) that sets a custom property is the old
        // per-component restore (.bit-col.bit-fam, .bit-fam .bit-col { --bit-col-duration: ... }).
        var offenders = new List<string>();

        foreach (var (file, rule, declarations) in EnumerateDeclarations())
        {
            var headers = rule.Ancestors.Append(rule.Header).Where(h => h.StartsWith('@') is false);

            if (headers.Any(h => StripNot(h).Contains(".bit-fam", StringComparison.Ordinal)) is false) continue;

            foreach (var name in CustomPropertyDeclarations(declarations))
            {
                offenders.Add($"{Path.GetFileName(file)}: {rule.Header} {{ {name} }}");
            }
        }

        Assert.AreEqual(0, offenders.Count,
            "A component restores a custom property of its own for .bit-fam; the shared factor already comes back " +
            "with it:\n" + string.Join("\n", offenders));
    }

    [TestMethod]
    public void TheFactorCollapsesUnderReducedMotionAndComesBackWithTheTokens()
    {
        var motion = SourceFiles.StripScssComments(SourceFiles.ReadThemeStylesheet("Fluent", "motion.fluent.scss")).Replace("\r\n", "\n");

        StringAssert.Contains(motion, "--bit-mot-duration-factor-full: 1;");
        StringAssert.Contains(motion, "--bit-mot-duration-factor: var(--bit-mot-duration-factor-full);");
        StringAssert.Contains(SourceFiles.GetScssBlock(motion, "@media (prefers-reduced-motion: reduce) {"), "--bit-mot-duration-factor: 0;");

        var general = SourceFiles.StripScssComments(SourceFiles.ReadThemeStylesheet("general.scss"));
        StringAssert.Contains(SourceFiles.GetScssBlock(general, ".bit-fam {"), "--bit-mot-duration-factor: var(--bit-mot-duration-factor-full, 1);");

        var variables = SourceFiles.ReadThemeStylesheet("theme-variables.scss");
        StringAssert.Contains(variables, "$mot-duration-factor: var(--bit-mot-duration-factor, 1);");

        var functions = SourceFiles.ReadThemeStylesheet("functions.scss");
        StringAssert.Contains(functions, "@return calc($duration * $mot-duration-factor + 0.01ms * (1 - $mot-duration-factor));");
        StringAssert.Contains(functions, "@return calc($delay * $mot-duration-factor);");
    }

    [TestMethod]
    public void TheSwiperScalesItsDurationByTheSameFactor()
    {
        var script = SourceFiles.Read("Bit.BlazorUI", "Components", "Lists", "Swiper", "BitSwiper.ts");

        StringAssert.Contains(script, "getPropertyValue('--bit-mot-duration-factor')");
    }

    // Whether the read sits inside an open mot-scaled-*( call, however many lines and parentheses lie between them:
    // walking back out of the declaration, one of the parentheses still open where the read starts is that call's.
    private static bool IsScaled(string stylesheet, int index)
        => EnclosingParentheses(stylesheet, index).Any(open => ScaledCall.IsMatch(stylesheet[NameStart(stylesheet, open)..open]));

    private static int NameStart(string stylesheet, int end)
    {
        while (end > 0 && (char.IsAsciiLetterLower(stylesheet[end - 1]) || stylesheet[end - 1] == '-')) end--;

        return end;
    }

    // Whether the read belongs to a looping animation, which reduced motion slows by $mot-loop-factor rather than
    // collapsing: the declaration it is in multiplies by that factor.
    private static bool IsLoop(string stylesheet, int index)
    {
        var start = stylesheet.LastIndexOfAny([';', '{', '}'], index) + 1;
        var end = stylesheet.IndexOfAny([';', '}'], index);

        return stylesheet[start..(end < 0 ? stylesheet.Length : end)].Contains("$mot-loop-factor", StringComparison.Ordinal);
    }

    // The positions of the parentheses still open at an index, the innermost first, back to the start of its statement.
    // A Sass interpolation (#{...}) is part of the statement rather than the end of it.
    private static IEnumerable<int> EnclosingParentheses(string stylesheet, int index)
    {
        var depth = 0;
        var interpolation = 0;

        for (var i = index - 1; i >= 0; i--)
        {
            var c = stylesheet[i];

            if (c == '}' && IsInterpolationClose(stylesheet, i)) interpolation++;
            else if (c == '{' && i > 0 && stylesheet[i - 1] == '#' && interpolation > 0) interpolation--;
            else if (c == ')') depth++;
            else if (c == '(')
            {
                if (depth == 0) yield return i;
                else depth--;
            }
            else if (interpolation == 0 && depth == 0 && (c == ';' || c == '{' || c == '}')) yield break;
        }
    }

    private static bool IsInterpolationClose(string stylesheet, int close)
    {
        var depth = 0;

        for (var i = close; i >= 0; i--)
        {
            if (stylesheet[i] == '}') depth++;
            else if (stylesheet[i] == '{' && --depth == 0) return i > 0 && stylesheet[i - 1] == '#';
        }

        return false;
    }

    // The private properties the C# and the markup beside a stylesheet give a time inline: a value written with a time
    // unit after it, or a name that says it is one.
    private static HashSet<string> InlineTimeTokens(string stylesheet)
    {
        var source = ComponentSourceCache.GetOrAdd(Path.GetDirectoryName(stylesheet)!, folder =>
            string.Join("\n", Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                       .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))
                                       .Select(SourceFiles.ReadFullPath)));

        return InlineDeclaration.Matches(source)
                                .Where(m => m.Groups["unit"].Success || TimeName.IsMatch(m.Groups[1].Value))
                                .Select(m => m.Groups[1].Value)
                                .ToHashSet(StringComparer.Ordinal);
    }

    private static string StripNot(string selector)
    {
        var result = selector;

        while (true)
        {
            var start = result.IndexOf(":not(", StringComparison.Ordinal);
            if (start < 0) return result;

            var depth = 0;
            var end = start + 4;

            for (; end < result.Length; end++)
            {
                if (result[end] == '(') depth++;
                else if (result[end] == ')' && --depth == 0) break;
            }

            result = result[..start] + result[Math.Min(end + 1, result.Length)..];
        }
    }

    private static IEnumerable<string> CustomPropertyDeclarations(string declarations)
        => Regex.Matches(declarations, @"(?:^|[;{])\s*(--[a-zA-Z0-9-]+)\s*:", RegexOptions.Multiline).Select(m => m.Groups[1].Value);

    private static IEnumerable<(string File, SourceFiles.ScssRule Rule, string Declarations)> EnumerateDeclarations()
    {
        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n");

            foreach (var rule in SourceFiles.GetScssRules(stylesheet))
            {
                if (rule.Header.StartsWith("@each", StringComparison.Ordinal) ||
                    rule.Header.StartsWith("@keyframes", StringComparison.Ordinal) ||
                    rule.Header.StartsWith("@property", StringComparison.Ordinal)) continue;

                yield return (file, rule, SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header));
            }
        }
    }

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
