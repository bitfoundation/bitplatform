using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for semantic alias re-substitution: when an inline theme override (a
/// <see cref="BitThemeProvider"/> wrapper or <c>BitThemeManager.ApplyBitThemeAsync</c>, which share
/// the same augmentation) touches a primitive that a semantic alias points at, the alias is
/// re-declared - as its default <c>var()</c> reference - on the same element. Without this, the
/// alias tier (substituted at <c>:root</c> where <c>semantic-tokens.scss</c> defines it) would keep
/// the document palette's value inside the overridden subtree while components tracked the override.
/// </summary>
[TestClass]
public sealed class BitThemeSemanticAliasReSubstitutionTests : BunitTestContext
{
    private static readonly Regex SemDeclaration = new(
        @"(--bit-sem-[a-z0-9-]+)\s*:\s*var\((--bit-[a-z0-9-]+)\)\s*;",
        RegexOptions.Compiled);

    private static readonly Regex ReDeclaredAlias = new(
        @"--bit-sem-[a-z0-9-]+:var\(--bit-[a-z0-9-]+\)",
        RegexOptions.Compiled);

    private static (string Alias, string Target)[] ScssAliasPairs()
    {
        var scssPath = Path.Combine(AppContext.BaseDirectory, "theme-styles", "semantic-tokens.scss");
        Assert.IsTrue(File.Exists(scssPath), $"Missing {scssPath}; ensure the library Styles folder is copied to output.");

        return SemDeclaration.Matches(File.ReadAllText(scssPath))
            .Select(m => (Alias: m.Groups[1].Value, Target: m.Groups[2].Value))
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
    public void ReSubstitutionCoversExactlyTheScssAliasVocabulary()
    {
        // Every primitive set, no explicit semantic values: the provider must re-declare exactly
        // the alias->target pairs semantic-tokens.scss defines - each pair present (an alias the C#
        // side forgot fails here) and none extra (an alias scss no longer declares fails the count).
        var theme = new BitTheme();
        BitThemeTestGraph.FillStringLeavesWithSentinels(theme);
        theme.Color.Semantic = new BitThemeSemanticColors();

        var style = RenderProviderStyle(theme);
        var scssPairs = ScssAliasPairs();

        Assert.IsTrue(scssPairs.Length > 0, "semantic-tokens.scss declares no --bit-sem-*: var(--bit-*) aliases.");

        foreach (var (alias, target) in scssPairs)
        {
            StringAssert.Contains(style, $"{alias}:var({target})",
                $"Alias {alias} must be re-declared as var({target}) when its target primitive is overridden.");
        }

        Assert.AreEqual(scssPairs.Length, ReDeclaredAlias.Matches(style).Count,
            "The provider re-declared a different number of aliases than semantic-tokens.scss defines - " +
            "the C# alias map and the scss have drifted apart.");
    }

    [TestMethod]
    public void TouchedPrimitiveReDeclaresItsAliasAndOnlyItsAlias()
    {
        var theme = new BitTheme();
        theme.Color.Background.Secondary = "#101418";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-clr-bg-sec:#101418");
        StringAssert.Contains(style, "--bit-sem-surface-elevated:var(--bit-clr-bg-sec)");

        // Untouched intents must stay absent - a sparse overlay must not clobber unrelated
        // (possibly app-customized) intents.
        Assert.IsFalse(style.Contains("--bit-sem-surface-page", StringComparison.Ordinal),
            $"Untouched intents must not be re-declared by a sparse overlay. Actual: {style}");
        Assert.IsFalse(style.Contains("--bit-sem-accent-primary", StringComparison.Ordinal),
            $"Untouched intents must not be re-declared by a sparse overlay. Actual: {style}");
    }

    [TestMethod]
    public void ExplicitAliasValueWinsOverReSubstitution()
    {
        var theme = new BitTheme();
        theme.Color.Background.Secondary = "#101418";
        theme.Color.Semantic.SurfaceElevated = "#222222";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-sem-surface-elevated:#222222");
        Assert.IsFalse(style.Contains("--bit-sem-surface-elevated:var(", StringComparison.Ordinal),
            $"An explicitly-set alias must not be replaced by the re-substitution. Actual: {style}");
    }
}
