using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for the density-aware tokens - the dialog and card insets, the control heights and
/// paddings and the other sizes measured in steps of the spacing unit. A custom property is computed where it is
/// declared, so one computed on <c>:root</c> would reach every descendant as the document's length and a density or
/// spacing unit re-valued lower in the tree (a <see cref="BitThemeProvider"/>, <c>ApplyBitThemeAsync</c> on the
/// body, plain CSS) would resize nothing under it. So no theme scope computes one: the presets declare its unitless
/// <c>-steps</c> and leave the token itself <c>initial</c>, and its alias in <c>theme-variables.scss</c> computes the
/// length where a component uses it, reading a value set for the token itself first.
/// </summary>
[TestClass]
public sealed class BitThemeDensityAwareTokensTests : BunitTestContext
{
    private const string ScalingFactor = "--bit-spa-scaling-factor";
    private const string DensityScale = "--bit-layout-density-scale";

    private static readonly Regex Declaration = new(
        @"(--bit-[a-z0-9-]+)\s*:\s*([^;{}]+)",
        RegexOptions.Compiled);

    // The innermost rules of a compiled stylesheet: a selector (or an at-rule's prelude) and a body without braces.
    private static readonly Regex InnermostRule = new(
        @"([^{}]+)\{([^{}]*)\}",
        RegexOptions.Compiled);

