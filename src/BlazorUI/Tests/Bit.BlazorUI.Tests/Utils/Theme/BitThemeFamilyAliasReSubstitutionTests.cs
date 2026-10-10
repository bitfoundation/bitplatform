using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract and behavior for family alias re-substitution under an inline theme overlay (a
/// <see cref="BitThemeProvider"/> wrapper or <c>BitThemeManager.ApplyBitThemeAsync</c>). The family tier is
/// declared on <c>:root</c>, and a custom property's <c>var()</c>s are substituted where it is declared, so
/// the aliases an overlay's primitives feed have to be declared again on the overlay's element. What they
/// are declared AS is the active theme's decision (Material's tooltip casts no shadow, Fluent's card has a
/// depth of its own), so C# only names the groups the overlay re-values in the <c>bit-theme-overlay</c>
/// attribute and every theme stylesheet re-declares its own aliases for each group on that element. These
/// tests pin both halves: which groups an overlay names, and that every theme's overlay rules say exactly
/// what its own blocks say.
/// </summary>
[TestClass]
public sealed class BitThemeFamilyAliasReSubstitutionTests : BunitTestContext
{
    /// <summary>The compiled bundles that carry theme scopes: the core stylesheet and the packaged presets.</summary>
    private static readonly string[][] CompiledBundles =
    [
        ["Bit.BlazorUI", "wwwroot", "styles", "bit.blazorui.css"],
        ["Bit.BlazorUI.Extras", "wwwroot", "styles", "bit.blazorui.fluent2.css"],
        ["Bit.BlazorUI.Extras", "wwwroot", "styles", "bit.blazorui.material.css"],
        ["Bit.BlazorUI.Extras", "wwwroot", "styles", "bit.blazorui.cupertino.css"],
    ];

    private static readonly string[] Roles = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err"];

    private static readonly string[] ControlRadii = ["--bit-shp-radius-button", "--bit-shp-radius-chip", "--bit-shp-radius-selection", "--bit-shp-radius-tab-indicator"];

    /// <summary>The aliases each overlay group re-declares, as family-tokens.scss groups them.</summary>
    private static readonly Dictionary<string, string[]> GroupAliases = BuildGroupAliases();

    private static Dictionary<string, string[]> BuildGroupAliases()
    {
        var groups = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["radius"] = ["--bit-shp-radius-control", "--bit-shp-radius-surface", "--bit-shp-radius-popup", "--bit-shp-radius-dialog", .. ControlRadii],
            ["radius-control"] = ControlRadii,
            ["shadow"] = ["--bit-shd-card", "--bit-shd-card-hover", "--bit-shd-popup", "--bit-shd-dialog", "--bit-shd-sheet", "--bit-shd-tooltip",
                          "--bit-shd-snackbar", "--bit-shd-appbar-top", "--bit-shd-appbar-bottom"],
            ["tooltip"] = ["--bit-clr-tooltip-bg", "--bit-clr-tooltip-fg"],
            ["foreground"] = [.. Roles.Select(r => $"--bit-clr-{r}-fg")],
        };

        foreach (var role in Roles)
        {
            groups[role] = [$"--bit-clr-{role}-fg", $"--bit-clr-{role}-tint"];
        }

        return groups;
    }

    private static readonly Regex Declaration = new(@"(--bit-[a-z0-9-]+)\s*:\s*([^;{}]+)", RegexOptions.Compiled);

    // A theme block's own selectors: the document (`:root`, `:root[bit-theme=x]`) and the scoped twins
    // (`:root [bit-theme]`, `:root [bit-theme=x]`), with the quotes the compiler may or may not keep dropped.
    private static readonly Regex ThemeSelector = new(@"^:root(\[bit-theme=[a-z0-9-]+\])?$|^:root \[bit-theme(=[a-z0-9-]+)?\]$", RegexOptions.Compiled);

    private static readonly Regex OverlayGroup = new(@"\[bit-theme-overlay~=([a-z-]+)\]", RegexOptions.Compiled);

    /// <summary>The group names of the internal BitThemeMapper.FamilyAliasOverlayGroups table, in its order.</summary>
    private static string[] OverlayGroupNames()
    {
        var field = typeof(BitTheme).Assembly.GetType("Bit.BlazorUI.BitThemeMapper", throwOnError: true)!
                                    .GetField("FamilyAliasOverlayGroups", BindingFlags.NonPublic | BindingFlags.Static)!;

        return [.. ((IReadOnlyList<KeyValuePair<string, string[]>>)field.GetValue(null)!).Select(g => g.Key)];
    }

    private sealed record CssRule(string[] Selectors, IReadOnlyList<KeyValuePair<string, string>> Declarations);

    /// <summary>
    /// The top-level style rules of a compiled bundle in source order - the rules inside an at-rule (the
    /// forced-colors and phone blocks) are left out, as they apply only under their condition - each with
    /// its selector list split and its quotes dropped.
    /// </summary>
    private static List<CssRule> TopLevelRules(string css)
    {
        var rules = new List<CssRule>();
        var preludes = new Stack<string>();
        var text = new StringBuilder();

        foreach (var c in css)
        {
            if (c == '{')
            {
                // A statement ended by ';' ahead of the block (@charset, @import) is not part of its prelude.
                preludes.Push(text.ToString().Split(';')[^1].Trim().TrimStart('\uFEFF'));
                text.Clear();
            }
            else if (c == '}')
            {
                var prelude = preludes.Count > 0 ? preludes.Pop() : string.Empty;

                if (preludes.Count == 0 && prelude.StartsWith('@') is false)
                {
                    rules.Add(new CssRule(
                        [.. prelude.Replace("\"", string.Empty).Split(',').Select(s => Regex.Replace(s.Trim(), @"\s+", " "))],
                        [.. Declaration.Matches(text.ToString()).Select(m => new KeyValuePair<string, string>(m.Groups[1].Value, m.Groups[2].Value.Trim()))]));
                }

                text.Clear();
            }
            else
            {
                text.Append(c);
            }
        }

        return rules;
    }

    private static IEnumerable<(string Bundle, List<CssRule> Rules)> Bundles()
    {
        foreach (var segments in CompiledBundles)
        {
            var path = SourceFiles.GetPath(segments);

            Assert.IsTrue(File.Exists(path), $"Missing {path}; it is compiled by the library's build, which building this test project runs.");

            yield return (segments[^1], TopLevelRules(File.ReadAllText(path)));
        }
    }

    /// <summary>The overlay selectors theme-overlay.scss derives from one selector of a theme block.</summary>
    private static string[] OverlaySelectors(string themeSelector, string group)
    {
        var overlay = $"[bit-theme-overlay~={group}]";

        return themeSelector.StartsWith(":root [", StringComparison.Ordinal)
            ? [$":root:root {themeSelector[6..]} {overlay}", $":root:root {themeSelector[6..]}{overlay}"]
            : [$"{themeSelector} {overlay}"];
    }

    /// <summary>The value the LAST rule in the bundle naming <paramref name="selector"/> declares <paramref name="alias"/> with, if any.</summary>
    private static string? EffectiveValue(List<CssRule> rules, string selector, string alias)
    {
        string? value = null;

        foreach (var rule in rules.Where(r => r.Selectors.Contains(selector, StringComparer.Ordinal)))
        {
            foreach (var (name, declared) in rule.Declarations)
            {
                if (name == alias) value = declared;
            }
        }

        return value;
    }

    private IRenderedComponent<BitThemeProvider> RenderProvider(BitTheme theme)
    {
        return RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, theme);
            parameters.AddChildContent("<span>content</span>");
        });
    }

    private string? RenderOverlay(BitTheme theme)
    {
        return RenderProvider(theme).Find("div").GetAttribute(BitThemeAttributeNames.ThemeOverlay);
    }

    [TestMethod]
    public void EveryThemeReDeclaresItsOwnFamilyAliasesForAnOverlay()
    {
        // For every family alias a theme block declares, under each of its selectors, the overlay rule that
        // theme-overlay.scss derives from that selector for each group of the alias must declare the same
        // value - compared as what the bundle ends up with for each selector, so a later theme overriding an
        // earlier one (the Fluent depths over the core fallback) is checked as the override. A preset that
        // declares a family alias without the overlay rule beside it fails here: an overlay would re-declare
        // the core default in its place.
        var checkedPairs = 0;

        foreach (var (bundle, rules) in Bundles())
        {
            var themeRules = rules.Where(r => r.Selectors.All(ThemeSelector.IsMatch)).ToArray();

            foreach (var rule in themeRules)
            {
                foreach (var selector in rule.Selectors)
                {
                    foreach (var (group, aliases) in GroupAliases)
                    {
                        foreach (var alias in aliases.Where(a => rule.Declarations.Any(d => d.Key == a)))
                        {
                            var expected = EffectiveValue([.. themeRules], selector, alias);

                            foreach (var overlaySelector in OverlaySelectors(selector, group))
                            {
                                Assert.AreEqual(expected, EffectiveValue(rules, overlaySelector, alias),
                                    $"{bundle}: '{selector}' declares {alias} as '{expected}', so '{overlaySelector}' must re-declare it so for an " +
                                    $"overlay that re-values the {group} group - include the theme's {group} mixin in a theme-overlay rule.");
                                checkedPairs++;
                            }
                        }
                    }
                }
            }
        }

        Assert.IsTrue(checkedPairs > 500, $"Only {checkedPairs} alias/selector pairs were checked; the bundles no longer look like theme stylesheets.");
    }

    [TestMethod]
    public void TheOverlayGroupsAreExactlyTheOnesTheStylesheetsKeyOn()
    {
        // The groups C# names are the ones the stylesheets select: a group no stylesheet re-declares anything
        // for re-substitutes nothing, and one a stylesheet keys on but C# never writes is dead.
        var keyed = Bundles()
            .SelectMany(b => b.Rules)
            .SelectMany(r => r.Selectors)
            .SelectMany(s => OverlayGroup.Matches(s).Select(m => m.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            OverlayGroupNames().ToArray(),
            keyed.ToArray());

        CollectionAssert.AreEquivalent(GroupAliases.Keys.ToArray(), keyed.ToArray(),
            "This test's own map of the aliases per group has drifted from the groups the stylesheets key on.");
    }

    [TestMethod]
    public void TheCoreStylesheetReDeclaresExactlyTheGroupedAliasesForEachGroup()
    {
        // family-tokens.scss is the fallback every theme starts from: its rule for a group re-declares that
        // group's aliases - all of them, and nothing else.
        var (_, rules) = Bundles().First();

        foreach (var (group, aliases) in GroupAliases)
        {
            var rule = rules.FirstOrDefault(r => r.Selectors.Contains($":root [bit-theme-overlay~={group}]", StringComparer.Ordinal));

            Assert.IsNotNull(rule, $"The core stylesheet has no overlay rule for the {group} group.");
            CollectionAssert.AreEquivalent(aliases, rule.Declarations.Select(d => d.Key).ToArray(), $"The core overlay rule for the {group} group.");
        }
    }

    [TestMethod]
    public void NoThemeReDeclaresAnAliasOutsideItsGroupForAnOverlay()
    {
        // The overlay rule of the document's theme matches inside a scoped region of another theme as well,
        // and only the core rule's scoped twins outrank it there. So an alias a theme re-declares for a group
        // has to be one the core rule re-declares for that group too: one it does not would carry the
        // document theme's recipe into the region (Fluent dark's ring under a Cupertino snackbar).
        var checkedRules = 0;

        foreach (var (bundle, rules) in Bundles())
        {
            foreach (var rule in rules)
            {
                var groups = rule.Selectors.SelectMany(s => OverlayGroup.Matches(s).Select(m => m.Groups[1].Value)).Distinct().ToArray();

                if (groups.Length == 0) continue;

                foreach (var group in groups)
                {
                    foreach (var (alias, _) in rule.Declarations)
                    {
                        CollectionAssert.Contains(GroupAliases[group], alias,
                            $"{bundle}: '{string.Join(", ", rule.Selectors)}' re-declares {alias} for the {group} group, which the core " +
                            $"stylesheet's rule for that group does not - add it to the group's mixin in family-tokens.scss.");
                    }
                }

                checkedRules++;
            }
        }

        Assert.IsTrue(checkedRules > 20, $"Only {checkedRules} overlay rules were found; the bundles no longer look like theme stylesheets.");
    }

    [TestMethod]
    public void ForcedColorsReachTheOverlayElement()
    {
        // Every theme re-declares its elevations on the overlay element, which would bring the shadows back in
        // forced-colors mode unless the system-palette block covers that element too, from above them.
        var css = File.ReadAllText(SourceFiles.GetPath(CompiledBundles[0]));

        StringAssert.Contains(css, ":root:root:root:root [bit-theme-overlay]");
    }

    [TestMethod]
    public void AnOverlayNamesTheGroupsItsVariablesFeed()
    {
        var cases = new (string What, Action<BitTheme> Set, string Expected)[]
        {
            ("the global radius", t => t.Shape.BorderRadius = "1rem", "radius"),
            ("the control radius", t => t.Shape.Radius.Control = "0.75rem", "radius-control"),
            ("the full radius", t => t.Shape.Radius.Full = "100rem", "radius-control"),
            ("the callout shadow", t => t.BoxShadow.Callout = "0 2px 4px #0003", "shadow"),
            ("the tertiary border", t => t.Color.Border.Tertiary = "#CCCCCC", "shadow"),
            ("the border width", t => t.Shape.BorderWidth = "0.125rem", "shadow"),
            ("the primary background", t => t.Color.Background.Primary = "#FFFFFF", "tooltip"),
            ("the secondary background", t => t.Color.Background.Secondary = "#EEEEEE", "tooltip"),
            ("the primary foreground", t => t.Color.Foreground.Primary = "#111111", "tooltip foreground"),
            ("the warning role", t => t.Color.Warning.Main = "#FFB900", "wrn"),
            ("the primary role", t => t.Color.Primary.Main = "#0F6CBD", "pri"),
        };

        foreach (var (what, set, expected) in cases)
        {
            var theme = new BitTheme();
            set(theme);

            Assert.AreEqual(expected, RenderOverlay(theme), $"An overlay of {what}.");
        }
    }

    [TestMethod]
    public void AnOverlayOfEveryInputNamesEveryGroupInTableOrder()
    {
        var theme = new BitTheme();
        theme.Shape.BorderRadius = "1rem";
        theme.Shape.Radius.Control = "0.75rem";
        theme.BoxShadow.Callout = "0 2px 4px #0003";
        theme.Color.Background.Secondary = "#EEEEEE";
        theme.Color.Foreground.Primary = "#111111";
        theme.Color.Primary.Main = "#0F6CBD";
        theme.Color.Secondary.Main = "#FD7F36";
        theme.Color.Tertiary.Main = "#424242";
        theme.Color.Info.Main = "#6B737C";
        theme.Color.Success.Main = "#228422";
        theme.Color.Warning.Main = "#EDAE12";
        theme.Color.SevereWarning.Main = "#CE4207";
        theme.Color.Error.Main = "#D2393B";

        Assert.AreEqual(string.Join(' ', OverlayGroupNames()), RenderOverlay(theme));
    }

    [TestMethod]
    public void AnOverlayOfNoFamilyInputCarriesNoMarker()
    {
        var theme = new BitTheme();
        theme.Typography.FontFamily = "Georgia, serif";
        theme.Shape.Radius.Surface = "2rem";

        Assert.IsNull(RenderOverlay(theme));
    }

    [TestMethod]
    public void TheFamilyAliasesAreLeftToTheStylesheetsRatherThanInlined()
    {
        // Re-declaring them inline would fix them at the core default whatever the active theme decides; the
        // provider writes only the primitives it was given.
        var theme = new BitTheme();
        theme.Shape.BorderRadius = "1rem";
        theme.Shape.Radius.Control = "0.75rem";
        theme.BoxShadow.Callout = "0 2px 4px #0003";
        theme.Color.Background.Secondary = "#EEEEEE";
        theme.Color.Foreground.Primary = "#111111";
        theme.Color.Warning.Main = "#FFB900";

        var style = RenderProvider(theme).Find("div").GetAttribute("style") ?? string.Empty;

        foreach (var alias in GroupAliases.Values.SelectMany(a => a).Distinct().Where(a => a != "--bit-shp-radius-control"))
        {
            Assert.IsFalse(style.Contains($"{alias}:", StringComparison.Ordinal), $"{alias} must not be written inline. Actual: {style}");
        }
    }

    [TestMethod]
    public void ExplicitFamilyValueStaysInlineBesideTheMarker()
    {
        // An alias set on the theme is inline, and so outranks whatever a stylesheet re-declares for the group.
        var theme = new BitTheme();
        theme.Shape.BorderRadius = "1rem";
        theme.Shape.Radius.Surface = "2rem";
        theme.Color.TooltipBackground = "#222222";
        theme.Color.Background.Secondary = "#EEEEEE";

        var div = RenderProvider(theme).Find("div");
        var style = div.GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "--bit-shp-radius-surface:2rem");
        StringAssert.Contains(style, "--bit-clr-tooltip-bg:#222222");
        Assert.AreEqual("radius tooltip", div.GetAttribute(BitThemeAttributeNames.ThemeOverlay));
    }

    [TestMethod]
    public void ANestedProviderNamesWhatItInheritsAsWell()
    {
        // A nested provider writes the merge of its theme with its parent's, so it re-values (and names) both.
        var outer = new BitTheme();
        outer.BoxShadow.Callout = "0 2px 4px #0003";

        var inner = new BitTheme();
        inner.Shape.BorderRadius = "1rem";

        var cut = RenderComponent<BitThemeProvider>(parameters =>
        {
            parameters.Add(p => p.Theme, outer);
            parameters.AddChildContent<BitThemeProvider>(child =>
            {
                child.Add(p => p.Theme, inner);
                child.AddChildContent("<span>content</span>");
            });
        });

        var divs = cut.FindAll("div");

        Assert.AreEqual("shadow", divs[0].GetAttribute(BitThemeAttributeNames.ThemeOverlay));
        Assert.AreEqual("radius shadow", divs[1].GetAttribute(BitThemeAttributeNames.ThemeOverlay));
    }

    [TestMethod]
    public void TheMarkerFollowsTheThemeAcrossRenders()
    {
        var theme = new BitTheme();
        theme.BoxShadow.Callout = "0 2px 4px #0003";

        var cut = RenderProvider(theme);

        Assert.AreEqual("shadow", cut.Find("div").GetAttribute(BitThemeAttributeNames.ThemeOverlay));

        var next = new BitTheme();
        next.Typography.FontFamily = "Georgia, serif";

        cut.Render(parameters => parameters.Add(p => p.Theme, next));

        Assert.IsNull(cut.Find("div").GetAttribute(BitThemeAttributeNames.ThemeOverlay));
    }

    [TestMethod]
    public async Task ApplyBitThemeAsyncHandsTheGroupsToTheClient()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        var theme = new BitTheme();
        theme.Color.Foreground.Primary = "#111111";

        await manager.ApplyBitThemeAsync(theme);

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.applyTheme");
        var variables = (IReadOnlyDictionary<string, string>)invocation.Arguments[0]!;

        Assert.AreEqual("tooltip foreground", invocation.Arguments[2]);
        Assert.IsFalse(GroupAliases.Values.SelectMany(a => a).Any(variables.ContainsKey),
            "The family aliases are the stylesheets' to re-declare, not the overlay's.");
    }

    [TestMethod]
    public async Task ApplyBitThemeAsyncOfNoFamilyInputHandsTheClientNoGroups()
    {
        var manager = new BitThemeManager(Services.GetRequiredService<IJSRuntime>());

        var theme = new BitTheme();
        theme.Typography.FontFamily = "Georgia, serif";

        await manager.ApplyBitThemeAsync(theme);

        Assert.IsNull(Context.JSInterop.VerifyInvoke("BitBlazorUI.Theme.applyTheme").Arguments[2]);
    }
}
