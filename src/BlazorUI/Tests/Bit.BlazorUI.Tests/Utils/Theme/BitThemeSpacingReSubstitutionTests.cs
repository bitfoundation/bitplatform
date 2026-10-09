using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for the spacing inset re-substitution: an inline theme override (a
/// <see cref="BitThemeProvider"/> wrapper or <c>BitThemeManager.ApplyBitThemeAsync</c>, which share the same
/// augmentation) that re-values only the density scale or the spacing unit re-declares <c>--bit-spa-dialog</c> and
/// <c>--bit-spa-card-{sm,md,lg}</c> on the same element, as the very expression every packaged preset declares them
/// with, so the dialogs and cards of the subtree resize against the active preset's steps instead of inheriting the
/// document's already-computed inset.
/// </summary>
[TestClass]
public sealed class BitThemeSpacingReSubstitutionTests : BunitTestContext
{
    /// <summary>
    /// The stylesheet each packaged preset declares its spacing insets in: the core Fluent tokens and the three
    /// presets of Bit.BlazorUI.Extras.
    /// </summary>
    private static readonly string[][] PresetSpacingStylesheets =
    [
        ["Fluent", "shapes.fluent.scss"],
        ["Fluent2", "tokens.fluent2.scss"],
        ["Material", "tokens.material.scss"],
        ["Cupertino", "tokens.cupertino.scss"],
    ];

    /// <summary>
    /// Any inset a stylesheet derives from the spacing unit and the density, which is what makes it one a scoped
    /// density override has to re-declare.
    /// </summary>
    private static readonly Regex DerivedInsetDeclaration = new(
        @"(--bit-spa-[a-z0-9-]+)\s*:\s*calc\(var\(--bit-spa-scaling-factor\)\s*\*\s*var\(--bit-layout-density-scale\)",
        RegexOptions.Compiled);

    /// <summary>
    /// The mapper's table of each inset and the steps token it is derived from - read as the mapper reads it, so a
    /// steps name renamed or mistyped there is one these tests look for in the stylesheets.
    /// </summary>
    private static readonly IReadOnlyList<KeyValuePair<string, string>> StepTargets =
        (IReadOnlyList<KeyValuePair<string, string>>)typeof(BitThemeProvider).Assembly
            .GetType("Bit.BlazorUI.BitThemeMapper", throwOnError: true)!
            .GetField("SpacingStepTargets", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;

    private static string Expected(string steps)
        => $"calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale) * var({steps}))";

    private static string ExpectedFor(string inset)
        => Expected(StepTargets.Single(target => target.Key == inset).Value);

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
    public void TheMapperReSubstitutesEveryInsetDerivedFromTheSpacingUnit()
    {
        var derived = SourceFiles.EnumerateThemeStylesheets()
            .SelectMany(file => DerivedInsetDeclaration.Matches(SourceFiles.ReadFullPath(file)).Select(m => m.Groups[1].Value))
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.Contains(derived, "--bit-spa-dialog");
        CollectionAssert.Contains(derived, "--bit-spa-card-md");

        CollectionAssert.AreEqual(derived, StepTargets.Select(target => target.Key).OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            "Every inset a stylesheet derives from the spacing unit and the density must have a row in BitThemeMapper.SpacingStepTargets, or a scoped density override leaves it at the document's length.");
    }

    [TestMethod]
    public void EveryPresetDeclaresEachStepsTokenTheMapperReadsAndDerivesTheInsetFromIt()
    {
        foreach (var segments in PresetSpacingStylesheets)
        {
            var file = Path.Combine(segments);
            var scss = SourceFiles.ReadThemeStylesheet(segments);

            foreach (var (inset, steps) in StepTargets)
            {
                Assert.IsTrue(Regex.IsMatch(scss, Regex.Escape(steps) + @"\s*:\s*\d+(?:\.\d+)?\s*;"),
                    $"{file} does not declare {steps}, the unitless steps BitThemeMapper re-declares {inset} against.");

                var declarations = Regex.Matches(scss, Regex.Escape(inset) + @"\s*:\s*([^;]+);");

                Assert.AreNotEqual(0, declarations.Count, $"{file} does not declare {inset}.");

                foreach (Match declaration in declarations)
                {
                    Assert.AreEqual(Expected(steps), declaration.Groups[1].Value.Trim(),
                        $"{file} declares {inset} differently from what a scoped density override re-declares it as.");
                }
            }
        }
    }

    [TestMethod]
    public void DensityOnlyOverrideReDeclaresTheInsets()
    {
        var style = RenderProviderStyle(new BitTheme { Layout = { DensityScale = "0.9" } });

        foreach (var (inset, steps) in StepTargets)
        {
            StringAssert.Contains(style, $"{inset}:{Expected(steps)}");
        }
    }

    [TestMethod]
    public void ScalingFactorOnlyOverrideReDeclaresTheInsets()
    {
        var style = RenderProviderStyle(new BitTheme { Spacing = { ScalingFactor = "0.25rem" } });

        foreach (var (inset, steps) in StepTargets)
        {
            StringAssert.Contains(style, $"{inset}:{Expected(steps)}");
        }
    }

    [TestMethod]
    public void ExplicitCardInsetWins()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Spacing.Card.Md = "10px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-card-md:10px");
        Assert.IsFalse(style.Contains($"--bit-spa-card-md:{ExpectedFor("--bit-spa-card-md")}", StringComparison.Ordinal));
        StringAssert.Contains(style, $"--bit-spa-card-sm:{ExpectedFor("--bit-spa-card-sm")}");
        StringAssert.Contains(style, $"--bit-spa-card-lg:{ExpectedFor("--bit-spa-card-lg")}");
        StringAssert.Contains(style, $"--bit-spa-dialog:{ExpectedFor("--bit-spa-dialog")}");
    }

    [TestMethod]
    public void ExplicitDialogInsetWins()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Spacing.Dialog = "20px";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-dialog:20px");
        Assert.IsFalse(style.Contains($"--bit-spa-dialog:{ExpectedFor("--bit-spa-dialog")}", StringComparison.Ordinal));
        StringAssert.Contains(style, $"--bit-spa-card-md:{ExpectedFor("--bit-spa-card-md")}");
    }

    [TestMethod]
    public void UntouchedDensityAndSpacingUnitLeaveTheInsetsInherited()
    {
        var style = RenderProviderStyle(new BitTheme { Shape = { BorderRadius = "4px" } });

        foreach (var (inset, _) in StepTargets)
        {
            Assert.IsFalse(style.Contains(inset, StringComparison.Ordinal),
                $"{inset} must not be re-declared when neither input changed. Actual: {style}");
        }
    }
}