    private static readonly Regex DensityAwareAlias = new(
        @"^\$[a-z0-9-]+: var\((--bit-[a-z0-9-]+), calc\(var\(--bit-spa-scaling-factor\) \* var\(--bit-layout-density-scale, 1\) \* var\((--bit-[a-z0-9-]+)\)\)\);",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex UnitlessNumber = new(
        @"^\d+(\.\d+)?$",
        RegexOptions.Compiled);

    /// <summary>The compiled bundles that carry theme scopes: the core stylesheet and the packaged presets.</summary>
    private static readonly string[][] CompiledBundles =
    [
        ["Bit.BlazorUI", "wwwroot", "styles", "bit.blazorui.css"],
        ["Bit.BlazorUI.Extras", "wwwroot", "styles", "bit.blazorui.fluent2.css"],
        ["Bit.BlazorUI.Extras", "wwwroot", "styles", "bit.blazorui.material.css"],
        ["Bit.BlazorUI.Extras", "wwwroot", "styles", "bit.blazorui.cupertino.css"],
    ];

    private sealed record ThemeRule(string Bundle, string Selector, IReadOnlyDictionary<string, string> Declarations);

    /// <summary>
    /// Every rule of the compiled bundles that declares tokens for a theme scope (<c>:root</c> or a
    /// <c>[bit-theme]</c> element). Read compiled rather than as SCSS so a token derived through
    /// <c>spacing()</c> or the <c>$spacing-scaling-factor</c> / <c>$layout-density-scale</c> aliases is seen
    /// for what it compiles to.
    /// </summary>
    private static ThemeRule[] ThemeRules()
    {
        return CompiledBundles.SelectMany(segments =>
        {
            var path = SourceFiles.GetPath(segments);

            Assert.IsTrue(File.Exists(path), $"Missing {path}; it is compiled by the library's build, which building this test project runs.");

            return InnermostRule.Matches(File.ReadAllText(path))
                                .Select(m => (Selector: m.Groups[1].Value.Trim(), Body: m.Groups[2].Value))
                                .Where(r => r.Selector.Contains(":root", StringComparison.Ordinal) || r.Selector.Contains("[bit-theme", StringComparison.Ordinal))
                                .Select(r => new ThemeRule(segments[^1], r.Selector, Declaration.Matches(r.Body)
                                    .GroupBy(d => d.Groups[1].Value, StringComparer.Ordinal)
                                    .ToDictionary(g => g.Key, g => g.Last().Groups[2].Value.Trim(), StringComparer.Ordinal)));
        }).ToArray();
    }

    /// <summary>Each density-aware token named by its alias in theme-variables.scss, with the steps token it falls back to.</summary>
    private static Dictionary<string, string> DensityAwareAliases()
    {
        return DensityAwareAlias.Matches(SourceFiles.ReadThemeStylesheet("theme-variables.scss"))
                                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value, StringComparer.Ordinal);
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

    /// <summary>The theme tokens a provider declares, less the display it gives its own element.</summary>
    private string RenderProviderTokens(BitTheme theme)
    {
        return string.Join(';', RenderProviderStyle(theme).Split(';').Where(d => d.StartsWith("--bit-", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void NoThemeScopeComputesALengthFromTheSpacingInputs()
    {
        var rules = ThemeRules();

        Assert.IsTrue(rules.Length > 0, "Expected the compiled bundles to carry theme scopes.");

        foreach (var rule in rules)
        {
            foreach (var (token, value) in rule.Declarations)
            {
                if (token is ScalingFactor or DensityScale) continue;

                Assert.IsFalse(value.Contains(ScalingFactor, StringComparison.Ordinal) || value.Contains(DensityScale, StringComparison.Ordinal),
                    $"{rule.Bundle} computes {token} from the spacing inputs in '{rule.Selector}' ({value}). Computed there, it is " +
                    $"inherited as a length, so a density or spacing unit re-valued lower in the tree never reaches it: declare a " +
                    $"unitless {token}-steps, leave {token} initial, and give it a density-aware alias in theme-variables.scss.");
            }
        }
    }

    [TestMethod]
    public void EveryDensityAwareAliasReadsTheTokenThenItsOwnSteps()
    {
        var aliases = DensityAwareAliases();

        string[] expected =
        [
            "--bit-spa-dialog", "--bit-spa-card-sm", "--bit-spa-card-md", "--bit-spa-card-lg",
            "--bit-siz-ctrl-sm", "--bit-siz-ctrl-md", "--bit-siz-ctrl-lg",
            "--bit-siz-ctrl-pad-x-sm", "--bit-siz-ctrl-pad-x-md", "--bit-siz-ctrl-pad-x-lg",
            "--bit-siz-ctrl-pad-y-sm", "--bit-siz-ctrl-pad-y-md", "--bit-siz-ctrl-pad-y-lg",
            "--bit-siz-ctrl-min-width",
            "--bit-siz-sel-sm", "--bit-siz-sel-md", "--bit-siz-sel-lg",
            "--bit-siz-item-sm", "--bit-siz-item-md", "--bit-siz-item-lg",
            "--bit-siz-tab",
            "--bit-siz-switch-w-sm", "--bit-siz-switch-w-md", "--bit-siz-switch-w-lg",
            "--bit-siz-switch-h-sm", "--bit-siz-switch-h-md", "--bit-siz-switch-h-lg",
            "--bit-siz-switch-thumb-sm", "--bit-siz-switch-thumb-md", "--bit-siz-switch-thumb-lg",
            "--bit-siz-slider-thumb-sm", "--bit-siz-slider-thumb-md", "--bit-siz-slider-thumb-lg",
            "--bit-siz-badge-sm", "--bit-siz-badge-md", "--bit-siz-badge-lg",
            "--bit-siz-badge-dot-sm", "--bit-siz-badge-dot-md", "--bit-siz-badge-dot-lg",
            "--bit-siz-chip-sm", "--bit-siz-chip-md", "--bit-siz-chip-lg",
            "--bit-siz-popup-max-height", "--bit-siz-dialog-max-width",
        ];

        CollectionAssert.AreEquivalent(expected, aliases.Keys.ToArray(), "The density-aware aliases of theme-variables.scss changed.");

        foreach (var (token, steps) in aliases)
        {
            Assert.AreEqual($"{token}-steps", steps, $"The alias of {token} falls back to the steps of another token.");
        }
    }

    [TestMethod]
    public void EveryStepsTokenIsDeclaredByTheThemeScopesAndAliased()
    {
        var aliases = DensityAwareAliases();
        var rules = ThemeRules();

        var declaredSteps = rules.SelectMany(r => r.Declarations.Keys).Where(t => t.EndsWith("-steps", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);

        foreach (var steps in declaredSteps)
        {
            Assert.IsTrue(aliases.ContainsValue(steps), $"{steps} is declared by a theme scope, but no alias in theme-variables.scss computes a length from it.");
        }

        // The core stylesheet is what every app loads, so it declares a default for every steps token an alias reads -
        // except the button minimum width, which Fluent sets as `auto` and so derives from no steps.
        var core = rules.Where(r => r.Bundle == "bit.blazorui.css" && r.Selector.StartsWith(":root,", StringComparison.Ordinal)).SelectMany(r => r.Declarations.Keys).ToHashSet(StringComparer.Ordinal);

        foreach (var (token, steps) in aliases)
        {
            if (token == BitCss.Var.Size.ControlMinWidth)
            {
                Assert.IsTrue(core.Contains(token), "The core stylesheet no longer declares the button minimum width.");
                continue;
            }

            Assert.IsTrue(core.Contains(steps), $"The core stylesheet declares no default for {steps}, so {token} has nothing to compute from.");
        }
    }

    [TestMethod]
    public void AScopeDeclaringStepsLeavesTheTokenItselfInitial()
    {
        var aliases = DensityAwareAliases();
        var checkedAny = false;

        foreach (var rule in ThemeRules())
        {
            foreach (var (token, steps) in aliases)
            {
                if (rule.Declarations.TryGetValue(steps, out var value) is false) continue;

                checkedAny = true;

                Assert.IsTrue(UnitlessNumber.IsMatch(value), $"{rule.Bundle} declares {steps} as '{value}' in '{rule.Selector}'; it must be a unitless number so it inherits unchanged.");

                // A value an enclosing scope set for the token (app CSS on :root, a custom preset, a theme) would
                // otherwise win over this scope's steps, the way it would over any other token this scope declares.
                Assert.IsTrue(rule.Declarations.TryGetValue(token, out var own) && own == "initial",
                    $"{rule.Bundle} declares {steps} in '{rule.Selector}' without resetting {token} to initial beside it.");
            }
        }

        Assert.IsTrue(checkedAny, "Expected the theme scopes to declare steps.");
    }

    [TestMethod]
    public void PresetsKeepTheirOwnGeometry()
    {
        static string Value(string folder, string file, string token)
            => Declaration.Matches(SourceFiles.StripScssComments(SourceFiles.ReadThemeStylesheet(folder, file)))
                          .Single(m => m.Groups[1].Value == token).Groups[2].Value.Trim();

        Assert.AreEqual("3", Value("Fluent", "shapes.fluent.scss", "--bit-spa-dialog-steps"));
        Assert.AreEqual("2.5", Value("Cupertino", "tokens.cupertino.scss", "--bit-spa-dialog-steps"));
        Assert.AreEqual("4", Value("Fluent", "sizes.fluent.scss", "--bit-siz-ctrl-md-steps"));
        Assert.AreEqual("5", Value("Material", "tokens.material.scss", "--bit-siz-ctrl-md-steps"));
        Assert.AreEqual("4.5", Value("Cupertino", "tokens.cupertino.scss", "--bit-siz-ctrl-md-steps"));

        // The button floor follows density where a design system sets one, and is `auto` where it sets none.
        Assert.AreEqual("auto", Value("Fluent", "sizes.fluent.scss", "--bit-siz-ctrl-min-width"));
        Assert.AreEqual("auto", Value("Cupertino", "tokens.cupertino.scss", "--bit-siz-ctrl-min-width"));
        Assert.AreEqual("8", Value("Material", "tokens.material.scss", "--bit-siz-ctrl-min-width-steps"));
        Assert.AreEqual("12", Value("Fluent2", "tokens.fluent2.scss", "--bit-siz-ctrl-min-width-steps"));

        // Fluent's dialog ceiling follows density; the Extras presets each set their own as an absolute length.
        Assert.AreEqual("65", Value("Fluent", "sizes.fluent.scss", "--bit-siz-dialog-max-width-steps"));
        Assert.AreEqual("35rem", Value("Material", "tokens.material.scss", "--bit-siz-dialog-max-width"));
    }

    [TestMethod]
    public void DensityOnlyOverrideDeclaresTheDensityAlone()
    {
        Assert.AreEqual("--bit-layout-density-scale:0.9", RenderProviderTokens(new BitTheme { Layout = { DensityScale = "0.9" } }));
    }

    [TestMethod]
    public void ScalingFactorOnlyOverrideDeclaresTheSpacingUnitAlone()
    {
        Assert.AreEqual("--bit-spa-scaling-factor:0.25rem", RenderProviderTokens(new BitTheme { Spacing = { ScalingFactor = "0.25rem" } }));
    }

    [TestMethod]
    public void ExplicitTokensAndStepsAreDeclaredAsGiven()
    {
        var theme = new BitTheme { Layout = { DensityScale = "0.9" } };
        theme.Spacing.Card.Md = "10px";
        theme.Spacing.Steps.Dialog = "2";
        theme.Size.Control.Md = "30px";
        theme.Size.Steps.Item.Md = "5";
        theme.Size.Steps.Switch.Thumb.Lg = "2.5";

        var style = RenderProviderStyle(theme);

        StringAssert.Contains(style, "--bit-spa-card-md:10px");
        StringAssert.Contains(style, "--bit-spa-dialog-steps:2");
        StringAssert.Contains(style, "--bit-siz-ctrl-md:30px");
        StringAssert.Contains(style, "--bit-siz-item-md-steps:5");
        StringAssert.Contains(style, "--bit-siz-switch-thumb-lg-steps:2.5");
        Assert.IsFalse(style.Contains("calc(", StringComparison.Ordinal), $"Nothing is re-declared over the inputs. Actual: {style}");
    }

    [TestMethod]
    public void StepsCascadeThroughNestedProviders()
    {
        var outer = new BitTheme();
        outer.Size.Steps.Control.Md = "6";

        var inner = new BitTheme();
        inner.Size.Steps.Control.Sm = "3";

        var merged = BitThemeUtilities.Merge(inner, outer);

        Assert.AreEqual("6", merged.Size.Steps.Control.Md);
        Assert.AreEqual("3", merged.Size.Steps.Control.Sm);
    }
}
