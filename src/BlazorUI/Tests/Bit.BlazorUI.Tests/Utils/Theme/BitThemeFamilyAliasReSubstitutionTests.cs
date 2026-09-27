using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for family alias re-substitution: when an inline theme override (a
/// <see cref="BitThemeProvider"/> wrapper or <c>BitThemeManager.ApplyBitThemeAsync</c>, which share
/// the same augmentation) re-values a token that a per-family radius or elevation falls back to,
/// the family alias is re-declared - as its default <c>var()</c> reference - on the same element.
/// Without this, the family tier (substituted at <c>:root</c> where <c>family-tokens.scss</c>
/// defines it) would keep the document's corners and shadows inside the overridden subtree - and
/// every component reads the family tier rather than the primitive behind it.
/// </summary>
[TestClass]
public sealed class BitThemeFamilyAliasReSubstitutionTests : BunitTestContext
{
    // The plain `--bit-x: var(--bit-y);` declarations of the family tier. The app-bar shadows
    // (foreground-tinted expressions) and the snackbar elevation (a literal `none`) do not match,
    // which is exactly the set the C# table leaves out: there is nothing to re-substitute for them.
    private static readonly Regex FamilyDeclaration = new(
        @"(--bit-(?:shp-radius|shd)-[a-z0-9-]+)\s*:\s*var\((--bit-[a-z0-9-]+)\)\s*;",
        RegexOptions.Compiled);

    private static readonly Regex ReDeclaredAlias = new(
        @"--bit-(?:shp-radius|shd)-[a-z0-9-]+:var\(--bit-[a-z0-9-]+\)",
        RegexOptions.Compiled);

    private static (string Alias, string Target)[] ScssAliasPairs()
    {
        var scssPath = Path.Combine(AppContext.BaseDirectory, "theme-styles", "family-tokens.scss");
        Assert.IsTrue(File.Exists(scssPath), $"Missing {scssPath}; ensure the library Styles folder is copied to output.");

        return FamilyDeclaration.Matches(File.ReadAllText(scssPath))
            .Select(m => (Alias: m.Groups[1].Value, Target: m.Groups[2].Value))
            .Distinct()
            .ToArray();
    }

    // The per-role foregrounds are the tier's one derived color: a color-mix() of the role and the
    // primary foreground rather than a plain var() alias, so they are re-declared as that expression.
    private static readonly Regex RoleForegroundDeclaration = new(
        @"(--bit-clr-[a-z]+-fg)\s*:\s*(color-mix\([^;]+\))\s*;",
        RegexOptions.Compiled);

    private static readonly Regex ReDeclaredRoleForeground = new(
        @"--bit-clr-[a-z]+-fg:color-mix\(",
        RegexOptions.Compiled);

    private static (string Alias, string Value)[] ScssRoleForegrounds()
    {
        var scssPath = Path.Combine(AppContext.BaseDirectory, "theme-styles", "family-tokens.scss");
        Assert.IsTrue(File.Exists(scssPath), $"Missing {scssPath}; ensure the library Styles folder is copied to output.");

        return RoleForegroundDeclaration.Matches(File.ReadAllText(scssPath))
            .Select(m => (Alias: m.Groups[1].Value, Value: m.Groups[2].Value))
            .Distinct()
            .ToArray();
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

    [TestMethod]
    public void ReSubstitutionCoversExactlyTheScssFamilyVocabulary()
    {
        // Overriding the two roots of the family tier (the global radius and the callout shadow)
        // must re-declare every plain var() alias family-tokens.scss defines - each pair present (an
        // alias the C# table forgot, or one ordered ahead of the alias it chains from, fails here)
        // and none extra (an alias the scss no longer declares fails the count).
        var theme = new BitTheme();
        theme.Shape.BorderRadius = "1rem";
        theme.BoxShadow.Callout = "0 2px 4px #0003";

        var style = RenderProviderStyle(theme);
        var scssPairs = ScssAliasPairs();

        Assert.IsTrue(scssPairs.Length > 0, "family-tokens.scss declares no plain var() family aliases.");

        foreach (var (alias, target) in scssPairs)
        {
            StringAssert.Contains(style, $"{alias}:var({target})",
                $"Alias {alias} must be re-declared as var({target}) when its target is overridden.");
        }

        Assert.AreEqual(scssPairs.Length, ReDeclaredAlias.Matches(style).Count,
            "The provider re-declared a different number of family aliases than family-tokens.scss " +
            "defines - the C# alias table and the scss have drifted apart.");
    }

    [TestMethod]
    public void OverridingTheControlRadiusReachesButtonsChipsAndCheckboxes()
    {
        // The second link of the chain: the three control sub-families fall back to the control
        // radius rather than to the global one, so they have to follow an override of it.
        var theme = new BitTheme();
        theme.Shape.Radius.Control = "0.75rem";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-shp-radius-control:0.75rem");
        StringAssert.Contains(style, "--bit-shp-radius-button:var(--bit-shp-radius-control)");
        StringAssert.Contains(style, "--bit-shp-radius-chip:var(--bit-shp-radius-control)");
        StringAssert.Contains(style, "--bit-shp-radius-selection:var(--bit-shp-radius-control)");

        // an untouched family must not be dragged along by a sparse overlay
        Assert.IsFalse(style.Contains("--bit-shp-radius-surface", StringComparison.Ordinal),
            $"Untouched families must not be re-declared by a sparse overlay. Actual: {style}");
    }

    [TestMethod]
    public void OverridingTheCalloutShadowLeavesTheNonAliasElevationsAlone()
    {
        var theme = new BitTheme();
        theme.BoxShadow.Callout = "0 2px 4px #0003";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-shd-card:var(--bit-shd-cal)");

        // the snackbar is flat under Fluent and the app bars are tinted expressions, so neither is
        // an alias of the callout shadow and neither may be re-declared as one.
        Assert.IsFalse(style.Contains("--bit-shd-snackbar", StringComparison.Ordinal),
            $"The snackbar elevation is not an alias of the callout shadow. Actual: {style}");
        Assert.IsFalse(style.Contains("--bit-shd-appbar", StringComparison.Ordinal),
            $"The app-bar shadows are not aliases of the callout shadow. Actual: {style}");
    }

    [TestMethod]
    public void OverridingThePrimaryForegroundReShadesEveryRoleForeground()
    {
        // Every role foreground mixes towards the primary foreground, so re-valuing that one token must
        // re-declare all of them - with exactly the expression family-tokens.scss declares (an alias the C#
        // table forgot, or a default that drifted from the stylesheet, fails here) and none extra.
        var theme = new BitTheme();
        theme.Color.Foreground.Primary = "#101010";

        var style = RenderProviderStyle(theme);
        var scssForegrounds = ScssRoleForegrounds();

        Assert.AreEqual(8, scssForegrounds.Length, "family-tokens.scss must declare one foreground per accent role.");

        foreach (var (alias, value) in scssForegrounds)
        {
            StringAssert.Contains(style, $"{alias}:{value}",
                $"Role foreground {alias} must be re-declared as {value} when the primary foreground is overridden.");
        }

        Assert.AreEqual(scssForegrounds.Length, ReDeclaredRoleForeground.Matches(style).Count,
            "The provider re-declared a different number of role foregrounds than family-tokens.scss " +
            "defines - the C# table and the scss have drifted apart.");
    }

    [TestMethod]
    public void OverridingARoleReShadesOnlyThatRolesForeground()
    {
        var theme = new BitTheme();
        theme.Color.Warning.Main = "#FFB900";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-clr-wrn-fg:color-mix(in srgb, var(--bit-clr-wrn) 55%, var(--bit-clr-fg-pri))");
        Assert.AreEqual(1, ReDeclaredRoleForeground.Matches(style).Count,
            $"Untouched roles must not be re-declared by a sparse overlay. Actual: {style}");
    }

    [TestMethod]
    public void ExplicitRoleForegroundWinsOverReSubstitution()
    {
        var theme = new BitTheme();
        theme.Color.Warning.Main = "#FFB900";
        theme.Color.Warning.Foreground = "#8A5A00";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-clr-wrn-fg:#8A5A00");
        Assert.IsFalse(style.Contains("--bit-clr-wrn-fg:color-mix(", StringComparison.Ordinal),
            $"An explicitly-set role foreground must not be replaced by the re-substitution. Actual: {style}");
    }

    [TestMethod]
    public void ExplicitFamilyValueWinsOverReSubstitution()
    {
        var theme = new BitTheme();
        theme.Shape.BorderRadius = "1rem";
        theme.Shape.Radius.Surface = "2rem";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-shp-radius-surface:2rem");
        Assert.IsFalse(style.Contains("--bit-shp-radius-surface:var(", StringComparison.Ordinal),
            $"An explicitly-set family alias must not be replaced by the re-substitution. Actual: {style}");
    }
}
