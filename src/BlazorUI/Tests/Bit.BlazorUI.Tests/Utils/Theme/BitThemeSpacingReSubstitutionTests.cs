using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for the card inset re-substitution: an inline theme override (a
/// <see cref="BitThemeProvider"/> wrapper or <c>BitThemeManager.ApplyBitThemeAsync</c>, which share the same
/// augmentation) that re-values only the density scale or the spacing unit re-declares
/// <c>--bit-spa-card-{sm,md,lg}</c> on the same element, as the very expression every packaged preset declares
/// them with, so the cards of the subtree resize against the active preset's steps instead of inheriting the
/// document's already-computed inset.
/// </summary>
[TestClass]
public sealed class BitThemeSpacingReSubstitutionTests : BunitTestContext
{
    private static readonly string[] Sizes = ["sm", "md", "lg"];

    private static readonly Regex CardInsetDeclaration = new(
        @"(--bit-spa-card-(?:sm|md|lg))\s*:\s*([^;]+);",
        RegexOptions.Compiled);

    private static string Expected(string size)
        => $"calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale) * var(--bit-spa-card-{size}-steps))";

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
    public void EveryPresetDeclaresTheCardInsetsAsTheReSubstitutedExpression()
    {
        var declarations = SourceFiles.EnumerateThemeStylesheets()
            .SelectMany(file => CardInsetDeclaration.Matches(SourceFiles.ReadFullPath(file))
                .Select(m => (File: Path.GetFileName(file), Inset: m.Groups[1].Value, Value: m.Groups[2].Value.Trim())))
            .ToArray();

        Assert.IsTrue(declarations.Length >= 12, "Expected the core Fluent tokens and the three Extras presets to declare the card insets.");

        foreach (var (file, inset, value) in declarations)
        {
            Assert.AreEqual(Expected(inset["--bit-spa-card-".Length..]), value,
                $"{file} declares {inset} differently from what a scoped density override re-declares it as.");
        }
    }

    [TestMethod]
    public void DensityOnlyOverrideReDeclaresTheCardInsets()
    {
        var style = RenderProviderStyle(new BitTheme { Layout = { DensityScale = "0.9" } });

        foreach (var size in Sizes)
        {
            StringAssert.Contains(style, $"--bit-spa-card-{size}:{Expected(size)}");
        }
    }

    [TestMethod]
    public void ScalingFactorOnlyOverrideReDeclaresTheCardInsets()
    {
        var style = RenderProviderStyle(new BitTheme { Spacing = { ScalingFactor = "0.25rem" } });

        foreach (var size in Sizes)
        {
            StringAssert.Contains(style, $"--bit-spa-card-{size}:{Expected(size)}");
        }
    }

    [TestMethod]
    public void ExplicitCardInsetWins()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Spacing.Card.Md = "10px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-card-md:10px");
        Assert.IsFalse(style.Contains($"--bit-spa-card-md:{Expected("md")}", StringComparison.Ordinal));
        StringAssert.Contains(style, $"--bit-spa-card-sm:{Expected("sm")}");
        StringAssert.Contains(style, $"--bit-spa-card-lg:{Expected("lg")}");
    }

    [TestMethod]
    public void UntouchedDensityAndSpacingUnitLeaveTheCardInsetsInherited()
    {
        var style = RenderProviderStyle(new BitTheme { Shape = { BorderRadius = "4px" } });

        Assert.IsFalse(style.Contains("--bit-spa-card-", StringComparison.Ordinal),
            $"The card insets must not be re-declared when neither input changed. Actual: {style}");
    }
}
