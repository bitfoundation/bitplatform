using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for the spacing re-substitution: an inline theme override (a
/// <see cref="BitThemeProvider"/> wrapper or <c>BitThemeManager.ApplyBitThemeAsync</c>, which share the same
/// augmentation) that re-values only the density scale or the spacing unit re-declares every token the
/// stylesheets derive from the two (the dialog and card insets, the control heights and paddings, the other
/// density-aware sizes) on the same element, as the very expression every packaged preset declares them with,
/// so the subtree resizes against the active preset's steps instead of inheriting the document's
/// already-computed lengths.
/// </summary>
[TestClass]
public sealed class BitThemeSpacingReSubstitutionTests : BunitTestContext
{
    private const string ScalingFactor = "--bit-spa-scaling-factor";
    private const string DensityScale = "--bit-layout-density-scale";

    private static readonly Regex Declaration = new(
        @"(--bit-[a-z0-9-]+)\s*:\s*([^;{}]+);",
        RegexOptions.Compiled);

    private static readonly Regex StyleEntry = new(
        @"(--bit-[a-z0-9-]+):([^;]+)",
        RegexOptions.Compiled);

    private static readonly Regex UnitlessNumber = new(
        @"^\d+(\.\d+)?$",
        RegexOptions.Compiled);

    private static string Expected(string token)
        => $"calc(var({ScalingFactor}) * var({DensityScale}) * var({token}-steps))";

    private string RenderProviderStyle(BitTheme theme)
    {
        var cut = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.AddChildContent("<span>content</span>");
        });

        return cut.Find("div").GetAttribute("style") ?? string.Empty;
    }

    private static Dictionary<string, string> ParseStyle(string style)
        => StyleEntry.Matches(style).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Trim(), StringComparer.Ordinal);

    /// <summary>The tokens a density-only override re-declares - the mapper's table, read through its only output.</summary>
    private HashSet<string> ReDeclaredTokens()
    {
        var style = ParseStyle(RenderProviderStyle(new BitTheme { Layout = { DensityScale = "0.9" } }));

        return style.Where(kv => kv.Value == Expected(kv.Key)).Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);
    }

    private static (string File, string Token, string Value)[] ThemeDeclarations()
        => SourceFiles.EnumerateThemeStylesheets()
            .SelectMany(file => Declaration.Matches(SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)))
                .Select(m => (File: Path.GetFileName(file), Token: m.Groups[1].Value, Value: m.Groups[2].Value.Trim())))
            .ToArray();

    [TestMethod]
    public void EveryTokenDerivedFromTheSpacingInputsIsReSubstituted()
    {
        var reDeclared = ReDeclaredTokens();

        var derived = ThemeDeclarations()
            .Where(d => d.Token is not ScalingFactor and not DensityScale)
            .Where(d => d.Value.Contains(ScalingFactor, StringComparison.Ordinal) || d.Value.Contains(DensityScale, StringComparison.Ordinal))
            .ToArray();

        Assert.IsTrue(derived.Length > 0, "Expected the theme stylesheets to derive tokens from the spacing unit and the density scale.");

        foreach (var (file, token, value) in derived)
        {
            Assert.AreEqual(Expected(token), value,
                $"{file} derives {token} from the spacing inputs differently from what a scoped density override re-declares it as. Declare it through a unitless {token}-steps.");

            Assert.IsTrue(reDeclared.Contains(token),
                $"{file} derives {token} from the spacing inputs, but a scoped density override does not re-declare it: add it to BitThemeMapper.SpacingDerivedTokens.");
        }

        var derivedTokens = derived.Select(d => d.Token).ToHashSet(StringComparer.Ordinal);
        foreach (var token in reDeclared)
        {
            Assert.IsTrue(derivedTokens.Contains(token),
                $"BitThemeMapper.SpacingDerivedTokens re-declares {token}, which no theme stylesheet derives from the spacing inputs.");
        }
    }

    [TestMethod]
    public void AReSubstitutedTokenIsDerivedInEveryPresetThatDeclaresIt()
    {
        var reDeclared = ReDeclaredTokens();

        // An absolute length or `auto` in one preset next to steps in another would be overwritten by the
        // re-declaration - and, under a nested scope, would read the outer preset's steps.
        foreach (var (file, token, value) in ThemeDeclarations().Where(d => reDeclared.Contains(d.Token)))
        {
            Assert.AreEqual(Expected(token), value,
                $"{file} declares {token} as something other than its steps, so a scoped density override would replace it.");
        }
    }

    [TestMethod]
    public void EveryFileDerivingATokenDeclaresItsOwnUnitlessSteps()
    {
        var declarations = ThemeDeclarations();
        var reDeclared = ReDeclaredTokens();

        foreach (var file in declarations.Where(d => reDeclared.Contains(d.Token)).GroupBy(d => d.File))
        {
            var steps = declarations.Where(d => d.File == file.Key).ToLookup(d => d.Token, d => d.Value, StringComparer.Ordinal);

            foreach (var (_, token, _) in file)
            {
                var values = steps[$"{token}-steps"].ToArray();

                Assert.AreEqual(1, values.Length, $"{file.Key} derives {token} but does not declare {token}-steps exactly once beside it.");
                Assert.IsTrue(UnitlessNumber.IsMatch(values[0]), $"{file.Key} declares {token}-steps as '{values[0]}'; it must be a unitless number so it inherits unchanged.");
            }
        }
    }

    [TestMethod]
    public void CoreFluentDerivesTheExpectedTokens()
    {
        var reDeclared = ReDeclaredTokens();

        string[] expected =
        [
            "--bit-spa-dialog",
            "--bit-spa-card-sm", "--bit-spa-card-md", "--bit-spa-card-lg",
            "--bit-siz-ctrl-sm", "--bit-siz-ctrl-md", "--bit-siz-ctrl-lg",
            "--bit-siz-ctrl-pad-x-md", "--bit-siz-ctrl-pad-y-md",
            "--bit-siz-sel-md", "--bit-siz-item-md", "--bit-siz-tab",
            "--bit-siz-switch-w-md", "--bit-siz-switch-h-md", "--bit-siz-switch-thumb-md",
            "--bit-siz-slider-thumb-md", "--bit-siz-badge-md", "--bit-siz-badge-dot-md", "--bit-siz-chip-md",
            "--bit-siz-popup-max-height",
        ];

        foreach (var token in expected)
        {
            Assert.IsTrue(reDeclared.Contains(token), $"A scoped density override does not re-declare {token}.");
        }

        Assert.IsFalse(reDeclared.Contains("--bit-siz-ctrl-min-width"), "The button minimum width is absolute (or auto) in every preset.");
        Assert.IsFalse(reDeclared.Contains("--bit-siz-dialog-max-width"), "The dialog maximum width is absolute in every preset.");
    }

    [TestMethod]
    public void PresetStepsKeepTheirOwnValues()
    {
        static string Steps(string folder, string file, string token)
            => Declaration.Matches(SourceFiles.StripScssComments(SourceFiles.ReadThemeStylesheet(folder, file)))
                .Single(m => m.Groups[1].Value == token).Groups[2].Value.Trim();

        Assert.AreEqual("3", Steps("Fluent", "shapes.fluent.scss", "--bit-spa-dialog-steps"));
        Assert.AreEqual("2.5", Steps("Cupertino", "tokens.cupertino.scss", "--bit-spa-dialog-steps"));
        Assert.AreEqual("4", Steps("Fluent", "sizes.fluent.scss", "--bit-siz-ctrl-md-steps"));
        Assert.AreEqual("5", Steps("Material", "tokens.material.scss", "--bit-siz-ctrl-md-steps"));
        Assert.AreEqual("4.5", Steps("Cupertino", "tokens.cupertino.scss", "--bit-siz-ctrl-md-steps"));
    }

    [TestMethod]
    public void DensityOnlyOverrideReDeclaresTheDerivedTokens()
    {
        var style = RenderProviderStyle(new BitTheme { Layout = { DensityScale = "0.9" } });

        foreach (var token in new[] { "--bit-spa-dialog", "--bit-spa-card-md", "--bit-siz-ctrl-md", "--bit-siz-item-md" })
        {
            StringAssert.Contains(style, $"{token}:{Expected(token)}");
        }
    }

    [TestMethod]
    public void ScalingFactorOnlyOverrideReDeclaresTheDerivedTokens()
    {
        var style = RenderProviderStyle(new BitTheme { Spacing = { ScalingFactor = "0.25rem" } });

        foreach (var token in new[] { "--bit-spa-dialog", "--bit-spa-card-md", "--bit-siz-ctrl-md", "--bit-siz-item-md" })
        {
            StringAssert.Contains(style, $"{token}:{Expected(token)}");
        }
    }

    [TestMethod]
    public void ExplicitTokensWin()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Spacing.Card.Md = "10px";
        theme.Spacing.Dialog = "18px";
        theme.Size.Control.Md = "30px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-card-md:10px");
        StringAssert.Contains(style, "--bit-spa-dialog:18px");
        StringAssert.Contains(style, "--bit-siz-ctrl-md:30px");
        Assert.IsFalse(style.Contains($"--bit-spa-card-md:{Expected("--bit-spa-card-md")}", StringComparison.Ordinal));
        Assert.IsFalse(style.Contains($"--bit-spa-dialog:{Expected("--bit-spa-dialog")}", StringComparison.Ordinal));
        Assert.IsFalse(style.Contains($"--bit-siz-ctrl-md:{Expected("--bit-siz-ctrl-md")}", StringComparison.Ordinal));
        StringAssert.Contains(style, $"--bit-spa-card-sm:{Expected("--bit-spa-card-sm")}");
        StringAssert.Contains(style, $"--bit-siz-ctrl-sm:{Expected("--bit-siz-ctrl-sm")}");
    }

    [TestMethod]
    public void UntouchedDensityAndSpacingUnitLeaveTheDerivedTokensInherited()
    {
        var style = RenderProviderStyle(new BitTheme { Shape = { BorderRadius = "4px" } });

        Assert.IsFalse(style.Contains("-steps)", StringComparison.Ordinal),
            $"The derived tokens must not be re-declared when neither input changed. Actual: {style}");
    }
}
