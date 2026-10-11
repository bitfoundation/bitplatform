using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the one way a duration follows the reduced-motion preference (#13552): the <c>--bit-mot-duration*</c> tokens
/// collapse under <c>prefers-reduced-motion</c>, and a duration the PAGE sets on a component - a public
/// <c>--bit-&lt;Component&gt;-*-duration</c> variable, a duration parameter written inline as a private <c>-full</c>
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

    // A read of the private token a duration or delay PARAMETER arrives inline as (--bit-col-dur-full, ...).
    private static readonly Regex InlineDurationRead = new(
        @"var\(--bit-[a-z0-9]+-(?:dur|del)-full(?![a-z0-9-])",
        RegexOptions.Compiled);

    private static readonly Regex ScaledRead = new(@"mot-scaled-(?:duration|delay)\(", RegexOptions.Compiled);

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

            var lines = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n").Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var match in PublicDurationRead.Matches(lines[i]).Concat(InlineDurationRead.Matches(lines[i])))
                {
                    if (IsScaled(lines[i], match.Index)) continue;

                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.AreEqual(0, offenders.Count,
            "A duration the page sets is read without mot-scaled-duration / mot-scaled-delay, so the reduced motion " +
            "preference never collapses it:\n" + string.Join("\n", offenders));
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

    private static bool IsScaled(string line, int index)
    {
        // The read sits inside an open mot-scaled-*( call: the nearest one before it on the line, with its parenthesis
        // still open where the read starts.
        foreach (Match call in ScaledRead.Matches(line[..index]).Reverse())
        {
            var depth = 0;

            for (var i = call.Index + call.Length - 1; i < index; i++)
            {
                if (line[i] == '(') depth++;
                else if (line[i] == ')') depth--;
            }

            if (depth > 0) return true;
        }

        return false;
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
