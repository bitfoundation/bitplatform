using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for the global focus ring's re-substitution: <c>--bit-shd-focus-ring</c> is composed on
/// <c>:root</c> from the page background, the primary focus color and the ring's width and offset, so a
/// <see cref="BitThemeProvider"/> (or <c>BitThemeManager.ApplyBitThemeAsync</c>, which shares the augmentation) that
/// re-values one of them re-declares the composite on its own element - otherwise every ring that has not been given a
/// color of its own would keep the document's values inside the themed subtree.
/// </summary>
[TestClass]
public sealed class BitThemeFocusRingReSubstitutionTests : BunitTestContext
{
    // Each input of the composite, as a theme sets it.
    private static readonly Action<BitTheme>[] InputSetters =
    [
        theme => theme.Color.Primary.Focus = "red",
        theme => theme.Color.Background.Primary = "#111",
        theme => theme.Shape.FocusRingWidth = "4px",
        theme => theme.Shape.FocusRingOffset = "0px",
    ];

    [TestMethod]
    public void ReValuingAnInputShouldReDeclareTheRingAsTheStylesheetComposesIt()
    {
        var composite = ScssComposite();

        foreach (var set in InputSetters)
        {
            var theme = new BitTheme();
            set(theme);

            var style = RenderProviderStyle(theme);

            StringAssert.Contains(style, $"--bit-shd-focus-ring:{composite}");
            // The semantic alias reads the ring, so it is re-declared along with it.
            StringAssert.Contains(style, "--bit-sem-focus-ring:var(--bit-shd-focus-ring)");
        }
    }

    [TestMethod]
    public void TheInputsShouldBeExactlyTheTokensTheStylesheetComposesTheRingFrom()
    {
        var reads = Regex.Matches(ScssComposite(), @"var\((?<name>--bit-[a-z0-9-]+)")
                         .Select(m => m.Groups["name"].Value)
                         .Where(name => name.StartsWith("--bit-focus-ring-", StringComparison.Ordinal) is false)
                         .Distinct()
                         .OrderBy(name => name, StringComparer.Ordinal)
                         .ToArray();

        var inputs = new List<string>();

        foreach (var set in InputSetters)
        {
            var theme = new BitTheme();
            set(theme);

            inputs.AddRange(BitThemeUtilities.ToCssVariables(theme).Keys);
        }

        CollectionAssert.AreEqual(reads, inputs.Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    [TestMethod]
    public void AThemeThatLeavesTheInputsAloneShouldNotReDeclareTheRing()
    {
        var theme = new BitTheme();
        theme.Color.Background.Secondary = "#101418";

        var style = RenderProviderStyle(theme);

        Assert.IsFalse(style.Contains("--bit-shd-focus-ring", StringComparison.Ordinal),
                       $"A sparse overlay must not clobber a ring the app may have replaced further up. Actual: {style}");
    }

    [TestMethod]
    public void AnExplicitRingShouldWinOverTheReSubstitution()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Focus = "red";
        theme.BoxShadow.FocusRing = "0 0 0 3px lime";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-shd-focus-ring:0 0 0 3px lime");
        Assert.AreEqual(1, Regex.Matches(style, "--bit-shd-focus-ring:").Count, style);
    }

    private static string ScssComposite()
    {
        var declaration = Regex.Match(SourceFiles.ReadThemeStylesheet("Fluent", "shapes.fluent.scss"), @"--bit-shd-focus-ring:\s*(?<value>[^;]+);");

        Assert.IsTrue(declaration.Success, "shapes.fluent.scss no longer declares --bit-shd-focus-ring.");

        return Regex.Replace(declaration.Groups["value"].Value, @"\s+", " ").Trim();
    }

    private string RenderProviderStyle(BitTheme theme)
    {
        var cut = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.AddChildContent("<span>content</span>");
        });

        return cut.Find("div").GetAttribute("style") ?? string.Empty;
    }
}
