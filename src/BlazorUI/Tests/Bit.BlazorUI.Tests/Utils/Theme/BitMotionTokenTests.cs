using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins how the motion tokens reach a component: the plain <c>--bit-mot-*</c> ones alias the unreduced <c>-full</c>
/// ones, the reduced-motion query collapses the aliases wherever they are declared, and a component reads the
/// unreduced values only through the <c>$mot-duration-*-full</c> aliases of <c>theme-variables.scss</c>.
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
    public void TheUnreducedDurationsHaveThemeAliases()
    {
        var scss = SourceFiles.ReadThemeStylesheet("theme-variables.scss");

        StringAssert.Contains(scss, "$mot-duration-full: var(--bit-mot-duration-full);");
        StringAssert.Contains(scss, "$mot-duration-short-full: var(--bit-mot-duration-short-full);");
        StringAssert.Contains(scss, "$mot-duration-long-full: var(--bit-mot-duration-long-full);");
    }

    [TestMethod]
    public void AnOverlayOfTheMotionTokensIsCollapsedUnderReducedMotion()
    {
        // A theme overlay writes the -full tokens on its own element, where the motion rule re-declares the aliases, so
        // the reduced-motion query has to collapse them on that element as well - and after it, to win at equal weight.
        var css = Regex.Replace(SourceFiles.Read("Bit.BlazorUI", "wwwroot", "styles", "bit.blazorui.css"), @"\s+", " ").Replace("\"", string.Empty);

        var overlay = Regex.Escape(":root [bit-theme-overlay~=motion]");
        var aliasing = Regex.Match(css, overlay + @" ?\{ ?--bit-mot-duration: ?var\(--bit-mot-duration-full\);");
        var reduced = Regex.Match(css, @"@media ?\(prefers-reduced-motion: ?reduce\) ?\{[^@]*?" + overlay + @" ?\{ ?--bit-mot-duration: ?0\.01ms;");

        Assert.IsTrue(aliasing.Success, "The core stylesheet has no rule re-declaring the motion aliases on a motion overlay.");
        Assert.IsTrue(reduced.Success, "The reduced-motion query does not collapse the motion aliases on a motion overlay.");
        Assert.IsTrue(reduced.Index > aliasing.Index, "The reduced-motion rule of the overlay must come after the rule it collapses.");
    }

    [TestMethod]
    public void ForceAnimationOutranksAMotionOverlayOnTheSameElement()
    {
        // The overlay's rule weighs (0,2,0), above .bit-fam alone, so the opt-out names that element at a weight above it.
        var general = SourceFiles.ReadThemeStylesheet("general.scss").Replace("\r\n", "\n");

        StringAssert.Contains(general, ".bit-fam,\n:root .bit-fam[bit-theme-overlay~=\"motion\"] {");
    }

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
