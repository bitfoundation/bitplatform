using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins how the motion tokens reach a component: the plain <c>--bit-mot-*</c> ones alias the inherited reduction (the
/// <c>-reduced</c> tokens) or else the unreduced <c>-full</c> ones, wherever they are declared, and a component reads
/// the unreduced values only through the <c>$mot-*-full</c> aliases of <c>theme-variables.scss</c>.
/// </summary>
[TestClass]
public sealed class BitMotionTokenTests
{
    private static readonly Regex FullMotionToken = new(@"--bit-mot-[a-z-]+-full\b", RegexOptions.Compiled);

    [TestMethod]
    public void NoComponentStylesheetReadsAnUnreducedMotionTokenDirectly()
    {
        // The -full tokens are the theme's to set and the motion tier's to alias; a component that needs the unreduced
        // duration reads it through $mot-duration-*-full, like every other design-system decision it reads.
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file));

            offenders.AddRange(FullMotionToken.Matches(stylesheet).Select(m => $"{Path.GetFileName(file)}: {m.Value}"));
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders.ToArray(),
            $"Read the unreduced durations through $mot-duration-full / -short-full / -long-full: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void TheUnreducedTokensHaveThemeAliases()
    {
        // Each falls back to its plain token, for a consumer theme that declares only the plain ones by hand.
        var scss = SourceFiles.ReadThemeStylesheet("theme-variables.scss");

        StringAssert.Contains(scss, "$mot-duration-full: var(--bit-mot-duration-full, var(--bit-mot-duration));");
        StringAssert.Contains(scss, "$mot-duration-short-full: var(--bit-mot-duration-short-full, var(--bit-mot-duration-short));");
        StringAssert.Contains(scss, "$mot-duration-long-full: var(--bit-mot-duration-long-full, var(--bit-mot-duration-long));");
        StringAssert.Contains(scss, "$mot-duration-spinner-full: var(--bit-mot-duration-spinner-full, var(--bit-mot-duration-spinner));");
        StringAssert.Contains(scss, "$mot-easing-spinner-full: var(--bit-mot-easing-spinner-full, var(--bit-mot-easing-spinner));");
        StringAssert.Contains(scss, "$mot-loop-factor-full: var(--bit-mot-loop-factor-full, var(--bit-mot-loop-factor, 1));");
    }

    [TestMethod]
    public void EveryPlaceThatSubstitutesTheAliasesReadsTheInheritedReduction()
    {
        // Whether motion is reduced is the inherited -reduced tokens, so every rule that substitutes the plain aliases
        // again - :root, a scoped preset, a motion overlay (at no weight, so a consumer's own rule on that element still
        // wins), and .bit-fam - reads it off its ancestors instead of deciding it anew: an overlay or a scoped preset
        // inside a ForceAnimation subtree, or below an app that withdraws the reduction on :root, stays unreduced.
        var css = CompiledCoreCss();

        var aliasing = Regex.Match(css, Regex.Escape(":root,:where(:root [bit-theme]),:where(:root [bit-theme-overlay~=motion]){") + "[^}]*");
        var forced = Regex.Match(css, @"(?<![\w-])\.bit-fam\{[^}]*");

        Assert.IsTrue(aliasing.Success, "The motion aliases are not declared on :root, the scoped presets and the motion overlays by one rule.");
        Assert.IsTrue(forced.Success, "There is no .bit-fam rule of the motion tokens.");

        foreach (var (plain, reduced, full) in Aliases)
        {
            StringAssert.Contains(aliasing.Value, $"{plain}: var({reduced},var({full}))");
            StringAssert.Contains(forced.Value, $"{reduced}: initial", ".bit-fam does not withdraw the reduction.");
            StringAssert.Contains(forced.Value, $"{plain}: var({reduced},var({full}))", ".bit-fam does not substitute the aliases again.");
        }
    }

    [TestMethod]
    public void TheReducedMotionQueryDeclaresOnlyTheReductionOnTheRoot()
    {
        // A plain alias declared under the query would be the reduction decided anew on that element, which is what left an
        // overlay inside a ForceAnimation subtree collapsed. The query declares the -reduced tokens on :root and nothing
        // else, and .bit-fam comes after it, so it still withdraws them on the root element itself.
        var css = CompiledCoreCss();

        var query = Regex.Match(css, @"@media ?\(prefers-reduced-motion: ?reduce\) ?\{:root\{(?<body>--bit-mot-[^}]*)\}\}");

        Assert.IsTrue(query.Success, "The reduced-motion query of the motion tokens is no longer a :root rule of its own.");
        CollectionAssert.AreEquivalent(Aliases.Select(a => a.Reduced).ToArray(),
            Regex.Matches(query.Groups["body"].Value, @"(--bit-[a-z-]+):").Select(m => m.Groups[1].Value).ToArray());
        StringAssert.Contains(query.Groups["body"].Value, "--bit-mot-duration-reduced: 0.01ms");
        StringAssert.Contains(query.Groups["body"].Value, "--bit-mot-duration-spinner-reduced: 4s");
        StringAssert.Contains(query.Groups["body"].Value, "--bit-mot-easing-spinner-reduced: linear");
        StringAssert.Contains(query.Groups["body"].Value, "--bit-mot-loop-factor-reduced: 3");

        Assert.IsTrue(Regex.Match(css, @"(?<![\w-])\.bit-fam\{").Index > query.Index, ".bit-fam must come after the query to win on the root element.");
    }

    [TestMethod]
    public void OnlyTheMotionTierDeclaresTheReduction()
    {
        // The -reduced tokens are set only while the reduction is in force; a preset or a component declaring one would
        // reduce, or un-reduce, motion regardless of the preference.
        var declared = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project), "*.scss", SearchOption.AllDirectories))
            .Where(file => file.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false)
            .Where(file => Path.GetFileName(file) != "motion.fluent.scss")
            .SelectMany(file => Regex.Matches(SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)), @"--bit-mot-[a-z-]+-reduced\s*:")
                                     .Select(m => $"{Path.GetFileName(file)}: {m.Value}"))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared, $"Declared outside motion.fluent.scss: {string.Join(", ", declared)}");
    }

    private static readonly (string Plain, string Reduced, string Full)[] Aliases =
    [
        .. new[] { "duration", "duration-short", "duration-long", "duration-spinner", "easing-spinner", "loop-factor" }
            .Select(n => ($"--bit-mot-{n}", $"--bit-mot-{n}-reduced", $"--bit-mot-{n}-full"))
    ];

    private static string CompiledCoreCss()
        => Regex.Replace(SourceFiles.Read("Bit.BlazorUI", "wwwroot", "styles", "bit.blazorui.css"), @"\s*([{};,])\s*", "$1").Replace("\"", string.Empty);

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
